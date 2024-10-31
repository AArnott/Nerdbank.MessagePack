﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
internal class StringConverter : MessagePackConverter<string>
{
	/// <inheritdoc/>
	public override string? Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadString();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref string? value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="bool"/>.
/// </summary>
internal class BooleanConverter : MessagePackConverter<bool>
{
	/// <inheritdoc/>
	public override bool Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadBoolean();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref bool value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="float"/>.
/// </summary>
internal class SingleConverter : MessagePackConverter<float>
{
	/// <inheritdoc/>
	public override float Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadSingle();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref float value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="double"/>.
/// </summary>
internal class DoubleConverter : MessagePackConverter<double>
{
	/// <inheritdoc/>
	public override double Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadDouble();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref double value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes <see cref="DateTime"/> values.
/// </summary>
internal class DateTimeConverter : MessagePackConverter<DateTime>
{
	/// <inheritdoc/>
	public override DateTime Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadDateTime();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref DateTime value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes <see cref="char"/> values.
/// </summary>
internal class CharConverter : MessagePackConverter<char>
{
	/// <inheritdoc/>
	public override char Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadChar();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref char value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes <see cref="byte"/> array values.
/// </summary>
internal class ByteArrayConverter : MessagePackConverter<byte[]?>
{
	/// <inheritdoc/>
	public override byte[]? Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadBytes()?.ToArray();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref byte[]? value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a nullable value type.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="elementConverter">The converter to use when the value is not null.</param>
internal class NullableConverter<T>(MessagePackConverter<T> elementConverter) : MessagePackConverter<T?>
	where T : struct
{
	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref T? value, SerializationContext context)
	{
		if (value.HasValue)
		{
			T nonnullValue = value.Value;
			elementConverter.Serialize(ref writer, ref nonnullValue, context);
		}
		else
		{
			writer.WriteNil();
		}
	}

	/// <inheritdoc/>
	public override T? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		return elementConverter.Deserialize(ref reader, context);
	}
}