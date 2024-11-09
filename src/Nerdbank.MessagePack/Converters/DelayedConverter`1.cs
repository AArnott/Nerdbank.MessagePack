// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter that defers to another converter that is not yet available.
/// </summary>
/// <typeparam name="T">The convertible data type.</typeparam>
/// <param name="box">A box containing the not-yet-done converter.</param>
internal class DelayedConverter<T>(ResultBox<MessagePackConverter<T>> box) : MessagePackConverter<T>
{
	/// <inheritdoc/>
	public override T? Deserialize(ref MessagePackReader reader, SerializationContext context) => box.Result.Deserialize(ref reader, context);

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, in T? value, SerializationContext context) => box.Result.Serialize(ref writer, value, context);
}
