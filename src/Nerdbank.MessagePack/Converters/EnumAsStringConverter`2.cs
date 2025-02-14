// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// Serializes <see langword="enum" /> types as a string if possible.
/// When no string is assigned to a particular value, the ordinal value is used.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <typeparam name="TUnderlyingType">The underlying integer type.</typeparam>
/// <remarks>
/// Upon deserialization, the enum value name match will be case insensitive
/// unless the enum type defines multiple values with names that are only distinguished by case.
/// </remarks>
internal class EnumAsStringConverter<TEnum, TUnderlyingType> : Converter<TEnum>
	where TEnum : struct, Enum
{
	private readonly SpanDictionary<byte, TEnum> valueByUtf8Name;
	private readonly Dictionary<string, TEnum> valueByName = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<TEnum, ReadOnlyMemory<byte>> msgpackEncodedNameByValue = new();
	private readonly Converter<TUnderlyingType> primitiveConverter;

	/// <summary>
	/// Initializes a new instance of the <see cref="EnumAsStringConverter{TEnum, TUnderlyingType}"/> class.
	/// </summary>
	/// <param name="primitiveConverter">The converter for the primitive underlying type.</param>
	public EnumAsStringConverter(Converter<TUnderlyingType> primitiveConverter, Formatter formatter)
	{
		this.primitiveConverter = primitiveConverter;

		if (!TryPopulateDictionary())
		{
			// Fallback to case sensitive mode.
			this.valueByName = new(StringComparer.Ordinal);
			this.msgpackEncodedNameByValue.Clear();
			Assumes.True(TryPopulateDictionary(), $"Failed to populate enum fields from {typeof(TEnum).FullName}.");
		}

		this.valueByUtf8Name = new SpanDictionary<byte, TEnum>(this.valueByName.Select(n => new KeyValuePair<ReadOnlyMemory<byte>, TEnum>(formatter.Encoding.GetBytes(n.Key), n.Value)), ByteSpanEqualityComparer.Ordinal);

		bool TryPopulateDictionary()
		{
			bool nonUniqueNameDetected = false;
#if NET
			foreach (TEnum value in Enum.GetValues<TEnum>())
#else
			foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
#endif
			{
#if NET
				if (Enum.GetName(value) is string name)
#else
				if (Enum.GetName(typeof(TEnum), value) is string name)
#endif
				{
					if (!this.valueByName.TryAdd(name, value))
					{
						if (!EqualityComparer<TEnum>.Default.Equals(this.valueByName[name], value))
						{
							// Unique values assigned to names that collided, apparently only by case.
							return false;
						}

						nonUniqueNameDetected = true;
					}

					// Values may be assigned to multiple names, so we don't guarantee uniqueness.
					formatter.GetEncodedStringBytes(name, out _, out ReadOnlyMemory<byte> msgpackEncodedName);
					this.msgpackEncodedNameByValue.TryAdd(value, msgpackEncodedName);
				}
			}

			if (nonUniqueNameDetected)
			{
				// Enumerate values and ensure we have all the names indexed so we can deserialize them.
#if NET
				foreach (string name in Enum.GetNames<TEnum>())
				{
					this.valueByName.TryAdd(name, Enum.Parse<TEnum>(name));
				}
#else
				foreach (string name in Enum.GetNames(typeof(TEnum)))
				{
					Assumes.True(Enum.TryParse(name, out TEnum value));
					this.valueByName.TryAdd(name, value);
				}
#endif
			}

			return true;
		}
	}

	/// <inheritdoc/>
	public override TEnum Read(ref Reader reader, SerializationContext context)
	{
		if (reader.NextTypeCode == TypeCode.String)
		{
			string stringValue;
			TEnum value;

			// Try to avoid any allocations by reading the string as a span.
			// This only works for case sensitive matches, so be prepared to fallback to string comparisons.
			if (reader.TryReadStringSpan(out ReadOnlySpan<byte> span))
			{
				if (this.valueByUtf8Name.TryGetValue(span, out value))
				{
					return value;
				}

				stringValue = reader.Deformatter.Encoding.GetString(span);
			}
			else
			{
				stringValue = reader.ReadString()!;
			}

			if (this.valueByName.TryGetValue(stringValue, out value))
			{
				return value;
			}
			else
			{
				throw new SerializationException($"Unrecognized enum value name: \"{stringValue}\".");
			}
		}
		else
		{
			return (TEnum)(object)this.primitiveConverter.Read(ref reader, context)!;
		}
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TEnum value, SerializationContext context)
	{
		if (this.msgpackEncodedNameByValue.TryGetValue(value, out ReadOnlyMemory<byte> name))
		{
			writer.Buffer.Write(name.Span);
		}
		else
		{
			this.primitiveConverter.Write(ref writer, (TUnderlyingType)(object)value, context);
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new JsonObject
		{
			["oneOf"] = new JsonArray(
				new JsonObject { ["type"] = "integer" },
				new JsonObject
				{
					["type"] = "string",
					["enum"] = new JsonArray([.. this.valueByName.Keys]),
				}),
		};
}
