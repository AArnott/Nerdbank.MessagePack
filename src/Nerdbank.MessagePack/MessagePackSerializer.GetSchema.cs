// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

public partial record MessagePackSerializer
{
	/// <summary>
	/// <inheritdoc cref="GetJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The self-describing type whose schema should be produced.</typeparam>
	/// <returns><inheritdoc cref="GetJsonSchema(ITypeShape)" path="/returns"/></returns>
	public JsonObject GetJsonSchema<T>()
		where T : IShapeable<T> => this.GetJsonSchema(T.GetShape());

	/// <summary>
	/// <inheritdoc cref="GetJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The type whose schema should be produced.</typeparam>
	/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
	/// <returns><inheritdoc cref="GetJsonSchema(ITypeShape)" path="/returns"/></returns>
	public JsonObject GetJsonSchema<T, TProvider>()
		where TProvider : IShapeable<T> => this.GetJsonSchema(TProvider.GetShape());

	/// <summary>
	/// Creates a JSON Schema that describes the msgpack serialization of the given type's shape.
	/// </summary>
	/// <param name="typeShape">The shape of the type.</param>
	/// <returns>The JSON Schema document.</returns>
	public JsonObject GetJsonSchema(ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);

		if (this.PreserveReferences)
		{
			// This could be enhanced to support schema generation when PreserveReferences is enabled by changing every reference typed property
			// to describe that it may be either a msgpack extension or the object itself.
			throw new NotSupportedException($"Schema generation is not supported when {nameof(this.PreserveReferences)} is enabled.");
		}

		return new JsonSchemaGenerator(this).GenerateSchema(typeShape);
	}

	private sealed class JsonSchemaGenerator : ITypeShapeFunc
	{
		private readonly JsonSchemaContext context;

		internal JsonSchemaGenerator(MessagePackSerializer serializer)
		{
			this.context = new JsonSchemaContext(serializer);
		}

		object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => this.context.GetJsonSchema(typeShape);

		internal JsonObject GenerateSchema(ITypeShape typeShape)
		{
			JsonObject schema = (JsonObject)typeShape.Invoke(this)!;

			if (IsNullableType(typeShape.Type))
			{
				schema = MessagePackConverter<int>.ApplyJsonSchemaNullability(schema);
			}

			if (this.context.SchemaDefinitions.Count > 0)
			{
				schema["definitions"] = new JsonObject(this.context.SchemaDefinitions.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value)));
			}

			schema.Add("$schema", "http://json-schema.org/draft-04/schema");

			return schema;
		}

		private static bool IsNullableType(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
	}
}
