// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

public ref struct Reader
{
	private readonly Deformatter deformatter;
	private SequenceReader<byte> inner;

	internal Reader(ReadOnlyMemory<byte> buffer, Deformatter deformatter)
		: this(new ReadOnlySequence<byte>(buffer), deformatter)
	{
	}

	internal Reader(ReadOnlySequence<byte> sequence, Deformatter deformatter)
		: this(new SequenceReader<byte>(sequence), deformatter)
	{
	}

	internal Reader(SequenceReader<byte> reader, Deformatter deformatter)
	{
		this.inner = reader;
		this.deformatter = deformatter;
	}

	internal SequenceReader<byte> SequenceReader => this.inner;

	public ReadOnlySequence<byte> Sequence => this.inner.Sequence;

	public long Remaining => this.inner.Remaining;

	public Deformatter Deformatter => this.deformatter;

	public byte NextCode => this.deformatter.PeekNextCode(this);

	public TypeCode NextTypeCode => this.deformatter.ToTypeCode(this.NextCode);

	/// <summary>
	/// Gets the current position of the reader within <see cref="Sequence"/>.
	/// </summary>
	public SequencePosition Position => this.SequenceReader.Position;

	/// <summary>
	/// Gets a value indicating whether the reader is at the end of the sequence.
	/// </summary>
	public bool End => this.SequenceReader.End;

	public ReadOnlySpan<byte> UnreadSpan => this.inner.UnreadSpan;

	public void Advance(long count) => this.inner.Advance(count);

	public bool TryReadNull() => this.deformatter.TryReadNull(ref this);

	public int ReadArrayHeader() => this.deformatter.ReadArrayHeader(ref this);

	public bool TryReadArrayHeader(out int count) => this.deformatter.TryReadArrayHeader(ref this, out count);

	public int ReadMapHeader() => this.deformatter.ReadMapHeader(ref this);

	public bool TryReadMapHeader(out int count) => this.deformatter.TryReadMapHeader(ref this, out count);

	public bool ReadBoolean() => this.deformatter.ReadBoolean(ref this);

	public string? ReadString() => this.deformatter.ReadString(ref this);

	public ReadOnlySequence<byte>? ReadStringSequence() => this.deformatter.ReadStringSequence(ref this);

	public bool TryReadStringSpan(out ReadOnlySpan<byte> value) => this.deformatter.TryReadStringSpan(ref this, out value);

	public sbyte ReadSByte() => this.deformatter.ReadSByte(ref this);

	public short ReadInt16() => this.deformatter.ReadInt16(ref this);

	public int ReadInt32() => this.deformatter.ReadInt32(ref this);

	public long ReadInt64() => this.deformatter.ReadInt64(ref this);

	public byte ReadByte() => this.deformatter.ReadByte(ref this);

	public ushort ReadUInt16() => this.deformatter.ReadUInt16(ref this);

	public uint ReadUInt32() => this.deformatter.ReadUInt32(ref this);

	public ulong ReadUInt64() => this.deformatter.ReadUInt64(ref this);

	public float ReadSingle() => this.deformatter.ReadSingle(ref this);

	public void Skip(SerializationContext context) => this.deformatter.Skip(ref this, context);
}
