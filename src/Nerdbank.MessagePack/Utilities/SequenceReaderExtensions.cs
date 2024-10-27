// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nerdbank.MessagePack.Utilities;

internal static class SequenceReaderExtensions
{
	internal static bool TryAdvance(this SequenceReader<byte> reader, long count)
	{
		if (reader.Remaining >= count)
		{
			reader.Advance(count);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Try to read the given type out of the buffer if possible. Warning: this is dangerous to use with arbitrary
	/// structs- see remarks for full details.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: The read is a straight copy of bits. If a struct depends on specific state of its members to
	/// behave correctly this can lead to exceptions, etc. If reading endian specific integers, use the explicit
	/// overloads such as <see cref="TryReadBigEndian(ref SequenceReader{byte}, out short)"/>.
	/// </remarks>
	/// <returns>
	/// True if successful. <paramref name="value"/> will be default if failed (due to lack of space).
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe bool TryRead<T>(ref this SequenceReader<byte> reader, out T value)
		where T : unmanaged
	{
		ReadOnlySpan<byte> span = reader.UnreadSpan;
		if (span.Length < sizeof(T))
		{
			return TryReadMultisegment(ref reader, out value);
		}

		value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
		reader.Advance(sizeof(T));
		return true;
	}

	private static unsafe bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value)
		where T : unmanaged
	{
		Debug.Assert(reader.UnreadSpan.Length < sizeof(T), "reader.UnreadSpan.Length < sizeof(T)");

		// Not enough data in the current segment, try to peek for the data we need.
		T buffer = default;
		Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(T));

		if (!reader.TryCopyTo(tempSpan))
		{
			value = default;
			return false;
		}

		value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
		reader.Advance(sizeof(T));
		return true;
	}

	/// <summary>
	/// Reads an <see cref="sbyte"/> from the next position in the sequence.
	/// </summary>
	/// <param name="reader">The reader to read from.</param>
	/// <param name="value">Receives the value read.</param>
	/// <returns><see langword="true"/> if there was another byte in the sequence; <see langword="false"/> otherwise.</returns>
	public static bool TryRead(ref this SequenceReader<byte> reader, out sbyte value)
	{
		if (TryRead(ref reader, out byte byteValue))
		{
			value = unchecked((sbyte)byteValue);
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Reads an <see cref="UInt16"/> as big endian.
	/// </summary>
	/// <returns>False if there wasn't enough data for an <see cref="UInt16"/>.</returns>
	public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ushort value)
	{
		if (reader.TryReadBigEndian(out short shortValue))
		{
			value = unchecked((ushort)shortValue);
			return true;
		}

		value = default;
		return false;
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out short value)
	{
		if (reader.TryRead(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Reads an <see cref="UInt32"/> as big endian.
	/// </summary>
	/// <returns>False if there wasn't enough data for an <see cref="UInt32"/>.</returns>
	public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out uint value)
	{
		if (reader.TryReadBigEndian(out int intValue))
		{
			value = unchecked((uint)intValue);
			return true;
		}

		value = default;
		return false;
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out int value)
	{
		if (reader.TryRead(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Reads an <see cref="UInt64"/> as big endian.
	/// </summary>
	/// <returns>False if there wasn't enough data for an <see cref="UInt64"/>.</returns>
	public static bool TryReadBigEndian(ref this SequenceReader<byte> reader, out ulong value)
	{
		if (reader.TryReadBigEndian(out long longValue))
		{
			value = unchecked((ulong)longValue);
			return true;
		}

		value = default;
		return false;
	}

	private static bool TryReadReverseEndianness(ref SequenceReader<byte> reader, out long value)
	{
		if (reader.TryRead(out value))
		{
			value = BinaryPrimitives.ReverseEndianness(value);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Reads a <see cref="Single"/> as big endian.
	/// </summary>
	/// <returns>False if there wasn't enough data for a <see cref="Single"/>.</returns>
	public static unsafe bool TryReadBigEndian(ref this SequenceReader<byte> reader, out float value)
	{
		if (reader.TryReadBigEndian(out int intValue))
		{
			value = *(float*)&intValue;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Reads a <see cref="Double"/> as big endian.
	/// </summary>
	/// <returns>False if there wasn't enough data for a <see cref="Double"/>.</returns>
	public static unsafe bool TryReadBigEndian(ref this SequenceReader<byte> reader, out double value)
	{
		if (reader.TryReadBigEndian(out long longValue))
		{
			value = *(double*)&longValue;
			return true;
		}

		value = default;
		return false;
	}
}
