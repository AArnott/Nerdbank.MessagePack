// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft;
using PolyType.Utilities;

namespace Nerdbank.MessagePack;

/// <summary>
/// Creates documentation for a type shape's schema as it is serialized with this library.
/// </summary>
public static class MessagePackSchema
{
	/// <summary>
	/// <inheritdoc cref="ToJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The self-describing type whose schema should be produced.</typeparam>
	/// <returns><inheritdoc cref="ToJsonSchema(ITypeShape)" path="/returns"/></returns>
	public static JsonObject ToJsonSchema<T>()
		where T : IShapeable<T> => ToJsonSchema(T.GetShape());

	/// <summary>
	/// <inheritdoc cref="ToJsonSchema(ITypeShape)" path="/summary"/>
	/// </summary>
	/// <typeparam name="T">The type whose schema should be produced.</typeparam>
	/// <typeparam name="TProvider">The witness type that provides the shape for <typeparamref name="T"/>.</typeparam>
	/// <returns><inheritdoc cref="ToJsonSchema(ITypeShape)" path="/returns"/></returns>
	public static JsonObject ToJsonSchema<T, TProvider>()
		where TProvider : IShapeable<T> => ToJsonSchema(TProvider.GetShape());

	/// <summary>
	/// Creates a JSON Schema that describes the msgpack serialization of the given type's shape.
	/// </summary>
	/// <param name="typeShape">The shape of the type.</param>
	/// <returns>The JSON Schema document.</returns>
	public static JsonObject ToJsonSchema(ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);

		JsonObject schema = new Generator().GenerateSchema(typeShape);
		schema.Add("$schema", "http://json-schema.org/draft-04/schema");
		return schema;
	}

	private sealed class Generator
	{
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
			[typeof(DateTime)] = new("string", format: "date-time"),
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

		public JsonObject GenerateSchema(ITypeShape typeShape, bool allowNull = true, bool cacheLocation = true)
		{
			allowNull = allowNull && IsNullableType(typeShape.Type);

			if (SimpleTypeInfo.TryGetValue(typeShape.Type, out SimpleTypeJsonSchema simpleType))
			{
				return ApplyNullability(simpleType.ToSchemaDocument(), allowNull);
			}

			if (cacheLocation)
			{
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
			}

			JsonObject schema;
			switch (typeShape)
			{
				case IEnumTypeShape enumShape:
					schema = new JsonObject { ["type"] = "string" };
					if (enumShape.Type.GetCustomAttribute<FlagsAttribute>() is null)
					{
						schema["enum"] = CreateArray(Enum.GetNames(enumShape.Type).Select(name => (JsonNode)name));
					}

					break;

				case INullableTypeShape nullableShape:
					schema = this.GenerateSchema(nullableShape.ElementType, cacheLocation: false);
					break;

				case IEnumerableTypeShape enumerableShape:
					for (int i = 0; i < enumerableShape.Rank; i++)
					{
						this.Push("items");
					}

					schema = this.GenerateSchema(enumerableShape.ElementType);

					for (int i = 0; i < enumerableShape.Rank; i++)
					{
						schema = new JsonObject
						{
							["type"] = "array",
							["items"] = schema,
						};

						this.Pop();
					}

					break;

				case IDictionaryTypeShape dictionaryShape:
					this.Push("additionalProperties");
					JsonObject additionalPropertiesSchema = this.GenerateSchema(dictionaryShape.ValueType);
					this.Pop();

					schema = new JsonObject
					{
						["type"] = "object",
						["additionalProperties"] = additionalPropertiesSchema,
					};

					break;

				case IObjectTypeShape objectShape:
					schema = [];

					if (objectShape.HasProperties)
					{
						IConstructorShape? ctor = objectShape.GetConstructor();
						Dictionary<string, IConstructorParameterShape>? ctorParams = ctor?.GetParameters()
							.Where(p => p.Kind is ConstructorParameterKind.ConstructorParameter || p.IsRequired)
							.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

						JsonObject properties = [];
						JsonArray? required = null;

						this.Push("properties");
						foreach (IPropertyShape prop in objectShape.GetProperties())
						{
							IConstructorParameterShape? associatedParameter = null;
							ctorParams?.TryGetValue(prop.Name, out associatedParameter);

							bool isNonNullable =
								(!prop.HasGetter || prop.IsGetterNonNullable) &&
								(!prop.HasSetter || prop.IsSetterNonNullable) &&
								(associatedParameter is null || associatedParameter.IsNonNullable);

							this.Push(prop.Name);
							JsonObject propSchema = this.GenerateSchema(prop.PropertyType, allowNull: !isNonNullable);
							ApplyDescription(prop.AttributeProvider, propSchema);
							this.Pop();

							properties.Add(prop.Name, propSchema);
							if (associatedParameter?.IsRequired is true)
							{
								(required ??= []).Add((JsonNode)prop.Name);
							}
						}

						this.Pop();

						schema["type"] = "object";
						schema["properties"] = properties;
						if (required is not null)
						{
							schema["required"] = required;
						}
					}

					ApplyDescription(objectShape.AttributeProvider, schema);
					break;

				default:
					schema = [];
					break;
			}

			return ApplyNullability(schema, allowNull);
		}

		private static void ApplyDescription(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema)
		{
			if (attributeProvider?.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
			{
				propertySchema["description"] = description.Description;
			}
		}

		private static JsonObject ApplyNullability(JsonObject schema, bool allowNull)
		{
			if (allowNull && schema.TryGetPropertyValue("type", out JsonNode? typeValue))
			{
				if (schema["type"] is JsonArray types)
				{
					types.Add((JsonNode)"null");
				}
				else
				{
					schema["type"] = new JsonArray { (JsonNode)(string)typeValue!, (JsonNode)"null" };
				}
			}

			return schema;
		}

		private static JsonArray CreateArray(IEnumerable<JsonNode> elements)
		{
			var arr = new JsonArray();
			foreach (JsonNode elem in elements)
			{
				arr.Add(elem);
			}

			return arr;
		}

		private static bool IsNullableType(Type type)
		{
			return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
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
