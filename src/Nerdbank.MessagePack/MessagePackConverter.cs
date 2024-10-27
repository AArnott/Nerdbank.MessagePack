// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

internal interface IMessagePackConverter { }

public abstract class MessagePackConverter<T> : IMessagePackConverter
{
	public abstract void Serialize(ref MessagePackWriter writer, ref T? value);

	public abstract T? Deserialize(ref MessagePackReader reader);
}
