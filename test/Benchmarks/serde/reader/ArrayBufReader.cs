namespace Serde.IO;

internal struct ArrayBufReader(byte[] bytes) : IBufReader
{
	private readonly byte[] buffer = bytes;
	private int offset;

	public ReadOnlySpan<byte> Span => this.buffer.AsSpan(this.offset);

	public void Advance(int count)
	{
		this.offset += count;
	}

	public bool FillBuffer(int fillCount)
	{
		return false;
	}
}
