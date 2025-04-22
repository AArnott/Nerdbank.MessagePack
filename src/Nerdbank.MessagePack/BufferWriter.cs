// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
/// </summary>
internal struct BufferWriter
{
	/// <summary>
	/// The underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	private IBufferWriter<byte>? output;

	/// <summary>
	/// The result of the last call to <see cref="IBufferWriter{T}.GetMemory(int)"/>.
	/// </summary>
	private Memory<byte> memory;

	/// <summary>
	/// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
	/// </summary>
	private int buffered;

	private SequencePool<byte>? sequencePool;

	private SequencePool<byte>.Rental rental;

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferWriter"/> struct.
	/// </summary>
	/// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
	/// <remarks>
	/// Results of writing will be available on <paramref name="output"/> after calling <see cref="Commit"/>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal BufferWriter(IBufferWriter<byte> output)
	{
		this.output = output;
		this.memory = this.output.GetMemoryCheckResult();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferWriter"/> struct.
	/// </summary>
	/// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
	/// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
	/// <remarks>
	/// Results of writing will be available by calling <see cref="ToArray"/>.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal BufferWriter(SequencePool<byte> sequencePool, byte[] array)
	{
		this.sequencePool = sequencePool;
		this.memory = array;
	}

	/// <summary>
	/// Gets the remaining span that can be written to without interacting with an underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	internal Span<byte> Span => this.memory.Span[this.buffered..];

	/// <summary>
	/// Gets the number of bytes written but not yet <see cref="Commit">committed</see>.
	/// </summary>
	internal int UncommittedBytes => this.buffered;

	/// <inheritdoc cref="IBufferWriter{T}.GetSpan(int)"/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal Span<byte> GetSpan(int sizeHint = 0)
	{
		this.Ensure(sizeHint);
		return this.Span;
	}

	/// <summary>
	/// Calls <see cref="IBufferWriter{T}.Advance(int)"/> on the underlying writer
	/// with the number of uncommitted bytes.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Commit()
	{
		// We only need to commit something if we have a writer
		// and we have a buffer from it.
		// In that case, we MUST call Advance (even with 0) to assure the caller
		// that the IBufferWriter<byte> is not in the state between issuing a buffer and a call to Advance.
		if (this.output is not null && !this.memory.IsEmpty)
		{
			this.output.Advance(this.buffered);
			this.memory = default;
			this.buffered = 0;
		}
	}

	/// <summary>
	/// Used to indicate that part of the buffer has been written to.
	/// </summary>
	/// <param name="count">The number of bytes written to.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Advance(int count)
	{
		int buffered = this.buffered + count;
		if (buffered > this.memory.Length)
		{
			Requires.FailRange(nameof(count), "Exceeds remaining buffer.");
		}

		this.buffered = buffered;
	}

	/// <summary>
	/// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
	/// </summary>
	/// <param name="source">The buffer to copy in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Write(ReadOnlySpan<byte> source)
	{
		Span<byte> span = this.Span;
		if (span.Length >= source.Length)
		{
			source.CopyTo(span);
			this.buffered += source.Length;
		}
		else
		{
			this.WriteMultiBuffer(source);
		}
	}

	/// <summary>
	/// Returns a rented <see cref="Sequence{T}"/>.
	/// This struct must not be used after calling this method.
	/// </summary>
	internal void Dispose()
	{
		this.rental.Dispose();

		// Clear all fields so that further use of this struct will likely throw NRE.
		this = default;
	}

	/// <summary>
	/// Returns a new array with all written bytes copied into it.
	/// </summary>
	/// <returns>The new array.</returns>
	/// <exception cref="InvalidOperationException">Thrown if this value was not constructed with <see cref="BufferWriter(SequencePool{byte}, byte[])"/>.</exception>
	internal byte[] ToArray()
	{
		// Only the pool constructor guarantees that our IBufferWriter<byte> is actually a
		// Sequence<byte> which we can get all written bytes from.
		if (this.sequencePool is null && this.rental.Value is null)
		{
			throw new InvalidOperationException("This instance was not initialized to support this operation.");
		}

		// If we haven't got a buffer writer, then all bytes are in our scratch array.
		if (this.output is null)
		{
			return this.memory[..this.buffered].ToArray();
		}

		this.Commit();
		return this.rental.Value.AsReadOnlySequence.ToArray();
	}

	/// <summary>
	/// Acquires a new buffer if necessary to ensure that some given number of bytes can be written to a single buffer.
	/// </summary>
	/// <param name="count">The number of bytes that must be allocated in a single buffer.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Ensure(int count = 0)
	{
		Span<byte> span = this.Span;
		if (span.Length < count || span.Length == 0)
		{
			this.EnsureMore(count);
		}
	}

	/// <summary>
	/// Gets a fresh span to write to, with an optional minimum size.
	/// </summary>
	/// <param name="sizeHint">The minimum size for the next requested buffer.</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void EnsureMore(int sizeHint = 0)
	{
		if (this.output is null)
		{
			// We're going to need an IBufferWriter<byte> at this point to get a new buffer.
			Debug.Assert(this.sequencePool is not null, "Invalid internal state.");
			this.rental = this.sequencePool!.Rent();
			this.sequencePool = null;
			this.output = this.rental.Value;

			// Transfer any uncommitted bytes
			int buffered = this.buffered;
			if (buffered > 0)
			{
				Memory<byte> realMemory = this.output.GetMemory(buffered);
				this.Span[..buffered].CopyTo(realMemory.Span);
				this.output.Advance(buffered);
				buffered = 0;
			}

			this.memory = default;
		}
		else
		{
			this.Commit();
		}

		this.memory = this.output.GetMemoryCheckResult(sizeHint);
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
			Span<byte> span = this.GetSpan();
			int writable = Math.Min(bytesLeftToCopy, span.Length);
			source.Slice(copiedBytes, writable).CopyTo(span);
			copiedBytes += writable;
			bytesLeftToCopy -= writable;
			this.Advance(writable);
		}
	}
}
