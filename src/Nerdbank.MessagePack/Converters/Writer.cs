// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

public ref struct Writer
{
	private BufferWriter inner;

	public Writer(BufferWriter writer, Formatter formatter)
	{
		this.inner = writer;
		this.Formatter = formatter;
	}

	public Writer(IBufferWriter<byte> writer, Formatter formatter)
		: this(new BufferWriter(writer), formatter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackWriter"/> struct.
	/// </summary>
	/// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
	/// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
	internal Writer(SequencePool<byte> sequencePool, byte[] array, Formatter formatter)
		: this(new BufferWriter(sequencePool, array), formatter)
	{
	}

	/// <summary>
	/// Gets the number of bytes that have been written but not yet committed <see cref="Flush">flushed</see> to the underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	public int UnflushedBytes => this.inner.UncommittedBytes;

	[UnscopedRef]
	public ref BufferWriter Buffer => ref this.inner;

	public Formatter Formatter { get; }

	public bool ArrayLengthRequiredInHeader => this.Formatter.ArrayLengthRequiredInHeader;

	public void Flush() => this.Buffer.Commit();

	public void WriteArrayHeader(int length) => this.Formatter.WriteArrayStart(ref this, length);

	public void WriteArrayElementSeparator() => this.Formatter.WriteArrayElementSeparator(ref this);

	public void WriteArrayEnd() => this.Formatter.WriteArrayEnd(ref this);

	public void WriteMapHeader(int length) => this.Formatter.WriteMapStart(ref this, length);

	public void WriteMapKeyValueSeparator() => this.Formatter.WriteMapKeyValueSeparator(ref this);

	public void WriteMapValueTrailer() => this.Formatter.WriteMapValueTrailer(ref this);

	public void WriteMapEnd() => this.Formatter.WriteMapEnd(ref this);

	public void WriteNull() => this.Formatter.WriteNull(ref this);

	public void Write(bool value) => this.Formatter.Write(ref this, value);

	public void Write(char value) => this.Formatter.Write(ref this, value);

	public void Write(byte value) => this.Formatter.Write(ref this, value);

	public void Write(sbyte value) => this.Formatter.Write(ref this, value);

	public void Write(ushort value) => this.Formatter.Write(ref this, value);

	public void Write(short value) => this.Formatter.Write(ref this, value);

	public void Write(uint value) => this.Formatter.Write(ref this, value);

	public void Write(int value) => this.Formatter.Write(ref this, value);

	public void Write(ulong value) => this.Formatter.Write(ref this, value);

	public void Write(long value) => this.Formatter.Write(ref this, value);

	public void Write(float value) => this.Formatter.Write(ref this, value);

	public void Write(double value) => this.Formatter.Write(ref this, value);

	public void Write(string? value) => this.Formatter.Write(ref this, value);

	public void Write(scoped ReadOnlySpan<char> value) => this.Formatter.Write(ref this, value);

	public void Write(DateTime value) => this.Formatter.Write(ref this, value);

	/// <summary>
	/// Writes a pre-encoded msgpack string.
	/// </summary>
	/// <param name="value">The string to write.</param>
	public void Write(PreformattedString value) => this.Buffer.Write(Requires.NotNull(value).Formatted.Span);

	public void Write(scoped ReadOnlySpan<byte> value) => this.Formatter.Write(ref this, value);

	public void Write(ReadOnlySequence<byte> value) => this.Formatter.Write(ref this, value);

	/// <summary>
	/// Writes a header introducing a binary buffer, if the formatter supports raw binary.
	/// </summary>
	/// <param name="length">The number of bytes to be written.</param>
	/// <returns><see langword="true" /> if the formatter support raw binary; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// <para>
	/// The caller is expected to follow-up with a call to <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/> to write the binary data
	/// and then a call to <see cref="BufferWriter.Advance(int)"/> to finalize the write.
	/// </para>
	/// <para>
	/// When this method returns <see langword="false" />, the caller should use
	/// <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(ReadOnlySequence{byte})"/> instead.
	/// </para>
	/// </remarks>
	public bool TryWriteBinHeader(int length) => this.Formatter.TryWriteBinHeader(ref this, length);

	public void WriteEncodedString(scoped ReadOnlySpan<byte> value) => this.Formatter.WriteEncodedString(ref this, value);

	/// <summary>
	/// Flushes the writer and returns the written data as a byte array.
	/// </summary>
	/// <returns>A byte array containing the written data.</returns>
	/// <exception cref="NotSupportedException">Thrown if the instance was not initialized to support this operation.</exception>
	internal byte[] FlushAndGetArray()
	{
		if (this.Buffer.TryGetUncommittedSpan(out ReadOnlySpan<byte> span))
		{
			return span.ToArray();
		}
		else
		{
			if (this.Buffer.SequenceRental.Value == null)
			{
				throw new NotSupportedException("This instance was not initialized to support this operation.");
			}

			this.Buffer.Commit();
			byte[] result = this.Buffer.SequenceRental.Value.AsReadOnlySequence.ToArray();
			this.Buffer.SequenceRental.Dispose();
			return result;
		}
	}
}
