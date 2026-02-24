// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Options for the object converter added by <see cref="OptionalConverters.WithObjectConverter(MessagePackSerializer, ObjectConverterOptions)"/>.
/// </summary>
public struct ObjectConverterOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether integer types are preserved during serialization and deserialization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see langword="true"/>, integers are serialized using the msgpack encoding that matches their .NET type's width
	/// (e.g. <see cref="int"/> uses <see cref="MessagePackCode.Int32"/>).
	/// This allows the original integer type to be recovered during deserialization based on the msgpack encoding.
	/// </para>
	/// <para>
	/// When <see langword="false"/> (the default), integers are serialized using the most compact msgpack encoding,
	/// and deserialized as <see cref="ulong"/> (or <see cref="long"/> if the value is negative).
	/// </para>
	/// <para>
	/// Note that only msgpack data written with this option enabled can guarantee type preservation.
	/// Data received from other sources may use compact encodings, in which case the integer
	/// will be deserialized based on the encoding actually present in the msgpack data.
	/// </para>
	/// </remarks>
	public bool PreserveIntegers { get; set; }
}
