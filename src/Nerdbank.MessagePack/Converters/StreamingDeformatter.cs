﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

public abstract class StreamingDeformatter
{
	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	/// <summary>
	/// Peeks at the next msgpack byte without advancing the reader.
	/// </summary>
	/// <param name="code">When successful, receives the next msgpack byte.</param>
	/// <returns>The success or error code.</returns>
	public DecodeResult TryPeekNextCode(in Reader reader, out byte code)
	{
		return reader.SequenceReader.TryPeek(out code) ? DecodeResult.Success : this.InsufficientBytes(reader);
	}

	public abstract DecodeResult TryReadNull(ref Reader reader);

	public abstract DecodeResult TryReadNull(ref Reader reader, out bool isNull);

	public abstract DecodeResult TryReadArrayHeader(ref Reader reader, out int length);

	public abstract DecodeResult TryReadMapHeader(ref Reader reader, out int count);

	public abstract string ToFormatName(byte code);

	/// <summary>
	/// Gets the error code to return when the buffer has insufficient bytes to finish a decode request.
	/// </summary>
	protected DecodeResult InsufficientBytes(in Reader reader) => this.eof && reader.SequenceReader.Sequence.IsEmpty ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;
}
