// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Simplify type syntax
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Nerdbank.MessagePack;

/// <summary>Serializes the primitive integer type <see cref="SByte"/> as a MessagePack integer.</summary>
internal class SByteConverter : IMessagePackConverter<SByte>
{
	/// <inheritdoc/>
	public override SByte Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadSByte();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref SByte value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="Int16"/> as a MessagePack integer.</summary>
internal class Int16Converter : IMessagePackConverter<Int16>
{
	/// <inheritdoc/>
	public override Int16 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt16();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref Int16 value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="Int32"/> as a MessagePack integer.</summary>
internal class Int32Converter : IMessagePackConverter<Int32>
{
	/// <inheritdoc/>
	public override Int32 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt32();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref Int32 value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="Int64"/> as a MessagePack integer.</summary>
internal class Int64Converter : IMessagePackConverter<Int64>
{
	/// <inheritdoc/>
	public override Int64 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt64();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref Int64 value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="Byte"/> as a MessagePack integer.</summary>
internal class ByteConverter : IMessagePackConverter<Byte>
{
	/// <inheritdoc/>
	public override Byte Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadByte();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref Byte value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="UInt16"/> as a MessagePack integer.</summary>
internal class UInt16Converter : IMessagePackConverter<UInt16>
{
	/// <inheritdoc/>
	public override UInt16 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadUInt16();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref UInt16 value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="UInt32"/> as a MessagePack integer.</summary>
internal class UInt32Converter : IMessagePackConverter<UInt32>
{
	/// <inheritdoc/>
	public override UInt32 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadUInt32();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref UInt32 value, SerializationContext context) => writer.Write(value);
}

/// <summary>Serializes the primitive integer type <see cref="UInt64"/> as a MessagePack integer.</summary>
internal class UInt64Converter : IMessagePackConverter<UInt64>
{
	/// <inheritdoc/>
	public override UInt64 Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadUInt64();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref UInt64 value, SerializationContext context) => writer.Write(value);
}
