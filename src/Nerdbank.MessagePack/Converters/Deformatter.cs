// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A convenience API for reading formatted data (e.g. MessagePack, JSON) from a complete buffer.
/// </summary>
/// <remarks>
/// <para>
/// This type is a wrapper around <see cref="Converters.StreamingDeformatter"/>, which is designed to support
/// reading from potentially incomplete buffers without throwing exceptions.
/// </para>
/// <para>
/// Particular formats may derive from this type in order to wrap unique APIs from their derived <see cref="Converters.StreamingDeformatter"/> type.
/// Derived types should avoid adding fields since the core functionality and options are expected to be on the wrapped <see cref="Converters.StreamingDeformatter"/>.
/// </para>
/// <para>
/// Derived types should be implemented in a thread-safe way, ideally by being immutable.
/// </para>
/// </remarks>
public partial class Deformatter
{
	public Deformatter(StreamingDeformatter streamingDeformatter)
	{
		Requires.NotNull(streamingDeformatter);
		this.StreamingDeformatter = streamingDeformatter;
	}

	public StreamingDeformatter StreamingDeformatter { get; }

	public Encoding Encoding => this.StreamingDeformatter.Encoding;

	/// <summary>
	/// Gets the type of the next MessagePack block.
	/// </summary>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	/// <remarks>
	/// See <see cref="MessagePackCode"/> for valid message pack codes and ranges.
	/// </remarks>
	public byte PeekNextCode(in Reader reader)
	{
		return this.StreamingDeformatter.TryPeekNextCode(reader, out byte code) switch
		{
			DecodeResult.Success => code,
			DecodeResult.EmptyBuffer or DecodeResult.InsufficientBuffer => throw ThrowNotEnoughBytesException(),
			_ => throw ThrowUnreachable(),
		};
	}

	public bool TryReadNull(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadNull(ref reader))
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

	public void ReadNull(ref Reader reader)
	{
		if (!this.TryReadNull(ref reader))
		{
			throw this.StreamingDeformatter.ThrowInvalidCode(reader);
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
		switch (this.StreamingDeformatter.TryReadArrayHeader(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(this.PeekNextCode(reader));
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
		switch (this.StreamingDeformatter.TryReadMapHeader(ref reader, out count))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(this.PeekNextCode(reader));
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	public bool ReadBoolean(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out bool value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public char ReadChar(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out char value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public unsafe float ReadSingle(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out float value))
		{
			case DecodeResult.Success:
				return value;
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public unsafe double ReadDouble(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out double value))
		{
			case DecodeResult.Success:
				return value;
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public string? ReadString(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out string? value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public ReadOnlySequence<byte>? ReadBytes(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadBinary(ref reader, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				if (this.TryReadNull(ref reader))
				{
					return null;
				}

				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public ReadOnlySequence<byte>? ReadStringSequence(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadStringSequence(ref reader, out ReadOnlySequence<byte> value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				if (this.TryReadNull(ref reader))
				{
					return null;
				}

				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public ReadOnlySpan<byte> ReadStringSpan(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryReadStringSpan(ref reader, out bool contiguous, out ReadOnlySpan<byte> span))
		{
			case DecodeResult.Success:
				return contiguous ? span : this.ReadStringSequence(ref reader)!.Value.ToArray();
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public bool TryReadStringSpan(scoped ref Reader reader, out ReadOnlySpan<byte> span)
	{
		switch (this.StreamingDeformatter.TryReadStringSpan(ref reader, out bool contiguous, out span))
		{
			case DecodeResult.Success:
				return contiguous;
			case DecodeResult.TokenMismatch:
				if (this.StreamingDeformatter.TryPeekNextCode(reader, out TypeCode code) == DecodeResult.Success
					&& code == TypeCode.Nil)
				{
					span = default;
					return false;
				}

				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public DateTime ReadDateTime(ref Reader reader)
	{
		switch (this.StreamingDeformatter.TryRead(ref reader, out DateTime value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public ReadOnlySequence<byte> ReadRaw(ref Reader reader, SerializationContext context)
	{
		SequencePosition initialPosition = reader.Position;
		this.Skip(ref reader, context);
		return reader.Sequence.Slice(initialPosition, reader.Position);
	}

	public ReadOnlySequence<byte> ReadRaw(ref Reader reader, long length)
	{
		if (reader.Remaining < length)
		{
			ThrowNotEnoughBytesException();
		}

		ReadOnlySequence<byte> result = reader.Sequence.Slice(reader.Position, length);
		reader.SequenceReader.Advance(length);
		return result;
	}

	public void Skip(ref Reader reader, SerializationContext context) => ThrowInsufficientBufferUnless(this.TrySkip(ref reader, context));

	public TypeCode ToTypeCode(byte code) => this.StreamingDeformatter.ToTypeCode(code);

	public bool PeekIsSignedInteger(in Reader reader)
	{
		switch (this.StreamingDeformatter.TryPeekIsSignedInteger(reader, out bool signed))
		{
			case DecodeResult.Success:
				return signed;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	public bool PeekIsFloat32(in Reader reader)
	{
		switch (this.StreamingDeformatter.TryPeekIsFloat32(reader, out bool float32))
		{
			case DecodeResult.Success:
				return float32;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(reader);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Advances the reader to the next MessagePack structure to be read.
	/// </summary>
	/// <param name="context">The serialization context. Used for the stack guard.</param>
	/// <returns><see langword="true"/> if the entire structure beginning at the current <see cref="Position"/> is found in the <see cref="Sequence"/>; <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The entire structure is skipped, including content of maps or arrays, or any other type with payloads.
	/// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw(SerializationContext)"/> instead.
	/// </remarks>
	internal bool TrySkip(ref Reader reader, SerializationContext context)
	{
		switch (this.StreamingDeformatter.TrySkip(ref reader, ref context))
		{
			case DecodeResult.Success:
				return true;
			case DecodeResult.TokenMismatch:
				throw this.StreamingDeformatter.ThrowInvalidCode(this.PeekNextCode(reader));
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
	protected static void ThrowInsufficientBufferUnless(bool condition)
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
	protected static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException();

	[DoesNotReturn]
	internal static Exception ThrowUnreachable() => throw new UnreachableException();
}
