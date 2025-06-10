// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
	/// The msgpack format used to store <see cref="Guid"/> values.
	/// </summary>
	public enum GuidFormat
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

		/// <summary>
		/// The <see cref="Guid"/> will be stored in a compact 16 byte binary representation, in little endian order.
		/// </summary>
		BinaryLittleEndian,
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
	/// Adds a converter for <see cref="Guid"/> to the specified serializer.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <param name="format">The format in which the <see cref="Guid"/> should be written.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="format"/> is not one of the allowed values.</exception>
	/// <exception cref="ArgumentException">Thrown if a converter for <see cref="Guid"/> has already been added.</exception>
	/// <remarks>
	/// The <see cref="Guid"/> converter is optimized to avoid allocating strings during the conversion.
	/// </remarks>
	public static MessagePackSerializer WithGuidConverter(this MessagePackSerializer serializer, GuidFormat format = GuidFormat.BinaryLittleEndian)
	{
		Requires.NotNull(serializer, nameof(serializer));
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				format switch {
					GuidFormat.StringN => new GuidAsStringConverter { Format = 'N' },
					GuidFormat.StringD => new GuidAsStringConverter { Format = 'D' },
					GuidFormat.StringB => new GuidAsStringConverter { Format = 'B' },
					GuidFormat.StringP => new GuidAsStringConverter { Format = 'P' },
					GuidFormat.StringX => new GuidAsStringConverter { Format = 'X' },
					GuidFormat.BinaryLittleEndian => GuidAsLittleEndianBinaryConverter.Instance,
					_ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
				},
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
	/// This can <em>deserialize</em> anything, but can only <em>serialize</em> object graphs for which every runtime type
	/// has a shape available as provided by <see cref="SerializationContext.TypeShapeProvider"/>.
	/// </para>
	/// </remarks>
	[RequiresDynamicCode(Reasons.DynamicObject)]
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
	public static MessagePackSerializer WithObjectPrimitiveConverter(this MessagePackSerializer serializer)
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
	/// This converter is very similar to the one added by <see cref="WithObjectPrimitiveConverter(MessagePackSerializer)"/>,
	/// except that the deserialized result can be used with the C# <c>dynamic</c> keyword where the content
	/// of maps can also be accessed using <see langword="string"/> keys as if they were properties.
	/// </remarks>
	[RequiresDynamicCode(Reasons.DynamicObject)]
	public static MessagePackSerializer WithObjectDynamicConverter(this MessagePackSerializer serializer)
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
