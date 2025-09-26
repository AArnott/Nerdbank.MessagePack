// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Contains extension methods to add optional converters.
/// </summary>
/// <remarks>
/// The library comes with many converters.
/// Some are not enabled by default to avoid unnecessary dependencies
/// and to keep a trimmed application size small when it doesn't require them.
/// The extension methods in this class can be used to turn these optional converters on.
/// </remarks>
public static class OptionalConverters
{
	/// <summary>
	/// The msgpack format used to store <see cref="Guid"/> values as strings.
	/// </summary>
	/// <remarks>
	/// By default, <see cref="Guid"/> values are serialized as binary.
	/// This enum can be used with <see cref="WithGuidConverter(MessagePackSerializer, GuidStringFormat)"/>
	/// to serialize them as strings instead.
	/// </remarks>
	public enum GuidStringFormat
	{
		/// <summary>
		/// The <see cref="Guid"/> will be stored as a string in the msgpack stream using the "N" format.
		/// </summary>
		/// <remarks>
		/// An example of this format is "69b942342c9e468b9bae77df7a288e45".
		/// </remarks>
		StringN,

		/// <summary>
		/// The <see cref="Guid"/> will be stored as a string in the msgpack stream using the "D" format,
		/// which is the default format used by <see cref="Guid.ToString()"/>.
		/// </summary>
		/// <remarks>
		/// An example of this format is "69b94234-2c9e-468b-9bae-77df7a288e45".
		/// </remarks>
		StringD,

		/// <summary>
		/// The <see cref="Guid"/> will be stored as a string in the msgpack stream using the "B" format.
		/// </summary>
		/// <remarks>
		/// An example of this format is "{69b94234-2c9e-468b-9bae-77df7a288e45}".
		/// </remarks>
		StringB,

		/// <summary>
		/// The <see cref="Guid"/> will be stored as a string in the msgpack stream using the "P" format.
		/// </summary>
		/// <remarks>
		/// An example of this format is "(69b94234-2c9e-468b-9bae-77df7a288e45)".
		/// </remarks>
		StringP,

		/// <summary>
		/// The <see cref="Guid"/> will be stored as a string in the msgpack stream using the "X" format.
		/// </summary>
		/// <remarks>
		/// An example of this format is "{0x69b94234,0x2c9e,0x468b,{0x9b,0xae,0x77,0xdf,0x7a,0x28,0x8e,0x45}}".
		/// </remarks>
		StringX,
	}

