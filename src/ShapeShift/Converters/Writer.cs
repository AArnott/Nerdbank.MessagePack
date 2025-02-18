// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace ShapeShift.Converters;

/// <summary>
/// Provides methods for encoding primitive values to a <see cref="BufferWriter"/>.
/// </summary>
public ref struct Writer
{
	private BufferWriter inner;

	/// <summary>
	/// Initializes a new instance of the <see cref="Writer"/> struct.
	/// </summary>
	/// <param name="writer">The buffer writer.</param>
	/// <param name="formatter"><inheritdoc cref="Writer(SequencePool{byte}, byte[], Formatter)" path="/param[@name='formatter']"/></param>
	public Writer(BufferWriter writer, Formatter formatter)
	{
		this.inner = writer;
		this.Formatter = formatter;
	}

	/// <inheritdoc cref="Writer(BufferWriter, Formatter)" />
	public Writer(IBufferWriter<byte> writer, Formatter formatter)
		: this(new BufferWriter(writer), formatter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Writer"/> struct.
	/// </summary>
	/// <param name="sequencePool">The pool from which to draw an <see cref="IBufferWriter{T}"/> if required..</param>
	/// <param name="array">An array to start with so we can avoid accessing the <paramref name="sequencePool"/> if possible.</param>
	/// <param name="formatter">The formatter that can encode primitive values.</param>
	internal Writer(SequencePool<byte> sequencePool, byte[] array, Formatter formatter)
		: this(new BufferWriter(sequencePool, array), formatter)
	{
	}

	/// <summary>
	/// Gets the number of bytes that have been written but not yet committed <see cref="Flush">flushed</see> to the underlying <see cref="IBufferWriter{T}"/>.
	/// </summary>
	public int UnflushedBytes => this.inner.UncommittedBytes;

	/// <summary>
	/// Gets the underlying <see cref="BufferWriter"/>, which may be used by a format-specific <see cref="Converter"/> to write raw bytes.
	/// </summary>
	[UnscopedRef]
	public ref BufferWriter Buffer => ref this.inner;

	/// <summary>
	/// Gets the formatter that is used to encode the primitive values written by this struct.
	/// </summary>
	public Formatter Formatter { get; }

	/// <inheritdoc cref="Formatter.VectorsMustHaveLengthPrefix"/>
	public bool VectorsMustHaveLengthPrefix => this.Formatter.VectorsMustHaveLengthPrefix;

	/// <summary>
	/// Flushes the data that has been written but not yet committed to the underlying <see cref="BufferWriter"/>.
	/// </summary>
	public void Flush() => this.Buffer.Commit();

	/// <inheritdoc cref="Formatter.WriteStartVector(ref BufferWriter, int)"/>
	public void WriteStartVector(int length) => this.Formatter.WriteStartVector(ref this.Buffer, length);

	/// <inheritdoc cref="Formatter.WriteVectorElementSeparator(ref BufferWriter)"/>
	public void WriteVectorElementSeparator() => this.Formatter.WriteVectorElementSeparator(ref this.Buffer);

	/// <inheritdoc cref="Formatter.WriteEndVector(ref BufferWriter)"/>
	public void WriteEndVector() => this.Formatter.WriteEndVector(ref this.Buffer);

	/// <inheritdoc cref="Formatter.WriteStartMap(ref BufferWriter, int)"/>
	public void WriteStartMap(int count) => this.Formatter.WriteStartMap(ref this.Buffer, count);

	/// <inheritdoc cref="Formatter.WriteMapKeyValueSeparator(ref BufferWriter)"/>
	public void WriteMapKeyValueSeparator() => this.Formatter.WriteMapKeyValueSeparator(ref this.Buffer);

	/// <inheritdoc cref="Formatter.WriteMapPairSeparator(ref BufferWriter)"/>
	public void WriteMapPairSeparator() => this.Formatter.WriteMapPairSeparator(ref this.Buffer);

	/// <inheritdoc cref="Formatter.WriteEndMap(ref BufferWriter)"/>
	public void WriteEndMap() => this.Formatter.WriteEndMap(ref this.Buffer);

	/// <inheritdoc cref="Formatter.WriteNull(ref BufferWriter)"/>
	public void WriteNull() => this.Formatter.WriteNull(ref this.Buffer);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, bool)"/>
	public void Write(bool value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, char)"/>
	public void Write(char value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, byte)"/>
	public void Write(byte value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, sbyte)"/>
	public void Write(sbyte value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, ushort)"/>
	public void Write(ushort value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, short)"/>
	public void Write(short value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, uint)"/>
	public void Write(uint value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, int)"/>
	public void Write(int value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, ulong)"/>
	public void Write(ulong value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, long)"/>
	public void Write(long value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, float)"/>
	public void Write(float value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, double)"/>
	public void Write(double value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, string)"/>
	public void Write(string? value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, ReadOnlySpan{char})"/>
	public void Write(scoped ReadOnlySpan<char> value) => this.Formatter.Write(ref this.Buffer, value);

	/// <summary>
	/// Writes a pre-encoded msgpack string.
	/// </summary>
	/// <param name="value">The string to write.</param>
	public void Write(PreformattedString value) => this.Buffer.Write(Requires.NotNull(value).Formatted.Span);

	/// <summary>
	/// Writes a nullable byte buffer.
	/// </summary>
	/// <param name="src">The array of bytes to write. May be <see langword="null"/>.</param>
	public void Write(byte[]? src)
	{
		if (src == null)
		{
			this.WriteNull();
		}
		else
		{
			this.Write(src.AsSpan());
		}
	}

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, ReadOnlySpan{byte})"/>
	public void Write(scoped ReadOnlySpan<byte> value) => this.Formatter.Write(ref this.Buffer, value);

	/// <inheritdoc cref="Formatter.Write(ref BufferWriter, in ReadOnlySequence{byte})"/>
	public void Write(in ReadOnlySequence<byte> value) => this.Formatter.Write(ref this.Buffer, value);

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
	/// <see cref="Write(ReadOnlySpan{byte})"/> or <see cref="Write(in ReadOnlySequence{byte})"/> instead.
	/// </para>
	/// </remarks>
	public bool TryWriteStartBinary(int length) => this.Formatter.TryWriteStartBinary(ref this.Buffer, length);

	/// <inheritdoc cref="Formatter.WriteEncodedString(ref BufferWriter, ReadOnlySpan{byte})"/>
	public void WriteEncodedString(scoped ReadOnlySpan<byte> value) => this.Formatter.WriteEncodedString(ref this.Buffer, value);

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
