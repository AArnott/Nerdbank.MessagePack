// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

public ref struct Reader
{
	private readonly Deformatter deformatter;
	private SequenceReader<byte> inner;

	internal Reader(SequenceReader<byte> reader, Deformatter deformatter)
	{
		this.inner = reader;
		this.deformatter = deformatter;
	}

	internal SequenceReader<byte> SequenceReader => this.inner;

	public byte NextCode => this.deformatter.PeekNextCode(this);

	public ReadOnlySpan<byte> UnreadSpan => this.inner.UnreadSpan;

	public void Advance(long count) => this.inner.Advance(count);

	public bool TryReadNull() => this.deformatter.TryReadNull(ref this);

	public int ReadArrayHeader() => this.deformatter.ReadArrayHeader(ref this);

	public bool TryReadArrayHeader(out int count) => this.deformatter.TryReadArrayHeader(ref this, out count);

	public int ReadMapHeader() => this.deformatter.ReadMapHeader(ref this);

	public bool TryReadMapHeader(out int count) => this.deformatter.TryReadMapHeader(ref this, out count);
}
