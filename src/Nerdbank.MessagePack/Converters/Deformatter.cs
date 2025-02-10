// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.Converters;

public class Deformatter(StreamingDeformatter streamingDeformatter)
{
	public StreamingDeformatter StreamingDeformatter => streamingDeformatter;

	/// <summary>
	/// Gets the type of the next MessagePack block.
	/// </summary>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	/// <remarks>
	/// See <see cref="MessagePackCode"/> for valid message pack codes and ranges.
	/// </remarks>
	public byte PeekNextCode(in Reader reader)
	{
		return streamingDeformatter.TryPeekNextCode(reader, out byte code) switch
		{
			DecodeResult.Success => code,
			DecodeResult.EmptyBuffer or DecodeResult.InsufficientBuffer => throw ThrowNotEnoughBytesException(),
			_ => throw ThrowUnreachable(),
		};
	}

	public bool TryReadNull(ref Reader reader)
	{
		switch (streamingDeformatter.TryReadNull(ref reader))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.InsufficientBuffer:
			case DecodeResult.EmptyBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				return false;
		}

	}

	public int ReadArrayHeader(ref Reader reader)
	{
		ThrowInsufficientBufferUnless(this.TryReadArrayHeader(ref reader, out int count));

		// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
		// We allow for each primitive to be the minimal 1 byte in size.
		// Formatters that know each element is larger can optionally add a stronger check.
		ThrowInsufficientBufferUnless(reader.SequenceReader.Remaining >= count);

		return count;
	}

	public bool TryReadArrayHeader(ref Reader reader, out int count)
	{
		switch (streamingDeformatter.TryReadArrayHeader(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.PeekNextCode(reader));
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	public int ReadMapHeader(ref Reader reader)
	{
		ThrowInsufficientBufferUnless(this.TryReadMapHeader(ref reader, out int count));

		// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
		// We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
		// Formatters that know each element is larger can optionally add a stronger check.
		ThrowInsufficientBufferUnless(reader.SequenceReader.Remaining >= count * 2);

		return count;
	}

	/// <summary>
	/// Reads a map header from
	/// <see cref="MessagePackCode.Map16"/>,
	/// <see cref="MessagePackCode.Map32"/>, or
	/// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>
	/// if there is sufficient buffer to read it.
	/// </summary>
	/// <param name="count">Receives the number of key=value pairs in the map if the entire map header can be read.</param>
	/// <returns><see langword="true"/> if there was sufficient buffer and a map header was found; <see langword="false"/> if the buffer incompletely describes an map header.</returns>
	/// <exception cref="SerializationException">Thrown if a code other than an map header is encountered.</exception>
	public bool TryReadMapHeader(ref Reader reader, out int count)
	{
		switch (streamingDeformatter.TryReadMapHeader(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.PeekNextCode(reader));
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Throws <see cref="EndOfStreamException"/> if a condition is false.
	/// </summary>
	/// <param name="condition">A boolean value.</param>
	/// <exception cref="EndOfStreamException">Thrown if <paramref name="condition"/> is <see langword="false"/>.</exception>
	private static void ThrowInsufficientBufferUnless(bool condition)
	{
		if (!condition)
		{
			ThrowNotEnoughBytesException();
		}
	}

	/// <summary>
	/// Throws <see cref="EndOfStreamException"/> indicating that there aren't enough bytes remaining in the buffer to store
	/// the promised data.
	/// </summary>
	private static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException();

	/// <summary>
	/// Throws an <see cref="SerializationException"/> explaining an unexpected code was encountered.
	/// </summary>
	/// <param name="code">The code that was encountered.</param>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	private Exception ThrowInvalidCode(byte code)
	{
		throw new SerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, streamingDeformatter.ToFormatName(code)));
	}

	[DoesNotReturn]
	internal static Exception ThrowUnreachable() => throw new UnreachableException();
}
