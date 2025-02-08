// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

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
	internal JsonSchemaContext(ConverterCache cache)
	{
		this.cache = cache;
	}

	/// <summary>
	/// Gets the referenceable schema definitions that should be included in the top-level schema.
	/// </summary>
	internal IReadOnlyDictionary<string, JsonObject> SchemaDefinitions => this.schemaDefinitions;

	/// <summary>
	/// Obtains the JSON schema for a given type.
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

		string definitionName = type.FullName!;
		string qualifiedReference = $"#/definitions/{definitionName}";
		if (!this.recursionGuard.Add(type))
		{
			this.schemaReferences.Add(type, qualifiedReference);
			return CreateReference(qualifiedReference);
		}

		MessagePackConverter converter = this.cache.GetOrAddConverter(typeShape);
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

		static JsonObject CreateReference(string referencePath) => new()
		{
			["$ref"] = referencePath,
		};
	}
}
