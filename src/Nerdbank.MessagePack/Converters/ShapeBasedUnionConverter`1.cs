// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable DuckTyping // Experimental API

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for a <see cref="DerivedTypeDuckTyping"/> union.
/// Cases are chosen by required property names. There is no fallback to the base converter.
/// </summary>
/// <typeparam name="TUnion">The union base type.</typeparam>
internal class ShapeBasedUnionConverter<TUnion> : MessagePackConverter<TUnion>
{
	private readonly Dictionary<Type, MessagePackConverter> convertersByType;
	private readonly MessagePackConverter[] convertersByCase;
	private readonly ITypeShape[] caseShapes;
	private readonly ObjectCaseClassifier.Case[] caseMetadata;
	private readonly ObjectCaseClassifier classifier;

	/// <summary>
	/// Initializes a new instance of the <see cref="ShapeBasedUnionConverter{TUnion}"/> class.
	/// </summary>
	/// <param name="shapeMapping">The duck-typing union. A concrete base must already have its object converter in <paramref name="convertersByType"/>.</param>
	/// <param name="convertersByType">Converters for each concrete case, including the base type when it is concrete.</param>
	/// <param name="cache">The serializer cache, used for serialized property names and keyed-layout settings.</param>
	/// <exception cref="NotSupportedException">Thrown when a case is not serialized as a name-keyed map.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown when a naming policy makes two cases indistinguishable.</exception>
	public ShapeBasedUnionConverter(
		DerivedTypeDuckTyping shapeMapping,
		Dictionary<Type, MessagePackConverter> convertersByType,
		ConverterCache cache)
	{
		this.convertersByType = convertersByType;

		int caseCount = (shapeMapping.BaseType is { IsAbstract: false, IsInterface: false } ? 1 : 0) + shapeMapping.DerivedShapes.Length;
		ObjectCaseClassifier.Case[] cases = new ObjectCaseClassifier.Case[caseCount];
		this.caseShapes = new ITypeShape[caseCount];
		this.convertersByCase = new MessagePackConverter[caseCount];
		int index = 0;
		if (shapeMapping.BaseType is { IsAbstract: false, IsInterface: false })
		{
			AddCase(shapeMapping.BaseShape);
		}

		foreach (ITypeShape shape in shapeMapping.DerivedShapes.Span)
		{
			AddCase(shape);
		}

		this.caseMetadata = cases;
		this.classifier = new ObjectCaseClassifier(typeof(TUnion), cases, namingPolicyApplied: true);

		bool preferAsync = false;
		for (int i = 0; i < this.convertersByCase.Length; i++)
		{
			if (this.convertersByCase[i].PreferAsyncSerialization)
			{
				preferAsync = true;
				break;
			}
		}

		this.PreferAsyncSerialization = preferAsync;

		void AddCase(ITypeShape shape)
		{
			DuckTypingRequiredMembers.EnsureNameKeyedMap(shape, cache.IgnoreKeyAttributes);
			string[] names = DuckTypingRequiredMembers.GetRequiredNames(
				shape,
				property => cache.GetSerializedPropertyName(property.Name, property.AttributeProvider));
			cases[index] = new ObjectCaseClassifier.Case(shape.Type, names);
			this.caseShapes[index] = shape;
			this.convertersByCase[index] = convertersByType[shape.Type];
			index++;
		}
	}

	/// <inheritdoc/>
	public override bool PreferAsyncSerialization { get; }

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		if (reader.NextMessagePackType != MessagePackType.Map)
		{
			throw new MessagePackSerializationException($"Duck-typed union '{typeof(TUnion).FullName}' expects a map, but the value was {reader.NextMessagePackType}.");
		}

		HashSet<string> names = ReadMapKeys(ref reader, context);
		int selected = this.classifier.Select(names);
		return (TUnion?)this.convertersByCase[selected].ReadObject(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		Type actualType = value.GetType();
		if (!this.convertersByType.TryGetValue(actualType, out MessagePackConverter? converter))
		{
			throw new MessagePackSerializationException($"No duck-typing union case is registered for type '{actualType.FullName}' in '{typeof(TUnion).FullName}'.");
		}

		converter.WriteObject(ref writer, value, context);
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		// Names choose the case. Property types must not, or a value that fails one case's type would
		// leave exactly one oneOf branch and accept a payload the reader rejects as ambiguous.
		JsonNode?[] nameBranches = new JsonNode[this.caseShapes.Length];
		JsonNode?[] typedBranches = new JsonNode[this.caseShapes.Length];
		for (int i = 0; i < this.caseShapes.Length; i++)
		{
			nameBranches[i] = this.NameConstraints(i);
			typedBranches[i] = new JsonObject
			{
				["allOf"] = new JsonArray(this.NameConstraints(i), CaseSchema(context, this.convertersByCase[i], this.caseShapes[i])),
			};
		}

		return new JsonObject
		{
			["allOf"] = new JsonArray(
				new JsonObject { ["oneOf"] = new JsonArray(nameBranches) },
				new JsonObject { ["anyOf"] = new JsonArray(typedBranches) }),
		};
	}

	/// <summary>
	/// Builds one case schema from the converter that reads and writes that case.
	/// </summary>
	/// <param name="context">The schema context. Property schemas may still be cached here.</param>
	/// <param name="converter">The object converter for the case.</param>
	/// <param name="shape">The case shape. A hierarchy is described through its object view.</param>
	/// <returns>The case schema.</returns>
	private static JsonNode CaseSchema(JsonSchemaContext context, MessagePackConverter converter, ITypeShape shape)
		=> (JsonNode)(converter.GetJsonSchema(context, DuckTypingRequiredMembers.ObjectView(shape)) ?? CreateUndocumentedSchema(converter.GetType()));

	/// <summary>
	/// Reads the string keys of a map without consuming <paramref name="reader"/>.
	/// Integer keys contribute no names.
	/// </summary>
	/// <param name="reader">A reader positioned at the map. The reader's position is not advanced.</param>
	/// <param name="context">The serialization context. Depth consumed while skipping values is not taken from this instance.</param>
	/// <returns>The string keys present on the map.</returns>
	private static HashSet<string> ReadMapKeys(ref MessagePackReader reader, SerializationContext context)
	{
		MessagePackReader peek = reader.CreatePeekReader();
		int count = peek.ReadMapHeader();
		HashSet<string> names = new(StringComparer.Ordinal);
		for (int i = 0; i < count; i++)
		{
			if (peek.NextMessagePackType == MessagePackType.String)
			{
				string? name = peek.ReadString();
				if (name is not null)
				{
					names.Add(name);
				}
			}
			else
			{
				peek.Skip(context);
			}

			peek.Skip(context);
		}

		return names;
	}

	/// <summary>
	/// Builds a JSON Schema <c>required</c> array for a sorted set of names.
	/// </summary>
	/// <param name="names">The required serialized names.</param>
	/// <returns>The array of names.</returns>
	private static JsonArray RequiredNames(string[] names)
	{
		JsonNode?[] items = new JsonNode[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			items[i] = JsonValue.Create(names[i]);
		}

		return new JsonArray(items);
	}

	/// <summary>
	/// Builds the name constraints for one concrete case.
	/// </summary>
	/// <param name="index">The case index.</param>
	/// <returns>
	/// A schema that matches when the reader would select this case from property names alone.
	/// It requires this case's names and excludes every strict superset. It does not check property types.
	/// </returns>
	private JsonObject NameConstraints(int index)
	{
		string[] mine = this.caseMetadata[index].RequiredNames;
		int exclusionCount = 0;
		for (int other = 0; other < this.caseMetadata.Length; other++)
		{
			if (other != index && ObjectCaseClassifier.IsStrictSuperset(this.caseMetadata[other].RequiredNames, mine))
			{
				exclusionCount++;
			}
		}

		bool stateRequiredNames = mine.Length > 0;
		if (exclusionCount == 0 && !stateRequiredNames)
		{
			return new JsonObject();
		}

		// Construct the array directly. JsonArray.Add<T> is incompatible with trimming.
		JsonNode?[] constraints = new JsonNode[(stateRequiredNames ? 1 : 0) + exclusionCount];
		int slot = 0;
		if (stateRequiredNames)
		{
			constraints[slot++] = new JsonObject
			{
				["required"] = RequiredNames(mine),
			};
		}

		for (int other = 0; other < this.caseMetadata.Length; other++)
		{
			string[] names = this.caseMetadata[other].RequiredNames;
			if (other == index || !ObjectCaseClassifier.IsStrictSuperset(names, mine))
			{
				continue;
			}

			constraints[slot++] = new JsonObject
			{
				["not"] = new JsonObject
				{
					["required"] = RequiredNames(names),
				},
			};
		}

		if (constraints.Length == 1)
		{
			return (JsonObject)constraints[0]!;
		}

		return new JsonObject
		{
			["allOf"] = new JsonArray(constraints),
		};
	}
}
