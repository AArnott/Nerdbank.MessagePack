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
public class MessagePackAsyncWriter(PipeWriter pipeWriter) : AsyncWriter(pipeWriter, MsgPackFormatter.Instance)
{
	/// <summary>
	/// The delegate type that may be provided to the <see cref="Write{TState}(SyncWriter{TState}, TState)"/> method.
	/// </summary>
	/// <typeparam name="T">The type of state that may be given to the writer.</typeparam>
	/// <param name="writer">The delegate to invoke to do the synchronous writing.</param>
	/// <param name="state">The state to pass to the <paramref name="writer"/>.</param>
	public delegate void SyncWriter<T>(ref MessagePackWriter writer, T state);

	/// <summary>
	/// Gets the fully-capable, synchronous writer.
	/// </summary>
	/// <returns>The writer.</returns>
	/// <remarks>
	/// The caller must take care to call <see cref="ReturnWriter(ref MessagePackWriter)"/> before discarding the writer.
	/// </remarks>
	public MessagePackWriter CreateWriter()
	{
#if !NET
		// ref fields are not supported on .NET Framework, so we have to prepare to copy the struct.
		this.Buffer.Commit();
#endif

		return new(new BufferWriter(ref this.Buffer));
	}

	/// <summary>
	/// Applies the bytes written with a writer previously obtained from <see cref="CreateWriter"/> back to this object.
	/// </summary>
	/// <param name="writer">The writer to return. It should not be used after this.</param>
	public void ReturnWriter(ref MessagePackWriter writer)
	{
		writer.Flush();

#if !NET
		// ref fields are not supported on .NET Framework, so we have to copy the struct since it'll disappear.
		this.Buffer = writer.Writer.BufferMemoryWriter;
#endif

		// Help prevent misuse of the writer after it's been returned.
		writer = default;
	}

	/// <summary>
	/// Creates a sync writer for purposes of serializing a message.
	/// </summary>
	/// <typeparam name="TState">The type of state that may be given to the writer.</typeparam>
	/// <param name="writer">The delegate to invoke to do the synchronous writing.</param>
	/// <param name="state">State to pass to the writer.</param>
	public void Write<TState>(SyncWriter<TState> writer, TState state)
	{
		Requires.NotNull(writer);

		MessagePackWriter syncWriter = this.CreateWriter();
		writer(ref syncWriter, state);
		this.ReturnWriter(ref syncWriter);
	}

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

	/// <inheritdoc cref="MessagePackWriter.WriteRaw(ReadOnlySpan{byte})"/>
	public void WriteRaw(ReadOnlySpan<byte> bytes)
	{
		MessagePackWriter writer = this.CreateWriter();
		writer.WriteRaw(bytes);
		this.ReturnWriter(ref writer);
	}
}
