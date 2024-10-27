// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

public class MessagePackSerializationException : Exception
{
	public MessagePackSerializationException(string? message) : base(message)
	{
	}

	public MessagePackSerializationException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}