	/// <summary>
	/// Adds converters for common System.Text.Json types, including:
	/// <see cref="JsonNode"/>, <see cref="JsonElement"/>, and <see cref="JsonDocument"/> to the specified serializer.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="serializer"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown if a converter for any of these System.Text.Json types has already been added.</exception>
	public static MessagePackSerializer WithSystemTextJsonConverters(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));

		return serializer with
		{
			Converters = [
				..serializer.Converters,
				new JsonNodeConverter(),
				new JsonElementConverter(),
				new JsonDocumentConverter(),
			],
		};
	}

	/// <summary>
	/// Adds a converter for <see cref="Guid"/> to the specified serializer that serializes GUIDs as strings.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <param name="format">
	/// The string format in which the <see cref="Guid"/> should be written.
	/// All string-based formats can <em>read</em> all other string-based formats.
	/// </param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="format"/> is not one of the allowed values.</exception>
	/// <exception cref="ArgumentException">Thrown if a converter for <see cref="Guid"/> has already been added.</exception>
	/// <remarks>
	/// By default, <see cref="Guid"/> values are serialized in a compact binary format.
	/// This method allows you to override that behavior to serialize them as strings instead.
	/// The <see cref="Guid"/> converter is optimized to avoid allocating strings during the conversion.
	/// </remarks>
	public static MessagePackSerializer WithGuidConverter(this MessagePackSerializer serializer, GuidStringFormat format)
	{
		Requires.NotNull(serializer, nameof(serializer));
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				format switch {
					GuidStringFormat.StringN => new GuidAsStringConverter { Format = 'N' },
					GuidStringFormat.StringD => new GuidAsStringConverter { Format = 'D' },
					GuidStringFormat.StringB => new GuidAsStringConverter { Format = 'B' },
					GuidStringFormat.StringP => new GuidAsStringConverter { Format = 'P' },
					GuidStringFormat.StringX => new GuidAsStringConverter { Format = 'X' },
					_ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
				},
			],
		};
	}

	/// <summary>
	/// Configures the built-in <see cref="DateTime"/> converter to assume a specific <see cref="DateTimeKind"/>
	/// when serializing values that have a <see cref="DateTimeKind.Unspecified"/> kind.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <param name="kind">Either <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Local"/>.</param>
	/// <returns>The modified serializer.</returns>
	/// <remarks>
	/// <para>
	/// By default, serializing a <see cref="DateTime"/> with an <see cref="DateTimeKind.Unspecified"/> kind
	/// throws an exception because the MessagePack format does not permit that kind of ambiguity.
	/// While an explicit <see cref="DateTimeKind"/> is always preferred, this method allows you to
	/// assume a specific <see cref="DateTimeKind"/> for such values.
	/// </para>
	/// <para>
	/// Specifying <see cref="DateTimeKind.Utc"/> will cause unspecified values to be treated as UTC and therefore
	/// serialized without any adjustment.
	/// Specifying <see cref="DateTimeKind.Local"/> will cause unspecified values to be treated as local time and
	/// therefore adjusted to UTC time during serialization.
	/// </para>
	/// <para>
	/// The serialized form will always be UTC (per the MessagePack encoding spec),
	/// and a deserialized <see cref="DateTime"/> will always have the <see cref="DateTimeKind"/>
	/// set to <see cref="DateTimeKind.Utc" />.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentException">Thrown if this method has already been called on the given <paramref name="serializer"/> or a custom <see cref="DateTime"/> converter has already been set.</exception>
	public static MessagePackSerializer WithAssumedDateTimeKind(this MessagePackSerializer serializer, DateTimeKind kind)
	{
		Requires.NotNull(serializer, nameof(serializer));
		Requires.Argument(kind is DateTimeKind.Utc or DateTimeKind.Local, nameof(kind), "Only UTC and Local DateTimeKind values are supported.");
		Assumes.True(PrimitiveConverterLookup.TryGetPrimitiveConverter(ReferencePreservationMode.Off, out MessagePackConverter<DateTime>? builtin));
		return serializer with
		{
			Converters = [
				..serializer.Converters.Except([builtin]),
				new DateTimeConverter { UnspecifiedKindAssumption = kind },
			],
		};
	}

	/// <summary>
	/// Replaces the built-in <see cref="DateTime" /> converter that uses msgpack standard timestamp encoding
	/// with a proprietary converter that preserves <see cref="DateTime.Kind"/> for <see cref="DateTime"/> values.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The msgpack encoding standard mandates that all timestamps are encoded as UTC.
	/// This means all deserialized <see cref="DateTime"/> values have their <see cref="DateTime.Kind"/> set to <see cref="DateTimeKind.Utc"/>,
	/// and forces serialization to throw if <see cref="DateTimeKind.Unspecified"/> is encountered
	/// (unless <see cref="WithAssumedDateTimeKind(MessagePackSerializer, DateTimeKind)"/> is used).
	/// </para>
	/// <para>
	/// With this method, <see cref="DateTime" /> values are serialized such that their <see cref="DateTime.Kind"/>
	/// is preserved.
	/// When the <see cref="DateTime.Kind"/> is <see cref="DateTimeKind.Utc"/>, the value is stored as a msgpack timestamp.
	/// For other kinds, a msgpack array of two elements is used: <c>[ticks, kind]</c>.
	/// </para>
	/// <para>
	/// Note that serializing anything other than <see cref="DateTimeKind.Utc"/> values is <em>not</em> recommended
	/// both because it is not interoperable with other msgpack implementations and (more importantly)
	/// because the party that deserializes the value may not be in the same time zone (even if the user and machine is the same)
	/// and thus could end up with a different time interpretation than the one that was serialized.
	/// </para>
	/// </remarks>
	/// <param name="serializer">The existing MessagePackSerializer to configure. Cannot be null.</param>
	/// <returns>A new MessagePackSerializer instance with high-fidelity DateTime support enabled.</returns>
	/// <seealso cref="WithAssumedDateTimeKind(MessagePackSerializer, DateTimeKind)"/>
	public static MessagePackSerializer WithHiFiDateTime(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));
		Assumes.True(PrimitiveConverterLookup.TryGetPrimitiveConverter(ReferencePreservationMode.Off, out MessagePackConverter<DateTime>? builtin));
		return serializer with
		{
			Converters = [
				..serializer.Converters.Except([builtin]),
				new HiFiDateTimeConverter(),
			],
		};
	}

	/// <summary>
	/// Adds a converter for <see cref="ExpandoObject"/> to the specified serializer.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentException">Thrown if a converter for <see cref="ExpandoObject"/> has already been added.</exception>
	/// <remarks>
	/// <para>
	/// This can only <em>serialize</em> an <see cref="ExpandoObject"/>
	/// whose properties are values or objects whose runtime type
	/// has a shape available as provided by <see cref="SerializationContext.TypeShapeProvider"/>.
	/// </para>
	/// <para>
	/// This can <em>deserialize</em> any msgpack map.
	/// Nested maps included in the deserialized graph will be deserialized as dictionaries that support C# <c>dynamic</c> access to their members,
	/// similar to <see cref="ExpandoObject"/> but with read-only access.
	/// </para>
	/// </remarks>
	public static MessagePackSerializer WithExpandoObjectConverter(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				ExpandoObjectConverter.Instance,
			],
		};
	}

	/// <summary>
	/// Adds a converter to the specified serializer
	/// that can write objects with a declared type of <see cref="object"/> based on their runtime type
	/// (provided a type shape is available for the runtime type),
	/// and can deserialize them based on their msgpack token types into primitives, dictionaries and arrays.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentException">Thrown if a converter for <see cref="object"/> has already been added.</exception>
	/// <inheritdoc cref="PrimitivesAsObjectConverter" path="/remarks"/>
	/// <remarks>
	/// Deserialized arrays will be typed as <see cref="object"/> arrays.
	/// Deserialized dictionaries will be typed with <see cref="object"/> keys and values.
	/// </remarks>
	/// <seealso cref="WithDynamicObjectConverter(MessagePackSerializer)"/>
	public static MessagePackSerializer WithObjectConverter(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				new PrimitivesAsObjectConverter(),
			],
		};
	}

	/// <summary>
	/// Adds a converter to the specified serializer
	/// that can write objects with a declared type of <see cref="object"/> based on their runtime type
	/// (provided a type shape is available for the runtime type),
	/// and can deserialize them based on their msgpack token types into primitives, dictionaries and arrays.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentException">Thrown if a converter for <see cref="object"/> has already been added.</exception>
	/// <remarks>
	/// This converter is very similar to the one added by <see cref="WithObjectConverter(MessagePackSerializer)"/>,
	/// except that the deserialized result can be used with the C# <c>dynamic</c> keyword where the content
	/// of maps can also be accessed using <see langword="string"/> keys as if they were properties.
	/// </remarks>
	public static MessagePackSerializer WithDynamicObjectConverter(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				PrimitivesAsDynamicConverter.Instance,
			],
		};
	}
}
