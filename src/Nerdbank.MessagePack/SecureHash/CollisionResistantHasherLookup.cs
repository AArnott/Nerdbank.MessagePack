// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Provides access to built-in secure hash functions for primitive types.
/// </summary>
/// <remarks>
/// This class is carefully crafted to help trimming be effective by avoiding type references
/// to types that are not used in the application.
/// Although the retrieval method references all the the fact that it is generic gives the
/// JIT/AOT compiler the opportunity to only reference types that match the type argument
/// (at least for the value types).
/// </remarks>
internal static class CollisionResistantHasherLookup
{
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_char_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_byte_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_ushort_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_uint_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_ulong_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_sbyte_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_short_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_int_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_long_;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_BigIntegerEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_StringEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_BooleanEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_VersionEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_AlreadySecureEqualityComparer_Uri_;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_SingleEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_DoubleEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_DecimalEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_DateTimeEqualityComparer;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_DateTimeOffsetEqualityComparer;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_TimeSpan_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_Guid_;
#if NET
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_Int128_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_UInt128_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_System_Text_Rune_;
	private static IEqualityComparer? _HashCollisionResistantPrimitives_HalfEqualityComparer;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_TimeOnly_;
	private static IEqualityComparer? _CollisionResistantHasherUnmanaged_DateOnly_;
#endif

	/// <summary>
	/// Gets a built-in equality comparer for the given type, if one is available.
	/// </summary>
	/// <typeparam name="T">The type to get a converter for.</typeparam>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveHasher<T>([NotNullWhen(true)] out SecureEqualityComparer<T>? converter)
	{
		if (typeof(T) == typeof(char))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_char_ ??= new CollisionResistantHasherUnmanaged<char>());
			return true;
		}

		if (typeof(T) == typeof(byte))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_byte_ ??= new CollisionResistantHasherUnmanaged<byte>());
			return true;
		}

		if (typeof(T) == typeof(ushort))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_ushort_ ??= new CollisionResistantHasherUnmanaged<ushort>());
			return true;
		}

		if (typeof(T) == typeof(uint))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_uint_ ??= new CollisionResistantHasherUnmanaged<uint>());
			return true;
		}

		if (typeof(T) == typeof(ulong))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_ulong_ ??= new CollisionResistantHasherUnmanaged<ulong>());
			return true;
		}

		if (typeof(T) == typeof(sbyte))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_sbyte_ ??= new CollisionResistantHasherUnmanaged<sbyte>());
			return true;
		}

		if (typeof(T) == typeof(short))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_short_ ??= new CollisionResistantHasherUnmanaged<short>());
			return true;
		}

		if (typeof(T) == typeof(int))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_int_ ??= new CollisionResistantHasherUnmanaged<int>());
			return true;
		}

		if (typeof(T) == typeof(long))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_long_ ??= new CollisionResistantHasherUnmanaged<long>());
			return true;
		}

		if (typeof(T) == typeof(System.Numerics.BigInteger))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_BigIntegerEqualityComparer ??= new HashCollisionResistantPrimitives.BigIntegerEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(string))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_StringEqualityComparer ??= new HashCollisionResistantPrimitives.StringEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(bool))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_BooleanEqualityComparer ??= new HashCollisionResistantPrimitives.BooleanEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(Version))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_VersionEqualityComparer ??= new HashCollisionResistantPrimitives.VersionEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(Uri))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_AlreadySecureEqualityComparer_Uri_ ??= new HashCollisionResistantPrimitives.AlreadySecureEqualityComparer<Uri>());
			return true;
		}

		if (typeof(T) == typeof(float))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_SingleEqualityComparer ??= new HashCollisionResistantPrimitives.SingleEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(double))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_DoubleEqualityComparer ??= new HashCollisionResistantPrimitives.DoubleEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(decimal))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_DecimalEqualityComparer ??= new HashCollisionResistantPrimitives.DecimalEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(DateTime))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_DateTimeEqualityComparer ??= new HashCollisionResistantPrimitives.DateTimeEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(DateTimeOffset))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_DateTimeOffsetEqualityComparer ??= new HashCollisionResistantPrimitives.DateTimeOffsetEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(TimeSpan))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_TimeSpan_ ??= new CollisionResistantHasherUnmanaged<TimeSpan>());
			return true;
		}

		if (typeof(T) == typeof(Guid))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_Guid_ ??= new CollisionResistantHasherUnmanaged<Guid>());
			return true;
		}

#if NET
		if (typeof(T) == typeof(Int128))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_Int128_ ??= new CollisionResistantHasherUnmanaged<Int128>());
			return true;
		}

		if (typeof(T) == typeof(UInt128))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_UInt128_ ??= new CollisionResistantHasherUnmanaged<UInt128>());
			return true;
		}

		if (typeof(T) == typeof(System.Text.Rune))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_System_Text_Rune_ ??= new CollisionResistantHasherUnmanaged<System.Text.Rune>());
			return true;
		}

		if (typeof(T) == typeof(Half))
		{
			converter = (SecureEqualityComparer<T>)(_HashCollisionResistantPrimitives_HalfEqualityComparer ??= new HashCollisionResistantPrimitives.HalfEqualityComparer());
			return true;
		}

		if (typeof(T) == typeof(TimeOnly))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_TimeOnly_ ??= new CollisionResistantHasherUnmanaged<TimeOnly>());
			return true;
		}

		if (typeof(T) == typeof(DateOnly))
		{
			converter = (SecureEqualityComparer<T>)(_CollisionResistantHasherUnmanaged_DateOnly_ ??= new CollisionResistantHasherUnmanaged<DateOnly>());
			return true;
		}

#endif
		converter = null;
		return false;
	}
}
