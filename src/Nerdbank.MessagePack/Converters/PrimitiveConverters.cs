// Copyright (c) Andrew Arnott. All rights reserved.
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
	public string? Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadString();

	/// <inheritdoc/>
	public void Serialize(ref MessagePackWriter writer, ref string? value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="bool"/>.
/// </summary>
internal class BooleanConverter : IMessagePackConverter<bool>
{
	/// <inheritdoc/>
	public bool Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadBoolean();

	/// <inheritdoc/>
	public void Serialize(ref MessagePackWriter writer, ref bool value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="float"/>.
/// </summary>
internal class SingleConverter : IMessagePackConverter<float>
{
	/// <inheritdoc/>
	public float Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadSingle();

	/// <inheritdoc/>
	public void Serialize(ref MessagePackWriter writer, ref float value, SerializationContext context) => writer.Write(value);
}

/// <summary>
/// Serializes a <see cref="double"/>.
/// </summary>
internal class DoubleConverter : IMessagePackConverter<double>
{
	/// <inheritdoc/>
	public double Deserialize(ref MessagePackReader reader, SerializationContext context) => reader.ReadDouble();

	/// <inheritdoc/>
	public void Serialize(ref MessagePackWriter writer, ref double value, SerializationContext context) => writer.Write(value);
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
	public void Serialize(ref MessagePackWriter writer, ref T? value, SerializationContext context)
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
	public T? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		return elementConverter.Deserialize(ref reader, context);
	}
}
