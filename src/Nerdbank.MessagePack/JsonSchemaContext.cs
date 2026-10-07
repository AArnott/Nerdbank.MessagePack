// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// The context provided to <see cref="MessagePackConverter{T}.GetJsonSchema"/>
/// to aid in the generation of JSON schemas for types.
/// </summary>
public class JsonSchemaContext
{
	private readonly ConverterCache cache;
	private readonly Dictionary<Type, string> schemaReferences = new();
	private readonly Dictionary<string, JsonObject> schemaDefinitions = new(StringComparer.Ordinal);
	private readonly HashSet<Type> recursionGuard = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonSchemaContext"/> class.
	/// </summary>
	/// <param name="cache">The <see cref="ConverterCache"/> object from which the JSON schema is being retrieved.</param>
	/// <param name="extensionTypeCodes">The extension type codes to use for library-reserved extension types.</param>
	/// <param name="dialect">The dialect of the schemas being generated.</param>
	internal JsonSchemaContext(ConverterCache cache, LibraryReservedMessagePackExtensionTypeCode extensionTypeCodes, JsonSchemaDialect dialect)
	{
		this.cache = cache;
		this.ExtensionTypeCodes = extensionTypeCodes;
		this.Dialect = dialect;
	}

	/// <summary>
	/// Gets the dialect that converter-generated schema fragments must conform to.
	/// </summary>
	public JsonSchemaDialect Dialect { get; }

	/// <summary>
	/// Gets the keyword used for reusable schema definitions.
	/// </summary>
	internal string DefinitionsKeyword => this.Dialect switch
	{
		JsonSchemaDialect.Draft4 => "definitions",
		JsonSchemaDialect.Draft2020_12 => "$defs",
		_ => throw new NotSupportedException($"Unsupported JSON Schema dialect: {this.Dialect}."),
	};

	/// <summary>
	/// Gets the meta-schema URI for the selected dialect.
	/// </summary>
	internal string SchemaUri => this.Dialect switch
	{
		JsonSchemaDialect.Draft4 => "http://json-schema.org/draft-04/schema#",
		JsonSchemaDialect.Draft2020_12 => "https://json-schema.org/draft/2020-12/schema",
		_ => throw new NotSupportedException($"Unsupported JSON Schema dialect: {this.Dialect}."),
	};

	/// <summary>
	/// Gets the referenceable schema definitions that should be included in the top-level schema.
	/// </summary>
	internal IReadOnlyDictionary<string, JsonObject> SchemaDefinitions => this.schemaDefinitions;

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	internal LibraryReservedMessagePackExtensionTypeCode ExtensionTypeCodes { get; }

	/// <summary>
	/// Creates an array schema with positional constraints using the selected dialect.
	/// </summary>
	/// <param name="items">The schema for each successive array position. Ownership is transferred to the returned schema.</param>
	/// <returns>A schema allowing arrays of any length, with the specified positional constraints.</returns>
	/// <remarks>Set <c>minItems</c> and <c>maxItems</c> on the result to constrain array length.</remarks>
	/// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
	public JsonObject CreateTupleSchema(JsonArray items)
	{
		JsonObject schema = new() { ["type"] = "array" };
		this.ApplyTupleSchema(schema, items);
		return schema;
	}

	/// <summary>
	/// Adds positional array constraints to an existing schema using the selected dialect.
	/// </summary>
	/// <param name="schema">The schema to modify.</param>
	/// <param name="items">The schema for each successive array position. Ownership is transferred to <paramref name="schema"/>.</param>
	/// <remarks>This does not change the schema's type or array length constraints.</remarks>
	/// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
	public void ApplyTupleSchema(JsonObject schema, JsonArray items)
	{
		Requires.NotNull(schema);
		Requires.NotNull(items);
		Requires.Argument(items.Count > 0, nameof(items), "At least one positional schema is required.");
		string keyword = this.Dialect switch
		{
			JsonSchemaDialect.Draft4 => "items",
			JsonSchemaDialect.Draft2020_12 => "prefixItems",
			_ => throw new NotSupportedException($"Unsupported JSON Schema dialect: {this.Dialect}."),
		};
		schema[keyword] = items;
	}

	/// <summary>
	/// Obtains the JSON schema for a given type shape.
	/// </summary>
	/// <param name="typeShape">The shape for the type.</param>
	/// <returns>The JSON schema.</returns>
	public JsonObject GetJsonSchema(ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);

		Type type = typeShape.Type;
		if (this.schemaReferences.TryGetValue(type, out string? referenceId))
		{
			return CreateReference(referenceId);
		}

		string definitionName = GetAssemblyIndependentTypeName(type);
		string qualifiedReference = $"#/{this.DefinitionsKeyword}/{EscapeJsonPointerToken(definitionName)}";
		if (!this.recursionGuard.Add(type))
		{
			this.schemaReferences.Add(type, qualifiedReference);
			return CreateReference(qualifiedReference);
		}

		MessagePackConverter converter = this.cache.GetOrAddConverter(typeShape).ValueOrThrow;
		if (converter.GetJsonSchema(this, typeShape) is not JsonObject schema)
		{
			schema = MessagePackConverter<int>.CreateUndocumentedSchema(converter.GetType());
		}

		this.recursionGuard.Remove(type);
		bool recursive = this.schemaReferences.ContainsKey(type);

		// If the schema is non-trivial, store it as a definition and return a reference.
		// We also store the schema as a definition if it was recursive.
		if (recursive || schema is not JsonObject { Count: 1 })
		{
			this.schemaDefinitions[definitionName] = schema;

			// Recursive types have already had their reference added to the schemaReferences dictionary.
			if (!recursive)
			{
				this.schemaReferences[type] = qualifiedReference;
			}

			schema = CreateReference(qualifiedReference);
		}

		return schema;

		static string EscapeJsonPointerToken(string token)
		{
			string escapedToken = token.Replace("~", "~0").Replace("/", "~1");
			return Uri.EscapeDataString(escapedToken).Replace("%2B", "+").Replace("%2C", ",");
		}

		static string GetAssemblyIndependentTypeName(Type type)
		{
			if (type.IsArray)
			{
				return $"{GetAssemblyIndependentTypeName(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
			}

			if (type.IsByRef)
			{
				return $"{GetAssemblyIndependentTypeName(type.GetElementType()!)}&";
			}

			if (type.IsPointer)
			{
				return $"{GetAssemblyIndependentTypeName(type.GetElementType()!)}*";
			}

			if (type.IsGenericParameter)
			{
				return $"{(type.DeclaringMethod is null ? "!" : "!!")}{type.GenericParameterPosition}";
			}

			if (type.IsGenericType)
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				return $"{genericTypeDefinition.FullName}[{string.Join(",", type.GetGenericArguments().Select(GetAssemblyIndependentTypeName))}]";
			}

			return type.FullName!;
		}

		static JsonObject CreateReference(string referencePath) => new()
		{
			["$ref"] = referencePath,
		};
	}
}
