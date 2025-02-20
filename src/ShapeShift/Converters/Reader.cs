// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.Converters;

/// <summary>
/// Provides methods for decoding primitive values from a buffer.
/// </summary>
public ref struct Reader
{
	private SequenceReader<byte> inner;

	/// <summary>
	/// Initializes a new instance of the <see cref="Reader"/> struct.
	/// </summary>
	/// <param name="buffer">The buffer to read from.</param>
	/// <param name="deformatter">The deformatter that can interpret the <paramref name="buffer" />.</param>
	public Reader(ReadOnlyMemory<byte> buffer, Deformatter deformatter)
		: this(new ReadOnlySequence<byte>(buffer), deformatter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Reader"/> struct.
	/// </summary>
	/// <param name="sequence">The buffer to read from.</param>
	/// <param name="deformatter">The deformatter that can interpret the <paramref name="sequence" />.</param>
	public Reader(ReadOnlySequence<byte> sequence, Deformatter deformatter)
		: this(new SequenceReader<byte>(sequence), deformatter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Reader"/> struct.
	/// </summary>
	/// <param name="reader">The buffer to read from.</param>
	/// <param name="deformatter">The deformatter that can interpret the <paramref name="reader" />.</param>
	internal Reader(SequenceReader<byte> reader, Deformatter deformatter)
	{
		this.inner = reader;
		this.Deformatter = deformatter;
	}

	/// <summary>
	/// Gets the sequence being read.
	/// </summary>
	public ReadOnlySequence<byte> Sequence => this.inner.Sequence;

	/// <inheritdoc cref="SequenceReader{T}.Remaining"/>
	public long Remaining => this.inner.Remaining;

	/// <summary>
	/// Gets the deformatter used to interpret the <see cref="Sequence"/>.
	/// </summary>
	public Deformatter Deformatter { get; }

	/// <summary>
	/// Gets the next <see cref="TokenType"/> that will be read from the sequence.
	/// </summary>
	/// <inheritdoc cref="Deformatter.PeekNextTypeCode(in Reader)" path="/exception"/>
	public TokenType NextTypeCode => this.Deformatter.PeekNextTypeCode(this);

	/// <summary>
	/// Gets a value indicating whether the next value to be read is <see langword="null" />.
	/// </summary>
	/// <inheritdoc cref="NextTypeCode" path="/exception"/>
	public bool IsNull => this.NextTypeCode == TokenType.Null;

	/// <summary>
	/// Gets the current position of the reader within <see cref="Sequence"/>.
	/// </summary>
	public SequencePosition Position => this.SequenceReader.Position;

	/// <summary>
	/// Gets a value indicating whether the reader is at the end of the sequence.
	/// </summary>
	public bool End => this.SequenceReader.End;

	/// <summary>
	/// Gets the unread portion of the current span. This may not contain all bytes in the original <see cref="UnreadSequence"/>.
	/// </summary>
	public ReadOnlySpan<byte> UnreadSpan => this.inner.UnreadSpan;

	/// <summary>
	/// Gets the unread portion of the sequence.
	/// </summary>
	public ReadOnlySequence<byte> UnreadSequence => this.SequenceReader.UnreadSequence;

	/// <summary>
	/// Gets the underlying <see cref="SequenceReader{T}"/>.
	/// </summary>
	[UnscopedRef]
	internal ref SequenceReader<byte> SequenceReader => ref this.inner;

	/// <summary>
	/// Gets or sets the number of structures that have been announced but not yet read.
	/// </summary>
	/// <remarks>
	/// At any point, skipping this number of structures should advance the reader to the end of the top-level structure it started at.
	/// </remarks>
	internal uint ExpectedRemainingStructures { get; set; }

	/// <inheritdoc cref="Deformatter.TryReadNull(ref Reader)"/>
	public bool TryReadNull() => this.Deformatter.TryReadNull(ref this);

	/// <inheritdoc cref="Deformatter.ReadNull(ref Reader)"/>
	public void ReadNull() => this.Deformatter.ReadNull(ref this);

	/// <inheritdoc cref="Deformatter.ReadStartVector(ref Reader)"/>
	public int? ReadStartVector() => this.Deformatter.ReadStartVector(ref this);

	/// <inheritdoc cref="Deformatter.TryReadStartVector(ref Reader, out int?)"/>
	public bool TryReadStartVector(out int? count) => this.Deformatter.TryReadStartVector(ref this, out count);

	/// <inheritdoc cref="Deformatter.TryAdvanceToNextElement(ref Reader)"/>
	public bool TryAdvanceToNextElement() => this.Deformatter.TryAdvanceToNextElement(ref this);

	/// <inheritdoc cref="Deformatter.ReadStartMap(ref Reader)"/>
	public int? ReadStartMap() => this.Deformatter.ReadStartMap(ref this);

	/// <inheritdoc cref="Deformatter.TryReadStartMap(ref Reader, out int?)"/>
	public bool TryReadStartMap(out int? count) => this.Deformatter.TryReadStartMap(ref this, out count);

	public void ReadMapKeyValueSeparator() => this.Deformatter.ReadMapKeyValueSeparator(ref this);

	/// <inheritdoc cref="Deformatter.ReadBoolean(ref Reader)"/>
	public bool ReadBoolean() => this.Deformatter.ReadBoolean(ref this);

	/// <inheritdoc cref="Deformatter.ReadChar(ref Reader)"/>
	public char ReadChar() => this.Deformatter.ReadChar(ref this);

	/// <inheritdoc cref="Deformatter.ReadString(ref Reader)"/>
	public string? ReadString() => this.Deformatter.ReadString(ref this);

	/// <inheritdoc cref="Deformatter.ReadBytes(ref Reader)"/>
	public ReadOnlySequence<byte>? ReadBytes() => this.Deformatter.ReadBytes(ref this);

	/// <inheritdoc cref="Deformatter.ReadStringSequence(ref Reader)"/>
	public ReadOnlySequence<byte>? ReadStringSequence() => this.Deformatter.ReadStringSequence(ref this);

	/// <inheritdoc cref="Deformatter.TryReadStringSpan(ref Reader, out ReadOnlySpan{byte})"/>
	public bool TryReadStringSpan(out ReadOnlySpan<byte> value) => this.Deformatter.TryReadStringSpan(ref this, out value);

	/// <inheritdoc cref="Deformatter.ReadStringSpan(ref Reader)"/>
	[UnscopedRef]
	public ReadOnlySpan<byte> ReadStringSpan() => this.Deformatter.ReadStringSpan(ref this);

	/// <inheritdoc cref="Deformatter.ReadSByte(ref Reader)"/>
	public sbyte ReadSByte() => this.Deformatter.ReadSByte(ref this);

	/// <inheritdoc cref="Deformatter.ReadInt16(ref Reader)"/>
	public short ReadInt16() => this.Deformatter.ReadInt16(ref this);

	/// <inheritdoc cref="Deformatter.ReadInt32(ref Reader)"/>
	public int ReadInt32() => this.Deformatter.ReadInt32(ref this);

	/// <inheritdoc cref="Deformatter.ReadInt64(ref Reader)"/>
	public long ReadInt64() => this.Deformatter.ReadInt64(ref this);

	/// <inheritdoc cref="Deformatter.ReadByte(ref Reader)"/>
	public byte ReadByte() => this.Deformatter.ReadByte(ref this);

	/// <inheritdoc cref="Deformatter.ReadUInt16(ref Reader)"/>
	public ushort ReadUInt16() => this.Deformatter.ReadUInt16(ref this);

	/// <inheritdoc cref="Deformatter.ReadUInt32(ref Reader)"/>
	public uint ReadUInt32() => this.Deformatter.ReadUInt32(ref this);

	/// <inheritdoc cref="Deformatter.ReadUInt64(ref Reader)"/>
	public ulong ReadUInt64() => this.Deformatter.ReadUInt64(ref this);

	/// <inheritdoc cref="Deformatter.ReadSingle(ref Reader)"/>
	public float ReadSingle() => this.Deformatter.ReadSingle(ref this);

	/// <inheritdoc cref="Deformatter.ReadDouble(ref Reader)"/>
	public double ReadDouble() => this.Deformatter.ReadDouble(ref this);

	/// <inheritdoc cref="Deformatter.ReadRaw(ref Reader, SerializationContext)"/>
	public ReadOnlySequence<byte> ReadRaw(SerializationContext context) => this.Deformatter.ReadRaw(ref this, context);

	/// <inheritdoc cref="Deformatter.ReadRaw(ref Reader, long)"/>
	public ReadOnlySequence<byte> ReadRaw(long length) => this.Deformatter.ReadRaw(ref this, length);

	/// <inheritdoc cref="Deformatter.Skip(ref Reader, SerializationContext)"/>
	public void Skip(SerializationContext context) => this.Deformatter.Skip(ref this, context);

	/// <summary>
	/// Advances the reader.
	/// </summary>
	/// <param name="count">The number of bytes to advance.</param>
	public void Advance(long count) => this.SequenceReader.Advance(count);
}
