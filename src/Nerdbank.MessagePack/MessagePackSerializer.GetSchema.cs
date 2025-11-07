// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

public partial record MessagePackSerializer
{
#if NET
	/// <inheritdoc cref="MessagePackSerializerExtensions.GetJsonSchema{T}(MessagePackSerializer)"/>
	public JsonObject GetJsonSchema<T>()
		where T : IShapeable<T> => this.GetJsonSchema(T.GetTypeShape());

	/// <inheritdoc cref="MessagePackSerializerExtensions.GetJsonSchema{T, TProvider}(MessagePackSerializer)"/>
	public JsonObject GetJsonSchema<T, TProvider>()
		where TProvider : IShapeable<T> => this.GetJsonSchema(TProvider.GetTypeShape());
#endif

	/// <summary>
	/// <inheritdoc cref="GetJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The type whose schema should be produced.</typeparam>
	/// <param name="provider"><inheritdoc cref="MessagePackSerializer.CreateSerializationContext(ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <returns><inheritdoc cref="GetJsonSchema(ITypeShape)" path="/returns"/></returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "Public API accepts an ITypeShapeProvider.")]
	public JsonObject GetJsonSchema<T>(ITypeShapeProvider provider)
	{
		Requires.NotNull(provider);
		return this.GetJsonSchema(provider.GetTypeShape(typeof(T)) ?? throw new ArgumentException($"This provider had no type shape for {typeof(T)}.", nameof(provider)));
	}

	/// <summary>
	/// Creates a JSON Schema that describes the msgpack serialization of the given type's shape.
	/// </summary>
	/// <param name="typeShape">The shape of the type.</param>
	/// <returns>The JSON Schema document.</returns>
	public JsonObject GetJsonSchema(ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);

		if (this.PreserveReferences != ReferencePreservationMode.Off)
		{
			// This could be enhanced to support schema generation when PreserveReferences is enabled by changing every reference typed property
			// to describe that it may be either a msgpack extension or the object itself.
			throw new NotSupportedException($"Schema generation is not supported when {nameof(this.PreserveReferences)} is enabled.");
		}

		return new JsonSchemaGenerator(this.ConverterCache, this.LibraryExtensionTypeCodes).GenerateSchema(typeShape);
	}

	private sealed class JsonSchemaGenerator : ITypeShapeFunc
	{
		private readonly JsonSchemaContext context;

		internal JsonSchemaGenerator(ConverterCache cache, LibraryReservedMessagePackExtensionTypeCode extensionTypeCodes)
		{
			this.context = new JsonSchemaContext(cache, extensionTypeCodes);
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
