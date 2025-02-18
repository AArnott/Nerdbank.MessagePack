// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.Converters;

/// <summary>
/// Provides the raw msgpack transport intended for the <see cref="PreformattedRawBytes"/> type.
/// </summary>
[SuppressMessage("Usage", "NBMsgPack032", Justification = "This converter by design has no idea what msgpack it reads or writes.")]
internal class PreformattedRawBytesConverter : Converter<PreformattedRawBytes>
{
	/// <inheritdoc/>
	/// <remarks>
	/// We always copy the msgpack into another buffer from the buffer we're reading from
	/// because we don't know how long that will last.
	/// For async deserialization, the buffer literally may not even last through the end of deserialization.
	/// And async deserialization may invoke this (synchronous) deserializing method as an optimization,
	/// so we really have no idea whether this buffer will last till the user has a chance to read from it.
	/// </remarks>
	public override PreformattedRawBytes Read(ref Reader reader, SerializationContext context) => new PreformattedRawBytes(reader.ReadRaw(context)).ToOwned();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in PreformattedRawBytes value, SerializationContext context) => writer.Buffer.Write(value.RawBytes);
}
