// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.MessagePack;

public interface IMessagePackConverter
{
	object? ReadObject(ref MessagePackReader reader, SerializationContext context);

	[Experimental("NBMsgPackAsync")]
	ValueTask<object?> ReadObjectAsync(MessagePackAsyncReader reader, SerializationContext context);

	[Experimental("NBMsgPackAsync")]
	ValueTask WriteObjectAsync(MessagePackAsyncWriter writer, object? value, SerializationContext context);

	void WriteObject(ref MessagePackWriter writer, object? value, SerializationContext context);

	[Experimental("NBMsgPackAsync")]
	Task<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? indexArg, SerializationContext context);

	[Experimental("NBMsgPackAsync")]
	Task<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context);
}
