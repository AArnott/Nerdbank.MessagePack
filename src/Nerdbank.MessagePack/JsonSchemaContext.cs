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
	private readonly MessagePackSerializer serializer;
	private readonly Dictionary<Type, string> schemaReferences = new();
	private readonly Dictionary<string, JsonObject> schemaDefinitions = new(StringComparer.Ordinal);
	private readonly HashSet<Type> recursionGuard = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonSchemaContext"/> class.
	/// </summary>
	/// <param name="serializer">The <see cref="MessagePackSerializer"/> object from which the JSON schema is being retrieved.</param>
	internal JsonSchemaContext(MessagePackSerializer serializer)
	{
		this.serializer = serializer;
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

		if (this.schemaReferences.TryGetValue(typeShape.Type, out string? referenceId))
		{
			return CreateReference(referenceId);
		}

		string definitionName = typeShape.Type.FullName!;
		string qualifiedReference = $"#/definitions/{definitionName}";
		if (!this.recursionGuard.Add(typeShape.Type))
		{
			this.schemaReferences.Add(typeShape.Type, qualifiedReference);
			return CreateReference(qualifiedReference);
		}

		IMessagePackConverter converter = this.serializer.GetOrAddConverter(typeShape);
		if (converter.GetJsonSchema(this, typeShape) is not JsonObject schema)
		{
			schema = new JsonObject
			{
				["type"] = new JsonArray("number", "integer", "string", "boolean", "object", "array", "null"),
				["description"] = $"The schema of this object is unknown as it is determined by the {converter.GetType().FullName} converter which does not override {nameof(MessagePackConverter<int>.GetJsonSchema)}.",
			};
		}

		this.recursionGuard.Remove(typeShape.Type);
		bool recursive = this.schemaReferences.ContainsKey(typeShape.Type);

		// If the schema is non-trivial, store it as a definition and return a reference.
		// We also store the schema as a definition if it was recursive.
		if (recursive || schema is not JsonObject { Count: 1 })
		{
			this.schemaDefinitions[definitionName] = schema;

			// Recursive types have already had their reference added to the schemaReferences dictionary.
			if (!recursive)
			{
				this.schemaReferences[typeShape.Type] = qualifiedReference;
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
