// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A fast access struct that wraps <see cref="IBufferWriter{T}"/>.
/// </summary>
internal ref struct BufferWriter
{
	/// <summary>
	/// The underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	private IBufferWriter<byte>? output;

	private ref BufferMemoryWriter memoryWriter;

	/// <summary>
	/// The result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
	/// Backing field for the <see cref="Span"/> property.
	/// </summary>
	private Span<byte> span;

	/// <summary>
	/// The result of the last call to <see cref="IBufferWriter{T}.GetMemory(int)"/>, less any bytes already "consumed" with <see cref="Advance(int)"/>.
	/// </summary>
	private ArraySegment<byte> segment;

	/// <summary>
	/// The number of uncommitted bytes (all the calls to <see cref="Advance(int)"/> since the last call to <see cref="Commit"/>).
	/// </summary>
	private int buffered;

	private SequencePool? sequencePool;

	private SequencePool.Rental rental;

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferWriter"/> struct.
	/// </summary>
	/// <param name="output">The <see cref="IBufferWriter{T}"/> to be wrapped.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal BufferWriter(IBufferWriter<byte> output)
	{
		this.output = output ?? throw new ArgumentNullException(nameof(output));

		Memory<byte> memory = this.output.GetMemoryCheckResult();
		MemoryMarshal.TryGetArray(memory, out this.segment);
		this.span = memory.Span;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferWriter"/> struct.
	/// </summary>
	/// <param name="memoryWriter">The async compatible equivalent to this struct, that this struct will temporarily wrap.</param>
	internal BufferWriter(ref BufferMemoryWriter memoryWriter)
	{
		this.memoryWriter = ref memoryWriter;
		this.span = memoryWriter.GetSpan();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferWriter"/> struct.
	/// </summary>
	/// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
	/// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal BufferWriter(SequencePool sequencePool, byte[] array)
	{
		this.sequencePool = sequencePool ?? throw new ArgumentNullException(nameof(sequencePool));

		this.segment = new ArraySegment<byte>(array);
		this.span = this.segment.AsSpan();
	}

	/// <summary>
	/// Gets the result of the last call to <see cref="IBufferWriter{T}.GetSpan(int)"/>.
	/// </summary>
	internal Span<byte> Span => this.span;

	/// <summary>
	/// Gets the rental.
	/// </summary>
	internal SequencePool.Rental SequenceRental => this.rental;

	/// <inheritdoc cref="IBufferWriter{T}.GetSpan(int)"/>
	internal Span<byte> GetSpan(int sizeHint = 0)
	{
		this.Ensure(sizeHint);
		return this.Span;
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

		if (this.segment.Array != null)
		{
			return ref this.segment.Array[this.segment.Offset + this.buffered];
		}
		else
		{
			return ref this.span.GetPinnableReference();
		}
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
			if (!Unsafe.IsNullRef(ref this.memoryWriter))
			{
				this.memoryWriter.Advance(buffered);
			}
			else
			{
				this.MigrateToSequence();

				Assumes.NotNull(this.output);
				this.output.Advance(buffered);
			}

			this.buffered = 0;
			this.span = default;
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
		this.span = this.span[count..];
	}

	/// <summary>
	/// Copies the caller's buffer into this writer and calls <see cref="Advance(int)"/> with the length of the source buffer.
	/// </summary>
	/// <param name="source">The buffer to copy in.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal void Write(ReadOnlySpan<byte> source)
	{
		if (this.span.Length >= source.Length)
		{
			source.CopyTo(this.span);
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
		if (this.span.Length < count || this.span.Length == 0)
		{
			this.EnsureMore(count);
		}
	}

	/// <summary>
	/// Gets the span to the bytes written if they were never committed to the underlying buffer writer.
	/// </summary>
	/// <param name="span">Receives the uncommitted span.</param>
	/// <returns><see langword="true" /> if an uncommitted span was set; otherwise <see langword="false" />.</returns>
	internal bool TryGetUncommittedSpan(out ReadOnlySpan<byte> span)
	{
		if (this.sequencePool != null)
		{
			span = this.segment.AsSpan(0, this.buffered);
			return true;
		}

		span = default;
		return false;
	}

	/// <summary>
	/// Gets a fresh span to write to, with an optional minimum size.
	/// </summary>
	/// <param name="sizeHint">The minimum size for the next requested buffer.</param>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private void EnsureMore(int sizeHint = 0)
	{
		if (this.buffered > 0)
		{
			this.Commit();
		}
		else
		{
			this.MigrateToSequence();
		}

		if (!Unsafe.IsNullRef(ref this.memoryWriter))
		{
			this.span = this.memoryWriter.GetSpan(sizeHint);
		}
		else if (this.output is not null)
		{
			Memory<byte> memory = this.output.GetMemoryCheckResult(sizeHint);
			MemoryMarshal.TryGetArray(memory, out this.segment);
			this.span = memory.Span;
		}
		else
		{
			Assumes.NotReachable();
		}
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
			if (this.span.Length == 0)
			{
				this.EnsureMore();
			}

			int writable = Math.Min(bytesLeftToCopy, this.span.Length);
			source.Slice(copiedBytes, writable).CopyTo(this.span);
			copiedBytes += writable;
			bytesLeftToCopy -= writable;
			this.Advance(writable);
		}
	}

	private void MigrateToSequence()
	{
		if (this.sequencePool != null)
		{
			// We were writing to our private scratch memory, so we have to copy it into the actual writer.
			this.rental = this.sequencePool.Rent();
			this.output = this.rental.Value;
			Span<byte> realSpan = this.output.GetSpan(this.buffered);
			this.segment.AsSpan(0, this.buffered).CopyTo(realSpan);
			this.sequencePool = null;
		}
	}
}
