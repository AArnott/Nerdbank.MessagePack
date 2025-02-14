// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Simplify type syntax
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Nerdbank.PolySerializer.Converters;

/// <summary>Serializes the primitive integer type <see cref="SByte"/>.</summary>
internal class SByteConverter : Converter<SByte>
{
	/// <inheritdoc/>
	public override SByte Read(ref Reader reader, SerializationContext context) => reader.ReadSByte();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in SByte value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="Int16"/>.</summary>
internal class Int16Converter : Converter<Int16>
{
	/// <inheritdoc/>
	public override Int16 Read(ref Reader reader, SerializationContext context) => reader.ReadInt16();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Int16 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="Int32"/>.</summary>
internal class Int32Converter : Converter<Int32>
{
	/// <inheritdoc/>
	public override Int32 Read(ref Reader reader, SerializationContext context) => reader.ReadInt32();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Int32 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="Int64"/>.</summary>
internal class Int64Converter : Converter<Int64>
{
	/// <inheritdoc/>
	public override Int64 Read(ref Reader reader, SerializationContext context) => reader.ReadInt64();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Int64 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="Byte"/>.</summary>
internal class ByteConverter : Converter<Byte>
{
	/// <inheritdoc/>
	public override Byte Read(ref Reader reader, SerializationContext context) => reader.ReadByte();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in Byte value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="UInt16"/>.</summary>
internal class UInt16Converter : Converter<UInt16>
{
	/// <inheritdoc/>
	public override UInt16 Read(ref Reader reader, SerializationContext context) => reader.ReadUInt16();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in UInt16 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="UInt32"/>.</summary>
internal class UInt32Converter : Converter<UInt32>
{
	/// <inheritdoc/>
	public override UInt32 Read(ref Reader reader, SerializationContext context) => reader.ReadUInt32();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in UInt32 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}

/// <summary>Serializes the primitive integer type <see cref="UInt64"/>.</summary>
internal class UInt64Converter : Converter<UInt64>
{
	/// <inheritdoc/>
	public override UInt64 Read(ref Reader reader, SerializationContext context) => reader.ReadUInt64();

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in UInt64 value, SerializationContext context) => writer.Write(value);

	/// <inheritdoc/>
	public override System.Text.Json.Nodes.JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => new() { ["type"] = "integer" };
}
