// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft;

namespace Nerdbank.MessagePack.SecureHash;

internal static class HashCollisionResistantPrimitives
{
	private static int SecureHash<T>(T value)
		where T : unmanaged
	{
#if NET
		Span<T> span = new Span<T>(ref value);
#else
		Span<T> span = stackalloc T[1] { value };
#endif

		return unchecked((int)SipHash.Default.Compute(MemoryMarshal.Cast<T, byte>(span)));
	}

	private static int SecureHash(ReadOnlySpan<byte> data) => unchecked((int)SipHash.Default.Compute(data));

	internal class BooleanEqualityComparer : CollisionResistantHasherUnmanaged<bool>
	{
		internal static readonly BooleanEqualityComparer Instance = new();

		public override long GetSecureHashCode(bool value) => base.GetSecureHashCode(value is true);

		public override bool Equals(bool x, bool y) => x is true == y is true;
	}

#if NET

	internal class HalfEqualityComparer : CollisionResistantHasherUnmanaged<Half>
	{
		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(Half value)
			=> base.GetSecureHashCode(
				value == (Half)0.0 ? (Half)0 : // Special check for 0.0 so that the hash of 0.0 and -0.0 will equal.
				value == Half.NaN ? Half.NaN : // Standardize on the binary representation of NaN prior to hashing.
				value);
	}

#endif

	internal class SingleEqualityComparer : CollisionResistantHasherUnmanaged<float>
	{
		internal static readonly SingleEqualityComparer Instance = new();

		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(float value)
			=> base.GetSecureHashCode(value switch
			{
				0.0f => 0, // Special check for 0.0 so that the hash of 0.0 and -0.0 will equal.
				float.NaN => float.NaN, // Standardize on the binary representation of NaN prior to hashing.
				_ => value,
			});
	}

	internal class DoubleEqualityComparer : CollisionResistantHasherUnmanaged<double>
	{
		internal static readonly DoubleEqualityComparer Instance = new();

		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(double value)
			=> base.GetSecureHashCode(value switch
			{
				0.0 => 0, // Special check for 0.0 so that the hash of 0.0 and -0.0 will equal.
				double.NaN => double.NaN, // Standardize on the binary representation of NaN prior to hashing.
				_ => value,
			});
	}

	internal class DateTimeEqualityComparer : CollisionResistantHasherUnmanaged<DateTime>
	{
		internal static readonly DateTimeEqualityComparer Instance = new();

		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(DateTime value) => SecureHash(value.Ticks);
	}

	internal class DateTimeOffsetEqualityComparer : CollisionResistantHasherUnmanaged<DateTimeOffset>
	{
		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(DateTimeOffset value) => SecureHash(value.UtcDateTime.Ticks);
	}

	internal class StringEqualityComparer : SecureEqualityComparer<string?>
	{
		internal static readonly StringEqualityComparer Instance = new();

		/// <inheritdoc/>
		public override long GetSecureHashCode(string? value)
		{
			// The Cast call could result in OverflowException at runtime if value is greater than 1bn chars in length.
			return SecureHash(MemoryMarshal.Cast<char, byte>(value.AsSpan()));
		}

		/// <inheritdoc/>
		public override bool Equals(string? x, string? y) => x == y;
	}

	/// <summary>
	/// A "secure" equality comparer that assumes the default one is secure.
	/// This should only be used on <see cref="string"/> or types that defer hash code generation to their <see cref="string"/> components.
	/// </summary>
	/// <typeparam name="T">The type to equate and hash.</typeparam>
	internal class AlreadySecureEqualityComparer<T> : SecureEqualityComparer<T>
	{
		public override bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);

		public override long GetSecureHashCode([DisallowNull] T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
	}

	internal class BigIntegerEqualityComparer : SecureEqualityComparer<BigInteger>
	{
		private const int MaxStackAllocBytes = 256;

		/// <inheritdoc/>
		public override bool Equals(BigInteger x, BigInteger y) => x.Equals(y);

		/// <inheritdoc/>
		public override long GetSecureHashCode([DisallowNull] BigInteger obj)
		{
#if NET
			int byteCount = obj.GetByteCount();
			byte[]? rented = byteCount > MaxStackAllocBytes ? ArrayPool<byte>.Shared.Rent(byteCount) : null;
			Span<byte> bytes = rented is null ? stackalloc byte[byteCount] : rented.AsSpan(0, byteCount);
			try
			{
				Assumes.True(obj.TryWriteBytes(bytes, out _));
				return SecureHash(bytes);
			}
			finally
			{
				if (rented is not null)
				{
					ArrayPool<byte>.Shared.Return(rented);
				}
			}
#else
			return SecureHash(obj.ToByteArray());
#endif
		}
	}

	internal class DecimalEqualityComparer : SecureEqualityComparer<decimal>
	{
		private const int DecimalSignMask = unchecked((int)0x80000000);
		private const int DecimalScaleMask = 0x00FF0000;
		private const int DecimalScaleShift = 16;

