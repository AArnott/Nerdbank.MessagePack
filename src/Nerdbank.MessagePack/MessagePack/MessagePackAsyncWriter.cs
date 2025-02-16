// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// A primitive types writer for the MessagePack format that writes to a <see cref="PipeWriter"/>.
/// </summary>
/// <param name="pipeWriter">The pipe writer to encode to.</param>
/// <remarks>
/// <para>
/// This is an async capable and slower alternative to <see cref="MessagePackWriter"/> with fewer methods,
/// making the sync version more generally useful.
/// It is useful when implementing the async virtual methods on <see cref="MessagePackConverter{T}"/>.
/// </para>
/// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
/// </remarks>
[Experimental("NBMsgPackAsync")]
public class MessagePackAsyncWriter(PipeWriter pipeWriter) : AsyncWriter(pipeWriter, MessagePackSerializer.Formatter)
{
	/// <inheritdoc cref="MessagePackWriter.WriteNil"/>
	public void WriteNil() => this.WriteNull();

	/// <inheritdoc cref="MessagePackWriter.WriteArrayHeader(int)"/>
	public void WriteArrayHeader(int count) => this.WriteArrayHeader(checked((uint)count));

	/// <inheritdoc cref="MessagePackWriter.WriteArrayHeader(uint)"/>
	public void WriteArrayHeader(uint count)
	{
		Span<byte> span = this.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteArrayHeader(span, count, out int written));
		this.Buffer.Advance(written);
	}

	/// <inheritdoc cref="MessagePackWriter.WriteMapHeader(int)"/>
	public void WriteMapHeader(int count) => this.WriteMapHeader(checked((uint)count));

	/// <inheritdoc cref="MessagePackWriter.WriteMapHeader(uint)"/>
	public void WriteMapHeader(uint count)
	{
		Span<byte> span = this.Buffer.GetSpan(5);
		Assumes.True(MessagePackPrimitives.TryWriteMapHeader(span, count, out int written));
		this.Buffer.Advance(written);
	}
}
