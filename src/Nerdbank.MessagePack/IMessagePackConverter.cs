// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An untyped base interface for all message pack converters.
/// </summary>
internal interface IMessagePackConverter
{
}

/// <summary>
/// A base class for all message pack converters.
/// </summary>
/// <typeparam name="T">The data type that can be converted by this object.</typeparam>
public abstract class IMessagePackConverter<T> : IMessagePackConverter
{
	/// <summary>
	/// Serializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	public abstract void Serialize(ref MessagePackWriter writer, ref T? value);

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/>.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <returns>The deserialized value.</returns>
	public abstract T? Deserialize(ref MessagePackReader reader);
}
