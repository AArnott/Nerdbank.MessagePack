// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter that defers to another converter that is not yet available.
/// </summary>
/// <typeparam name="T">The convertible data type.</typeparam>
/// <param name="box">A box containing the not-yet-done converter.</param>
internal class DelayedConverter<T>(ResultBox<IMessagePackConverter<T>> box) : IMessagePackConverter<T>
{
	/// <inheritdoc/>
	public override T? Deserialize(ref MessagePackReader reader) => box.Result.Deserialize(ref reader);

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref T? value) => box.Result.Serialize(ref writer, ref value);
}
