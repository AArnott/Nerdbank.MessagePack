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
		private bool referencedAnyType;

		public JsonSchemaGenerator(MessagePackSerializer serializer)
		{
			this.serializer = serializer;
			this.context = new JsonSchemaContext(serializer);
		}

		private JsonObject AnyTypeReference
		{
			get
			{
				this.referencedAnyType = true;
				return (JsonObject)AnyTypeReferenceModel.DeepClone();
			}
		}

		object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => this.context.GetJsonSchema(typeShape);

		public JsonObject GenerateSchema(ITypeShape typeShape)
		{
			JsonObject schema = this.GenerateSchemaCore(typeShape);

			schema.Add("$schema", "http://json-schema.org/draft-04/schema");

			if (this.referencedAnyType || this.context.ReferencedAnyType)
			{
				JsonObject? definitions = (JsonObject?)schema["definitions"];
				if (definitions is null)
				{
					schema["definitions"] = definitions = new JsonObject();
				}

				definitions["any"] = new JsonObject
				{
					["type"] = new JsonArray("number", "string", "boolean", "object", "array", "null"),
				};
			}

			return schema;
		}

		private static void ApplyDescription(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema)
		{
			if (attributeProvider?.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
			{
				propertySchema["description"] = description.Description;
			}
		}

		private static void ApplyDefaultValue(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema, IConstructorParameterShape? parameterShape)
		{
			JsonValue? defaultValue =
				parameterShape?.HasDefaultValue is true ? CreateValue(parameterShape.DefaultValue) :
				attributeProvider?.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute att ? CreateValue(att.Value) :
				null;

			if (defaultValue is not null)
			{
				propertySchema["default"] = defaultValue;
			}
		}

		private static JsonValue? CreateValue(object? value)
		{
			return value switch
			{
				string v => JsonValue.Create(v),
				short v => JsonValue.Create(v),
				int v => JsonValue.Create(v),
				long v => JsonValue.Create(v),
				float v => JsonValue.Create(v),
				double v => JsonValue.Create(v),
				decimal v => JsonValue.Create(v),
				bool v => JsonValue.Create(v),
				byte v => JsonValue.Create(v),
				sbyte v => JsonValue.Create(v),
				ushort v => JsonValue.Create(v),
				uint v => JsonValue.Create(v),
				ulong v => JsonValue.Create(v),
				char v => JsonValue.Create(v),
				_ => null, // not supported.
			};
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

		private static bool IsNullableType(Type type)
		{
			return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
		}

		private JsonObject GenerateSchemaCore(ITypeShape typeShape, bool allowNull = true, bool cacheLocation = true)
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

			return (JsonObject)typeShape.Invoke(this)!;

			JsonObject schema;
			switch (typeShape)
			{
				case IEnumTypeShape enumShape:
					bool serializedByOrdinal = true; // https://github.com/AArnott/Nerdbank.MessagePack/issues/132
					schema = new JsonObject { ["type"] = serializedByOrdinal ? "integer" : "string" };
					if (serializedByOrdinal)
					{
						StringBuilder description = new();
						Array enumValuesUntyped = enumShape.Type.GetEnumValuesAsUnderlyingType();
						JsonNode[] enumValueNodes = new JsonNode[enumValuesUntyped.Length];
						for (int i = 0; i < enumValueNodes.Length; i++)
						{
							object ordinalValue = enumValuesUntyped.GetValue(i)!;
							if (description.Length > 0)
							{
								description.Append(", ");
							}

							description.Append($"{ordinalValue} = {Enum.GetName(enumShape.Type, ordinalValue)}");
							enumValueNodes[i] = CreateValue(ordinalValue) ?? throw new NotSupportedException("Unrecognized ordinal value type.");
						}

						schema["enum"] = new JsonArray(enumValueNodes);
						schema["description"] = description.ToString();
					}
					else
					{
						if (enumShape.Type.GetCustomAttribute<FlagsAttribute>() is null)
						{
							schema["enum"] = new JsonArray(Enum.GetNames(enumShape.Type).Select(name => (JsonNode)name).ToArray());
						}
					}

					break;

				case INullableTypeShape nullableShape:
					schema = this.GenerateSchemaCore(nullableShape.ElementType, cacheLocation: false);
					break;

				case IEnumerableTypeShape enumerableShape:
					for (int i = 0; i < enumerableShape.Rank; i++)
					{
						this.Push("items");
					}

					schema = this.GenerateSchemaCore(enumerableShape.ElementType);

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
					JsonObject additionalPropertiesSchema = this.GenerateSchemaCore(dictionaryShape.ValueType);
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
						JsonArray? items = null;
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

							KeyAttribute? keyAttribute = prop.AttributeProvider?.GetCustomAttribute<KeyAttribute>();
							string propertyName = keyAttribute?.Index.ToString() ??
								this.serializer.GetSerializedPropertyName(prop.Name, prop.AttributeProvider);
							this.Push(propertyName);
							JsonObject propSchema = this.GenerateSchemaCore(prop.PropertyType, allowNull: !isNonNullable);
							this.Pop();
							ApplyDescription(prop.AttributeProvider, propSchema);
							ApplyDefaultValue(prop.AttributeProvider, propSchema, associatedParameter);

							if (keyAttribute is not null)
							{
								items ??= new JsonArray();

								while (items.Count < keyAttribute.Index)
								{
									items.Add((JsonNode)this.AnyTypeReference);
								}

								JsonObject itemSchema = this.GenerateSchemaCore(prop.PropertyType, allowNull: !isNonNullable);
								ApplyDescription(prop.AttributeProvider, itemSchema);
								items.Add((JsonNode)itemSchema);
							}

							properties.Add(propertyName, propSchema);
							if (associatedParameter?.IsRequired is true)
							{
								(required ??= []).Add((JsonNode)propertyName);
							}
						}

						this.Pop();

						schema["type"] = items is not null ? new JsonArray(["object", "array"]) : "object";
						schema["properties"] = properties;
						if (items is not null)
						{
							schema["items"] = items;
						}

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
