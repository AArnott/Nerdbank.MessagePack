// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

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
internal class EnumAsStringConverter<TEnum, TUnderlyingType> : MessagePackConverter<TEnum>
	where TEnum : struct, Enum
	where TUnderlyingType : unmanaged
{
	private readonly SpanDictionary<byte, TUnderlyingType> valueByUtf8Name;
	private readonly Dictionary<string, TUnderlyingType> valueByName;
	private readonly Dictionary<TUnderlyingType, ReadOnlyMemory<byte>> msgpackEncodedNameByValue = new();
	private readonly MessagePackConverter<TUnderlyingType> primitiveConverter;

	/// <summary>
	/// Initializes a new instance of the <see cref="EnumAsStringConverter{TEnum, TUnderlyingType}"/> class.
	/// </summary>
	/// <param name="primitiveConverter">The converter for the primitive underlying type.</param>
	/// <param name="members">A map of the enum members.</param>
	public EnumAsStringConverter(MessagePackConverter<TUnderlyingType> primitiveConverter, IReadOnlyDictionary<string, TUnderlyingType> members)
	{
		this.primitiveConverter = primitiveConverter;

		// First try in case-insensitive mode.
		this.valueByName = new(StringComparer.OrdinalIgnoreCase);
		if (!TryPopulateDictionary())
		{
			// Fallback to case sensitive mode.
			this.valueByName = new(StringComparer.Ordinal);
			this.msgpackEncodedNameByValue.Clear();
			Assumes.True(TryPopulateDictionary(), $"Failed to populate enum fields from {typeof(TEnum).FullName}.");
		}

		this.valueByUtf8Name = new SpanDictionary<byte, TUnderlyingType>(this.valueByName.Select(n => new KeyValuePair<ReadOnlyMemory<byte>, TUnderlyingType>(StringEncoding.UTF8.GetBytes(n.Key), n.Value)), ByteSpanEqualityComparer.Ordinal);

		bool TryPopulateDictionary()
		{
			bool nonUniqueNameDetected = false;
			foreach (KeyValuePair<string, TUnderlyingType> pair in members)
			{
				if (!this.valueByName.TryAdd(pair.Key, pair.Value))
				{
					if (!EqualityComparer<TUnderlyingType>.Default.Equals(this.valueByName[pair.Key], pair.Value))
					{
						// Unique values assigned to names that collided, apparently only by case.
						return false;
					}

					nonUniqueNameDetected = true;
				}

				// Values may be assigned to multiple names, so we don't guarantee uniqueness.
				StringEncoding.GetEncodedStringBytes(pair.Key, out _, out ReadOnlyMemory<byte> msgpackEncodedName);
				this.msgpackEncodedNameByValue.TryAdd(pair.Value, msgpackEncodedName);
			}

			if (nonUniqueNameDetected)
			{
				// Enumerate values and ensure we have all the names indexed so we can deserialize them.
				foreach (KeyValuePair<string, TUnderlyingType> pair in members)
				{
					this.valueByName.TryAdd(pair.Key, pair.Value);
				}
			}

			return true;
		}
	}

	/// <inheritdoc/>
	public override TEnum Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.NextMessagePackType == MessagePackType.String)
		{
			string stringValue;
			TUnderlyingType value;

			// Try to avoid any allocations by reading the string as a span.
			// This only works for case sensitive matches, so be prepared to fallback to string comparisons.
			if (reader.TryReadStringSpan(out ReadOnlySpan<byte> span))
			{
				if (this.valueByUtf8Name.TryGetValue(span, out value))
				{
					return (TEnum)(object)value; // The JIT will optimize the boxing away.
				}

				stringValue = StringEncoding.UTF8.GetString(span);
			}
			else
			{
				stringValue = reader.ReadString()!;
			}

			if (this.valueByName.TryGetValue(stringValue, out value))
			{
				return (TEnum)(object)value; // The JIT will optimize the boxing away.
			}
			else
			{
				throw new MessagePackSerializationException($"Unrecognized enum value name: \"{stringValue}\".");
			}
		}
		else
		{
			return (TEnum)(object)this.primitiveConverter.Read(ref reader, context)!;
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TEnum value, SerializationContext context)
	{
		TUnderlyingType typedValue = (TUnderlyingType)(object)value;
		if (this.msgpackEncodedNameByValue.TryGetValue(typedValue, out ReadOnlyMemory<byte> name))
		{
			writer.WriteRaw(name.Span);
		}
		else
		{
			this.primitiveConverter.Write(ref writer, typedValue, context);
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
