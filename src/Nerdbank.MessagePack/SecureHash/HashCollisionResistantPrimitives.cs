// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft;

namespace Nerdbank.PolySerializer.SecureHash;

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
		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(DateTime value) => SecureHash(value.Ticks);
	}

	internal class DateTimeOffsetEqualityComparer : CollisionResistantHasherUnmanaged<DateTimeOffset>
	{
		/// <inheritdoc/>
		public override unsafe long GetSecureHashCode(DateTimeOffset value) => SecureHash(value.UtcDateTime.Ticks);
	}

	internal class StringEqualityComparer : SecureEqualityComparer<string>
	{
		internal static readonly StringEqualityComparer Instance = new();

		/// <inheritdoc/>
		public override long GetSecureHashCode(string value)
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
		/// <inheritdoc/>
		public override bool Equals(BigInteger x, BigInteger y) => x.Equals(y);

		/// <inheritdoc/>
		public override long GetSecureHashCode([DisallowNull] BigInteger obj)
		{
#if NET
			Span<byte> bytes = stackalloc byte[obj.GetByteCount()];
			Assumes.True(obj.TryWriteBytes(bytes, out _));
			return SecureHash(bytes);
#else
			return SecureHash(obj.ToByteArray());
#endif
		}
	}

	internal class DecimalEqualityComparer : SecureEqualityComparer<decimal>
	{
		/// <inheritdoc/>
		public override bool Equals(decimal x, decimal y) => x.Equals(y);

		/// <inheritdoc/>
		public override long GetSecureHashCode([DisallowNull] decimal obj)
		{
#if NET
			Span<int> bytes = stackalloc int[500];
			if (!decimal.TryGetBits(obj, bytes, out int length))
			{
				throw new NotSupportedException("Decimal too long.");
			}

			return SecureHash(MemoryMarshal.Cast<int, byte>(bytes[..length]));
#else
			return StringEqualityComparer.Instance.GetSecureHashCode(obj.ToString(CultureInfo.InvariantCulture));
#endif
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

	internal class CollisionResistantEnumHasher<TEnum, TUnderlying>(SecureEqualityComparer<TUnderlying> equalityComparer) : SecureEqualityComparer<TEnum>
	{
		/// <inheritdoc/>
		public override bool Equals(TEnum? x, TEnum? y) => EqualityComparer<TEnum?>.Default.Equals(x, y);

		/// <inheritdoc/>
		public override long GetSecureHashCode(TEnum obj) => equalityComparer.GetSecureHashCode((TUnderlying)(object)obj!);
	}
}
