// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

internal class DelayedConverter<T>(ResultBox<MessagePackConverter<T>> box) : MessagePackConverter<T>
{
	public override T? Deserialize(ref MessagePackReader reader) => box.Result.Deserialize(ref reader);

	public override void Serialize(ref MessagePackWriter writer, ref T? value) => box.Result.Serialize(ref writer, ref value);
}
