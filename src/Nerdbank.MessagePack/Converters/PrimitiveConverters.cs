﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single class

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a <see cref="string"/>.
/// </summary>
internal class StringConverter : IMessagePackConverter<string>
{
	/// <inheritdoc/>
	public override string? Deserialize(ref MessagePackReader reader) => reader.ReadString();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref string? value) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="bool"/>.
/// </summary>
internal class BooleanConverter : IMessagePackConverter<bool>
{
	/// <inheritdoc/>
	public override bool Deserialize(ref MessagePackReader reader) => reader.ReadBoolean();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref bool value) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="float"/>.
/// </summary>
internal class SingleConverter : IMessagePackConverter<float>
{
	/// <inheritdoc/>
	public override float Deserialize(ref MessagePackReader reader) => reader.ReadSingle();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref float value) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="double"/>.
/// </summary>
internal class DoubleConverter : IMessagePackConverter<double>
{
	/// <inheritdoc/>
	public override double Deserialize(ref MessagePackReader reader) => reader.ReadDouble();

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref double value) => writer.Write(value);
}

/// <summary>
/// Serializes a nullable value type.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
/// <param name="elementConverter">The converter to use when the value is not null.</param>
internal class NullableConverter<T>(IMessagePackConverter<T> elementConverter) : IMessagePackConverter<T?>
	where T : struct
{
	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref T? value)
	{
		if (value.HasValue)
		{
			T nonnullValue = value.Value;
			elementConverter.Serialize(ref writer, ref nonnullValue);
		}
		else
		{
			writer.WriteNil();
		}
	}

	/// <inheritdoc/>
	public override T? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		return elementConverter.Deserialize(ref reader);
	}
}