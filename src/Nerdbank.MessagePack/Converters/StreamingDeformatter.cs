// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.PolySerializer.Converters;

public abstract record StreamingDeformatter
{
	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	public abstract string FormatName { get; }

	public abstract Encoding Encoding { get; }

	/// <summary>
	/// Peeks at the next msgpack byte without advancing the reader.
	/// </summary>
	/// <param name="code">When successful, receives the next msgpack byte.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryPeekNextCode(in Reader reader, out byte code)
	{
		return reader.SequenceReader.TryPeek(out code) ? DecodeResult.Success : this.InsufficientBytes(reader);
	}

	public DecodeResult TryPeekNextCode(in Reader reader, out TypeCode typeCode)
	{
		DecodeResult result = this.TryPeekNextCode(reader, out byte code);
		if (result != DecodeResult.Success)
		{
			typeCode = default;
			return result;
		}

		typeCode = this.ToTypeCode(code);
		return DecodeResult.Success;
	}

	public abstract DecodeResult TryReadNull(ref Reader reader);

	public abstract DecodeResult TryReadNull(ref Reader reader, out bool isNull);

	public abstract DecodeResult TryReadArrayHeader(ref Reader reader, out int length);

	public abstract DecodeResult TryReadMapHeader(ref Reader reader, out int count);

	public abstract DecodeResult TryReadBinary(ref Reader reader, out ReadOnlySequence<byte> value);

	public abstract DecodeResult TryRead(ref Reader reader, out bool value);

	public abstract DecodeResult TryRead(ref Reader reader, out char value);

	public abstract DecodeResult TryRead(ref Reader reader, out sbyte value);

	public abstract DecodeResult TryRead(ref Reader reader, out short value);

	public abstract DecodeResult TryRead(ref Reader reader, out int value);

	public abstract DecodeResult TryRead(ref Reader reader, out long value);

	public abstract DecodeResult TryRead(ref Reader reader, out byte value);

	public abstract DecodeResult TryRead(ref Reader reader, out ushort value);

	public abstract DecodeResult TryRead(ref Reader reader, out uint value);

	public abstract DecodeResult TryRead(ref Reader reader, out ulong value);

	public abstract DecodeResult TryRead(ref Reader reader, out float value);

	public abstract DecodeResult TryRead(ref Reader reader, out double value);

	public abstract DecodeResult TryRead(ref Reader reader, out string value);

	public abstract DecodeResult TryRead(ref Reader reader, out DateTime value);

	public abstract DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value);

	public abstract DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value);

	public abstract DecodeResult TrySkip(ref Reader reader, ref SerializationContext context);

	public abstract DecodeResult TryReadRaw(ref Reader reader, long length, out ReadOnlySequence<byte> rawMsgPack);

	public abstract string ToFormatName(byte code);

	public abstract TypeCode ToTypeCode(byte code);

	/// <summary>
	/// Gets the error code to return when the buffer has insufficient bytes to finish a decode request.
	/// </summary>
	protected DecodeResult InsufficientBytes(in Reader reader) => this.eof && reader.SequenceReader.Sequence.IsEmpty ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;
}