		/// <inheritdoc/>
		public override bool Equals(decimal x, decimal y) => x.Equals(y);

		/// <inheritdoc/>
		public override long GetSecureHashCode([DisallowNull] decimal obj)
		{
#if NET
			Span<int> bytes = stackalloc int[4];
			if (!decimal.TryGetBits(obj, bytes, out int length))
			{
				throw new NotSupportedException("Decimal too long.");
			}

			NormalizeBits(bytes);
			return SecureHash(MemoryMarshal.Cast<int, byte>(bytes[..length]));
#else
			int[] bytes = decimal.GetBits(obj);
			NormalizeBits(bytes);
			return SecureHash(MemoryMarshal.Cast<int, byte>(bytes.AsSpan()));
#endif
		}

		private static void NormalizeBits(Span<int> bits)
		{
			int flags = bits[3];
			int scale = (flags & DecimalScaleMask) >> DecimalScaleShift;
			if ((bits[0] | bits[1] | bits[2]) == 0)
			{
				bits[3] = 0;
				return;
			}

			while (scale > 0 && TryDivideBitsBy10(bits))
			{
				scale--;
			}

			bits[3] = (flags & DecimalSignMask) | (scale << DecimalScaleShift);
		}

		private static bool TryDivideBitsBy10(Span<int> bits)
		{
			ulong remainder = 0;

			ulong high = (uint)bits[2];
			ulong quotientHigh = high / 10;
			remainder = high % 10;

			ulong middle = (remainder << 32) | (uint)bits[1];
			ulong quotientMiddle = middle / 10;
			remainder = middle % 10;

			ulong low = (remainder << 32) | (uint)bits[0];
			ulong quotientLow = low / 10;
			remainder = low % 10;

			if (remainder != 0)
			{
				return false;
			}

			bits[2] = (int)(uint)quotientHigh;
			bits[1] = (int)(uint)quotientMiddle;
			bits[0] = (int)(uint)quotientLow;
			return true;
		}
	}

	internal class VersionEqualityComparer : SecureEqualityComparer<Version>
	{
		/// <inheritdoc/>
		public override bool Equals(Version? x, Version? y) => EqualityComparer<Version>.Default.Equals(x, y);

		/// <inheritdoc/>
		public override long GetSecureHashCode([DisallowNull] Version obj)
		{
			Span<int> bytes = [obj.Major, obj.Minor, obj.Build, obj.Revision];
			return SecureHash(MemoryMarshal.Cast<int, byte>(bytes));
		}
	}

	internal class ByteArrayEqualityComparer : SecureEqualityComparer<byte[]>
	{
		internal static readonly ByteArrayEqualityComparer Default = new();

		private ByteArrayEqualityComparer()
		{
		}

		public override bool Equals(byte[]? x, byte[]? y) => ReferenceEquals(x, y) || (x is null || y is null) ? false : x.SequenceEqual(y);

		public override long GetSecureHashCode([DisallowNull] byte[] obj) => SecureHash(obj);
	}

	internal class ReadOnlySequenceOfBytesEqualityComparer : SecureEqualityComparer<ReadOnlySequence<byte>>
	{
		internal static readonly ReadOnlySequenceOfBytesEqualityComparer Default = new();

		private ReadOnlySequenceOfBytesEqualityComparer()
		{
		}

		public override bool Equals(ReadOnlySequence<byte> x, ReadOnlySequence<byte> y) => x.SequenceEqual(y);

		public override long GetSecureHashCode([DisallowNull] ReadOnlySequence<byte> obj)
		{
			int segmentCount = 0;
			foreach (ReadOnlyMemory<byte> segment in obj)
			{
				if (++segmentCount > 64)
				{
					break;
				}
			}

			if (segmentCount <= 64)
			{
				Span<long> hashesSpan = stackalloc long[segmentCount];
				int i = 0;
				foreach (ReadOnlyMemory<byte> segment in obj)
				{
					hashesSpan[i++] = SecureHash(segment.Span);
				}

				return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(hashesSpan));
			}

			List<long> hashes = [];
			foreach (ReadOnlyMemory<byte> segment in obj)
			{
				hashes.Add(SecureHash(segment.Span));
			}

#if NET
			Span<long> span = CollectionsMarshal.AsSpan(hashes);
#else
			Span<long> span = hashes.ToArray();
#endif
			return SipHash.Default.Compute(MemoryMarshal.Cast<long, byte>(span));
		}
	}

	internal class CollisionResistantEnumHasher<TEnum, TUnderlying>(SecureEqualityComparer<TUnderlying> equalityComparer) : SecureEqualityComparer<TEnum>
	{
		/// <inheritdoc/>
		public override bool Equals(TEnum? x, TEnum? y) => EqualityComparer<TEnum?>.Default.Equals(x, y);

		/// <inheritdoc/>
		public override long GetSecureHashCode(TEnum obj) => equalityComparer.GetSecureHashCode((TUnderlying)(object)obj!);
	}
}
