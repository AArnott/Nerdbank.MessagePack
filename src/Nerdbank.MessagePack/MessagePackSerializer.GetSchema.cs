// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft;
using PolyType.Utilities;

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

		JsonObject schema = new JsonSchemaGenerator(this).GenerateSchema(typeShape);
		return schema;
	}

	private sealed class JsonSchemaGenerator : ITypeShapeFunc
	{
		private static readonly JsonObject AnyTypeReferenceModel = new JsonObject
		{
			["$ref"] = "#/definitions/any",
		};

		private static readonly Dictionary<Type, SimpleTypeJsonSchema> SimpleTypeInfo = new()
		{
			[typeof(object)] = default,
			[typeof(bool)] = new("boolean"),
			[typeof(byte)] = new("integer"),
			[typeof(ushort)] = new("integer"),
			[typeof(uint)] = new("integer"),
			[typeof(ulong)] = new("integer"),
			[typeof(sbyte)] = new("integer"),
			[typeof(short)] = new("integer"),
			[typeof(int)] = new("integer"),
			[typeof(long)] = new("integer"),
			[typeof(float)] = new("number"),
			[typeof(double)] = new("number"),
			[typeof(decimal)] = new("number"),
			[typeof(Half)] = new("number"),
			[typeof(UInt128)] = new("integer"),
			[typeof(Int128)] = new("integer"),
			[typeof(char)] = new("string"),
			[typeof(string)] = new("string"),
			[typeof(byte[])] = new("string"),
			[typeof(Memory<byte>)] = new("string"),
			[typeof(ReadOnlyMemory<byte>)] = new("string"),
			[typeof(DateTime)] = new("string", pattern: "^msgpack extension -1 as base64: "),
			[typeof(DateTimeOffset)] = new("string", format: "date-time"),
			[typeof(TimeSpan)] = new("string", pattern: @"^-?(\d+\.)?\d{2}:\d{2}:\d{2}(\.\d{1,7})?$"),
			[typeof(DateOnly)] = new("string", format: "date"),
			[typeof(TimeOnly)] = new("string", format: "time"),
			[typeof(Guid)] = new("string", format: "uuid"),
			[typeof(Uri)] = new("string", format: "uri"),
			[typeof(Version)] = new("string"),
			[typeof(JsonDocument)] = default,
			[typeof(JsonElement)] = default,
			[typeof(JsonNode)] = default,
			[typeof(JsonValue)] = default,
			[typeof(JsonObject)] = new("object"),
			[typeof(JsonArray)] = new("array"),
		};

		private readonly Dictionary<(Type, bool AllowNull), string> locations = [];
		private readonly List<string> path = [];
		private readonly JsonSchemaContext context;
		private readonly MessagePackSerializer serializer;

		public JsonSchemaGenerator(MessagePackSerializer serializer)
		{
			this.serializer = serializer;
			this.context = new JsonSchemaContext(serializer);
		}

		object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => this.context.GetJsonSchema(typeShape);

		public JsonObject GenerateSchema(ITypeShape typeShape)
		{
			JsonObject schema = this.GenerateSchemaCore(typeShape);

			if (this.context.SchemaDefinitions.Count > 0)
			{
				schema["definitions"] = new JsonObject(this.context.SchemaDefinitions.Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value)));
			}

			schema.Add("$schema", "http://json-schema.org/draft-04/schema");

			return schema;
		}

		private static bool IsNullableType(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

		private JsonObject GenerateSchemaCore(ITypeShape typeShape)
		{
			bool allowNull = IsNullableType(typeShape.Type);

			if (SimpleTypeInfo.TryGetValue(typeShape.Type, out SimpleTypeJsonSchema simpleType) && allowNull)
			{
				return MessagePackConverter<int>.ApplyJsonSchemaNullability(simpleType.ToSchemaDocument());
			}

			ref string? location = ref CollectionsMarshal.GetValueRefOrAddDefault(this.locations, (typeShape.Type, allowNull), out bool exists);
			if (exists)
			{
				return new JsonObject
				{
					["$ref"] = (JsonNode)location!,
				};
			}
			else
			{
				location = this.path.Count == 0 ? "#" : $"#/{string.Join("/", this.path)}";
			}

			JsonObject schema = (JsonObject)typeShape.Invoke(this)!;

			return allowNull ? MessagePackConverter<int>.ApplyJsonSchemaNullability(schema) : schema;
		}

		private void Push(string name) => this.path.Add(name);

		private void Pop() => this.path.RemoveAt(this.path.Count - 1);

		private readonly struct SimpleTypeJsonSchema(string? type, string? format = null, string? pattern = null)
		{
			public string? Type => type;

			public string? Format => format;

			public string? Pattern => pattern;

			public JsonObject ToSchemaDocument()
			{
				var schema = new JsonObject();
				if (this.Type is not null)
				{
					schema["type"] = this.Type;
				}

				if (this.Format is not null)
				{
					schema["format"] = this.Format;
				}

				if (this.Pattern is not null)
				{
					schema["pattern"] = this.Pattern;
				}

				return schema;
			}
		}
	}
}
