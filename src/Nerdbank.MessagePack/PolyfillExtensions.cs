// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1402 // multiple types
#pragma warning disable SA1403 // multiple namespaces

using System.Buffers.Text;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft;
using PolyType.Utilities;

namespace Nerdbank.MessagePack
{
	/// <content>
	/// Utility methods to help make up for cross-targeting support.
	/// </content>
	internal static partial class PolyfillExtensions
	{
		internal static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
		{
#if NET
			return Enumerable.TryGetNonEnumeratedCount(source, out count);
#else
			Requires.NotNull(source);

			switch (source)
			{
				case ICollection<TSource> collection:
					count = collection.Count;
					return true;
				case System.Collections.ICollection collection:
					count = collection.Count;
					return true;
				default:
					count = 0;
					return false;
			}
#endif
		}

		internal static object GetOrAddOrThrow(this MultiProviderTypeCache cache, Type type, ITypeShapeProvider provider)
			=> cache.GetOrAdd(type, provider) ?? throw ThrowMissingTypeShape(type, provider);

		private static Exception ThrowMissingTypeShape(Type type, ITypeShapeProvider provider)
			=> new ArgumentException($"The {provider.GetType().FullName} provider had no type shape for {type.FullName}.", nameof(provider));
	}

#if !NET
	/// <content>
	/// Polyfills specifically for .NET Standard targeting.
	/// </content>
	internal static partial class PolyfillExtensions
	{
		internal static bool HasAnySet(this BitArray bitArray)
		{
			for (int i = 0; i < bitArray.Count; i++)
			{
				if (bitArray[i])
				{
					return true;
				}
			}

			return false;
		}

		internal static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> source, Span<char> destination)
		{
			fixed (byte* pSource = source)
			{
				fixed (char* pDestination = destination)
				{
					return encoding.GetChars(pSource, source.Length, pDestination, destination.Length);
				}
			}
		}

		internal static unsafe int GetChars(this Encoding encoding, ReadOnlySequence<byte> source, Span<char> destination)
		{
			if (source.IsSingleSegment)
			{
				return GetChars(encoding, source.First.Span, destination);
			}

			Decoder decoder = encoding.GetDecoder();
			int charsWritten = 0;
			bool completed = true;
			foreach (ReadOnlyMemory<byte> sourceSegment in source)
			{
				fixed (byte* pSource = sourceSegment.Span)
				{
					fixed (char* pDestination = destination)
					{
						decoder.Convert(pSource, sourceSegment.Length, pDestination, destination.Length, false, out _, out int charsUsed, out completed);
						charsWritten += charsUsed;
						destination = destination[charsUsed..];
					}
				}
			}

			if (!completed)
			{
				fixed (char* pDest = destination)
				{
					decoder.Convert(null, 0, pDest, destination.Length, flush: true, out _, out int charsUsed, out _);
					charsWritten += charsUsed;
				}
			}

			return charsWritten;
		}

		internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> source, Span<byte> destination)
		{
			fixed (char* pSource = source)
			{
				fixed (byte* pDestination = destination)
				{
					return encoding.GetBytes(pSource, source.Length, pDestination, destination.Length);
				}
			}
		}

		internal static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
		{
			if (bytes.IsEmpty)
			{
				return string.Empty;
			}

			fixed (byte* pBytes = bytes)
			{
				return encoding.GetString(pBytes, bytes.Length);
			}
		}

		/// <summary>
		/// Reads from the stream into a memory buffer.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="buffer">The buffer to read directly into.</param>
		/// <returns>The number of bytes actually read.</returns>
		internal static int Read(this Stream stream, Span<byte> buffer)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				int bytesRead = stream.Read(array, 0, buffer.Length);
				new Span<byte>(array, 0, bytesRead).CopyTo(buffer);
				return bytesRead;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}

		/// <summary>
		/// Reads from the stream into a memory buffer.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="buffer">The buffer to read directly into.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>The number of bytes actually read.</returns>
		/// <devremarks>
		/// This method shamelessly copied from the .NET Core 2.1 Stream class: https://github.com/dotnet/coreclr/blob/a113b1c803783c9d64f1f0e946ff9a853e3bc140/src/System.Private.CoreLib/shared/System/IO/Stream.cs#L366-L391.
		/// </devremarks>
		internal static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
			{
				return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
			}
			else
			{
				byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
				return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

				async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
				{
					try
					{
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks -- it's actually from our parent context.
						int result = await readTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
						new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
						return result;
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(localBuffer);
					}
				}
			}
		}

		/// <summary>
		/// Writes a <see cref="Guid"/> as UTF-8 characters.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		/// <param name="destination">A buffer to write to. The longest <paramref name="format"/> requires 68 characters.</param>
		/// <param name="bytesWritten">Receives the number of bytes written to <paramref name="destination"/>.</param>
		/// <param name="format">The format for the GUID. May be "N", "D", "B", "P", or "X".</param>
		/// <returns><see langword="true" /> if <paramref name="destination"/> is large enough; otherwise <see langword="false" />.</returns>
		internal static bool TryFormat(this Guid value, Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format = default)
		{
			format = format.IsEmpty ? ['D'] : format; // Default to "D" if no format is specified.
			bytesWritten = 0;
			GuidBits bits = new(value);

			/*
			 * N: 32 digits, no hyphens. e.g. 69b942342c9e468b9bae77df7a288e45
			 * D: 8-4-4-4-12 digits, with hyphens. e.g. 69b94234-2c9e-468b-9bae-77df7a288e45
			 * B: 8-4-4-4-12 digits, with hyphens, enclosed in braces. e.g. {69b94234-2c9e-468b-9bae-77df7a288e45}
			 * P: 8-4-4-4-12 digits, with hyphens, enclosed in parentheses. e.g. (69b94234-2c9e-468b-9bae-77df7a288e45)
			 * X: 8-4-4-4-12 digits, with hyphens, enclosed in braces, with each group of digits in hexadecimal format. {0x69b94234,0x2c9e,0x468b,{0x9b,0xae,0x77,0xdf,0x7a,0x28,0x8e,0x45}}
			 */

			if (format[0] is 'N')
			{
				return TryWriteInt(destination, ref bytesWritten, bits.a)
					&& TryWriteShort(destination, ref bytesWritten, bits.b)
					&& TryWriteShort(destination, ref bytesWritten, bits.c)
					&& TryWriteByte(destination, ref bytesWritten, bits.d)
					&& TryWriteByte(destination, ref bytesWritten, bits.e)
					&& TryWriteByte(destination, ref bytesWritten, bits.f)
					&& TryWriteByte(destination, ref bytesWritten, bits.g)
					&& TryWriteByte(destination, ref bytesWritten, bits.h)
					&& TryWriteByte(destination, ref bytesWritten, bits.i)
					&& TryWriteByte(destination, ref bytesWritten, bits.j)
					&& TryWriteByte(destination, ref bytesWritten, bits.k);
			}

			// Write out the opening character, if needed.
			if (format[0] is 'B' or 'X')
			{
				destination[bytesWritten++] = (byte)'{';
			}
			else if (format[0] is 'P')
			{
				destination[bytesWritten++] = (byte)'(';
			}

			if (format[0] is 'X')
			{
				// X format is a bit more complex, as it requires the 0x prefix and braces around the last 8 bytes.
				if (!(
					TryWrite0x(destination, ref bytesWritten) &&
					TryWriteInt(destination, ref bytesWritten, bits.a) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteShort(destination, ref bytesWritten, bits.b) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteShort(destination, ref bytesWritten, bits.c) &&
					TryAppend(destination, ref bytesWritten, ",{0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.d) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.e) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.f) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.g) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.h) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.i) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.j) &&
					TryAppend(destination, ref bytesWritten, ",0x"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.k) &&
					TryAppend(destination, ref bytesWritten, "}"u8)))
				{
					return false;
				}
			}
			else
			{
				// D B P formats all use the same format aside from their wrapper, so we can write the values out the same way.
				if (!(
					TryWriteInt(destination, ref bytesWritten, bits.a) &&
					TryAppend(destination, ref bytesWritten, "-"u8) &&
					TryWriteShort(destination, ref bytesWritten, bits.b) &&
					TryAppend(destination, ref bytesWritten, "-"u8) &&
					TryWriteShort(destination, ref bytesWritten, bits.c) &&
					TryAppend(destination, ref bytesWritten, "-"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.d) &&
					TryWriteByte(destination, ref bytesWritten, bits.e) &&
					TryAppend(destination, ref bytesWritten, "-"u8) &&
					TryWriteByte(destination, ref bytesWritten, bits.f) &&
					TryWriteByte(destination, ref bytesWritten, bits.g) &&
					TryWriteByte(destination, ref bytesWritten, bits.h) &&
					TryWriteByte(destination, ref bytesWritten, bits.i) &&
					TryWriteByte(destination, ref bytesWritten, bits.j) &&
					TryWriteByte(destination, ref bytesWritten, bits.k)))
				{
					return false;
				}
			}

			// Write out the closing character, if needed.
			if (format[0] is 'B' or 'X')
			{
				destination[bytesWritten++] = (byte)'}';
			}
			else if (format[0] is 'P')
			{
				destination[bytesWritten++] = (byte)')';
			}

			return true;

			static bool TryAppend(Span<byte> dest, ref int index, ReadOnlySpan<byte> utf8Bytes)
			{
				if (index + utf8Bytes.Length > dest.Length)
				{
					return false;
				}

				utf8Bytes.CopyTo(dest[index..]);
				index += utf8Bytes.Length;
				return true;
			}

			static bool TryWrite0x(Span<byte> dest, ref int index) => TryAppend(dest, ref index, "0x"u8);
			static bool TryWriteInt(Span<byte> dest, ref int index, int value) => TryWriteInteger(dest, ref index, unchecked((uint)value), sizeof(int) * 2);
			static bool TryWriteShort(Span<byte> dest, ref int index, short value) => TryWriteInteger(dest, ref index, unchecked((ushort)value), sizeof(short) * 2);
			static bool TryWriteByte(Span<byte> dest, ref int index, byte value) => TryWriteInteger(dest, ref index, value, sizeof(byte) * 2);
			static bool TryWriteInteger(Span<byte> dest, ref int index, uint value, byte padding)
			{
				if (!Utf8Formatter.TryFormat(value, dest[index..], out int written, new('x', padding)))
				{
					return false;
				}

				index += written;
				return true;
			}
		}

		/// <summary>
		/// Writes a <see cref="Guid"/> as a little-endian binary representation.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		/// <param name="destination">A buffer of at least 16 bytes. Exactly 16 bytes will be written to this.</param>
		/// <returns><see langword="true" /> if the buffer was at least 16 bytes and initialized; otherwise <see langword="false" />.</returns>
		internal static bool TryWriteBytes(this Guid value, Span<byte> destination)
		{
			if (destination.Length < 16)
			{
				return false;
			}

			if (BitConverter.IsLittleEndian)
			{
				MemoryMarshal.TryWrite(destination, ref value);
			}
			else
			{
				// slower path for BigEndian
				Span<GuidBits> guidSpan = stackalloc GuidBits[1];
				guidSpan[0] = value;
				GuidBits endianSwitched = new(MemoryMarshal.AsBytes(guidSpan), false);
				MemoryMarshal.TryWrite(destination, ref endianSwitched);
			}

			return true;
		}

		/// <summary>
		/// Parse a <see cref="Guid"/> from a little-endian binary representation.
		/// </summary>
		/// <param name="bytes">The span of exactly 16 byes.</param>
		/// <returns>The parsed guid.</returns>
		internal static Guid ParseGuidFromLittleEndianBytes(ReadOnlySpan<byte> bytes) => new GuidBits(bytes, bigEndian: false);

		internal static bool IsAssignableTo(this Type left, Type right) => right.IsAssignableFrom(left);

		internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, value);
				return true;
			}

			return false;
		}

		internal static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T value)
		{
			if (stack.Count > 0)
			{
				value = stack.Pop();
				return true;
			}

			value = default;
			return false;
		}

		internal static Exception ThrowNotSupportedOnNETFramework() => throw new PlatformNotSupportedException("This functionality is only supported on .NET.");
	}
#endif
}

#if !NET

namespace System.Diagnostics
{
	internal class UnreachableException : Exception
	{
		internal UnreachableException()
			: base("This code path should be unreachable.")
		{
		}
	}
}

#endif
