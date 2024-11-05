// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
/// This one is slower than <see cref="BufferWriter"/> but it is more flexible in that it is not a <see langword="ref" /> struct.
/// </summary>
internal struct BufferMemoryWriter
{
	/// <summary>
	/// The underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	private IBufferWriter<byte> output;

	/// <summary>
	/// The result of the last call to <see cref="IBufferWriter{T}.GetMemory(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
	/// Backing field for the <see cref="Memory"/> property.
	/// </summary>
	private Memory<byte> memory;

	/// <summary>
	/// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
	/// </summary>
	private int buffered;

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferMemoryWriter"/> struct.
	/// </summary>
	/// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal BufferMemoryWriter(IBufferWriter<byte> output)
	{
		this.buffered = 0;
		this.output = output ?? throw new ArgumentNullException(nameof(output));
		this.memory = this.output.GetMemoryCheckResult();
	}

	/// <summary>
	/// Gets the result of the last call to <see cref="IBufferWriter{T}.GetMemory(int)"/>.
	/// </summary>
	internal Memory<byte> Memory => this.memory;

	/// <inheritdoc cref="IBufferWriter{T}.GetSpan(int)"/>
	internal Span<byte> GetSpan(int sizeHint = 0)
	{
		this.Ensure(sizeHint);
		return this.Memory.Span;
	}

	/// <inheritdoc cref="IBufferWriter{T}.GetMemory(int)"/>
	internal Memory<byte> GetMemory(int sizeHint = 0)
	{
		this.Ensure(sizeHint);
		return this.Memory;
	}

	/// <summary>
	/// Gets a reference to the next byte to write to.
	/// </summary>
	/// <param name="sizeHint">The minimum size to guarantee is available in the buffer.</param>
	/// <returns>The first byte in the buffer.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ref byte GetPointer(int sizeHint = 0)
	{
		this.Ensure(sizeHint);
		return ref this.memory.Span[0];
	}

	/// <summary>
	/// Calls <see cref="IBufferWriter{T}.Advance(int)"/> on the underlying writer
	/// with the number of uncommitted bytes.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Commit()
	{
		int buffered = this.buffered;
		if (buffered > 0)
		{
			this.buffered = 0;
			this.output.Advance(buffered);
			this.memory = default;
		}
	}

	/// <summary>
	/// Used to indicate that part of the buffer has been written to.
	/// </summary>
	/// <param name="count">The number of bytes written to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Advance(int count)
	{
		this.buffered += count;
		this.memory = this.memory[count..];
	}

	/// <summary>
	/// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
	/// </summary>
	/// <param name="source">The buffer to copy in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Write(ReadOnlySpan<byte> source)
	{
		if (this.memory.Length >= source.Length)
		{
			source.CopyTo(this.memory.Span);
			this.Advance(source.Length);
		}
		else
		{
			this.WriteMultiBuffer(source);
		}
	}

	/// <summary>
	/// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
	/// </summary>
	/// <param name="count">The number of bytes that must be allocated in a single buffer.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Ensure(int count = 0)
	{
		if (this.memory.Length < count || this.memory.Length == 0)
		{
			this.EnsureMore(count);
		}
	}

	/// <summary>
	/// Gets a fresh span to write to, with an optional minimum size.
	/// </summary>
	/// <param name="count">The minimum size for the next requested buffer.</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void EnsureMore(int count = 0)
	{
		if (this.buffered > 0)
		{
			this.Commit();
		}

		Assumes.NotNull(this.output);
		this.memory = this.output.GetMemoryCheckResult(count);
	}

	/// <summary>
	/// Copies the caller's buffer into this writer, potentially across multiple buffers from the underlying writer.
	/// </summary>
	/// <param name="source">The buffer to copy into this writer.</param>
	private void WriteMultiBuffer(ReadOnlySpan<byte> source)
	{
		int copiedBytes = 0;
		int bytesLeftToCopy = source.Length;
		while (bytesLeftToCopy > 0)
		{
			if (this.memory.Length == 0)
			{
				this.EnsureMore();
			}

			int writable = Math.Min(bytesLeftToCopy, this.memory.Length);
			source.Slice(copiedBytes, writable).CopyTo(this.memory.Span);
			copiedBytes += writable;
			bytesLeftToCopy -= writable;
			this.Advance(writable);
		}
	}
}
