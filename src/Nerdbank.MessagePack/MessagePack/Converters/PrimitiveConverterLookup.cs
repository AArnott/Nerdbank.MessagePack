// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.MessagePack.Converters;

/// <summary>
/// Provides access to built-in converters for primitive types.
/// </summary>
/// <remarks>
/// This class is carefully crafted to help trimming be effective by avoiding type references
/// to types that are not used in the application.
/// Although the retrieval method references all the the fact that it is generic gives the
/// JIT/AOT compiler the opportunity to only reference types that match the type argument
/// (at least for the value types).
/// </remarks>
internal static class PrimitiveConverterLookup
{
	private static Converter? _CharConverter;
	private static Converter? _ByteConverter;
	private static Converter? _SByteConverter;
	private static Converter? _Int16Converter;
	private static Converter? _UInt16Converter;
	private static Converter? _Int32Converter;
	private static Converter? _UInt32Converter;
	private static Converter? _Int64Converter;
	private static Converter? _UInt64Converter;
	private static Converter? _BigIntegerConverter;
	private static Converter? _BooleanConverter;
	private static Converter? _SingleConverter;
	private static Converter? _DoubleConverter;
	private static Converter? _DecimalConverter;
	private static Converter? _DateTimeConverter;
	private static Converter? _DateTimeOffsetConverter;
	private static Converter? _TimeSpanConverter;
	private static Converter? _GuidConverter;
	private static Converter? _SystemDrawingColorConverter;
	private static Converter? _MemoryOfByteConverter;
	private static Converter? _ReadOnlyMemoryOfByteConverter;
	private static Converter? _StringConverter;
	private static Converter? _StringConverterReferencePreserving;
	private static Converter? _VersionConverter;
	private static Converter? _VersionConverterReferencePreserving;
	private static Converter? _UriConverter;
	private static Converter? _UriConverterReferencePreserving;
	private static Converter? _ByteArrayConverter;
	private static Converter? _ByteArrayConverterReferencePreserving;
#if NET
	private static Converter? _RuneConverter;
	private static Converter? _Int128Converter;
	private static Converter? _UInt128Converter;
	private static Converter? _HalfConverter;
	private static Converter? _TimeOnlyConverter;
	private static Converter? _DateOnlyConverter;
#endif

	/// <summary>
	/// Gets a built-in converter for the given type, if one is available.
	/// </summary>
	/// <typeparam name="T">The type to get a converter for.</typeparam>
	/// <param name="referencePreserving">Indicates whether a reference-preserving converter is requested.</param>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter<T>(bool referencePreserving, [NotNullWhen(true)] out Converter<T>? converter)
	{
		if (typeof(T) == typeof(char))
		{
			converter = (Converter<T>)(_CharConverter ??= new CharConverter());
			return true;
		}

		if (typeof(T) == typeof(byte))
		{
			converter = (Converter<T>)(_ByteConverter ??= new ByteConverter());
			return true;
		}

		if (typeof(T) == typeof(sbyte))
		{
			converter = (Converter<T>)(_SByteConverter ??= new SByteConverter());
			return true;
		}

		if (typeof(T) == typeof(short))
		{
			converter = (Converter<T>)(_Int16Converter ??= new Int16Converter());
			return true;
		}

		if (typeof(T) == typeof(ushort))
		{
			converter = (Converter<T>)(_UInt16Converter ??= new UInt16Converter());
			return true;
		}

		if (typeof(T) == typeof(int))
		{
			converter = (Converter<T>)(_Int32Converter ??= new Int32Converter());
			return true;
		}

		if (typeof(T) == typeof(uint))
		{
			converter = (Converter<T>)(_UInt32Converter ??= new UInt32Converter());
			return true;
		}

		if (typeof(T) == typeof(long))
		{
			converter = (Converter<T>)(_Int64Converter ??= new Int64Converter());
			return true;
		}

		if (typeof(T) == typeof(ulong))
		{
			converter = (Converter<T>)(_UInt64Converter ??= new UInt64Converter());
			return true;
		}

		if (typeof(T) == typeof(System.Numerics.BigInteger))
		{
			converter = (Converter<T>)(_BigIntegerConverter ??= new BigIntegerConverter());
			return true;
		}

		if (typeof(T) == typeof(bool))
		{
			converter = (Converter<T>)(_BooleanConverter ??= new BooleanConverter());
			return true;
		}

		if (typeof(T) == typeof(float))
		{
			converter = (Converter<T>)(_SingleConverter ??= new SingleConverter());
			return true;
		}

		if (typeof(T) == typeof(double))
		{
			converter = (Converter<T>)(_DoubleConverter ??= new DoubleConverter());
			return true;
		}

		if (typeof(T) == typeof(decimal))
		{
			converter = (Converter<T>)(_DecimalConverter ??= new DecimalConverter());
			return true;
		}

		if (typeof(T) == typeof(DateTime))
		{
			converter = (Converter<T>)(_DateTimeConverter ??= new DateTimeConverter());
			return true;
		}

		if (typeof(T) == typeof(DateTimeOffset))
		{
			converter = (Converter<T>)(_DateTimeOffsetConverter ??= new DateTimeOffsetConverter());
			return true;
		}

		if (typeof(T) == typeof(TimeSpan))
		{
			converter = (Converter<T>)(_TimeSpanConverter ??= new TimeSpanConverter());
			return true;
		}

		if (typeof(T) == typeof(Guid))
		{
			converter = (Converter<T>)(_GuidConverter ??= new GuidConverter());
			return true;
		}

		if (typeof(T) == typeof(System.Drawing.Color))
		{
			converter = (Converter<T>)(_SystemDrawingColorConverter ??= new SystemDrawingColorConverter());
			return true;
		}

		if (typeof(T) == typeof(Memory<byte>))
		{
			converter = (Converter<T>)(_MemoryOfByteConverter ??= new MemoryOfByteConverter());
			return true;
		}

		if (typeof(T) == typeof(ReadOnlyMemory<byte>))
		{
			converter = (Converter<T>)(_ReadOnlyMemoryOfByteConverter ??= new ReadOnlyMemoryOfByteConverter());
			return true;
		}

		if (typeof(T) == typeof(string))
		{
			if (referencePreserving)
			{
				converter = (Converter<T>)(_StringConverterReferencePreserving ??= new StringConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (Converter<T>)(_StringConverter ??= new StringConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(Version))
		{
			if (referencePreserving)
			{
				converter = (Converter<T>)(_VersionConverterReferencePreserving ??= new VersionConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (Converter<T>)(_VersionConverter ??= new VersionConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(Uri))
		{
			if (referencePreserving)
			{
				converter = (Converter<T>)(_UriConverterReferencePreserving ??= new UriConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (Converter<T>)(_UriConverter ??= new UriConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(byte[]))
		{
			if (referencePreserving)
			{
				converter = (Converter<T>)(_ByteArrayConverterReferencePreserving ??= new ByteArrayConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (Converter<T>)(_ByteArrayConverter ??= new ByteArrayConverter());
			}

			return true;
		}

#if NET
		if (typeof(T) == typeof(System.Text.Rune))
		{
			converter = (Converter<T>)(_RuneConverter ??= new RuneConverter());
			return true;
		}

		if (typeof(T) == typeof(Int128))
		{
			converter = (Converter<T>)(_Int128Converter ??= new Int128Converter());
			return true;
		}

		if (typeof(T) == typeof(UInt128))
		{
			converter = (Converter<T>)(_UInt128Converter ??= new UInt128Converter());
			return true;
		}

		if (typeof(T) == typeof(Half))
		{
			converter = (Converter<T>)(_HalfConverter ??= new HalfConverter());
			return true;
		}

		if (typeof(T) == typeof(TimeOnly))
		{
			converter = (Converter<T>)(_TimeOnlyConverter ??= new TimeOnlyConverter());
			return true;
		}

		if (typeof(T) == typeof(DateOnly))
		{
			converter = (Converter<T>)(_DateOnlyConverter ??= new DateOnlyConverter());
			return true;
		}

#endif
		converter = null;
		return false;
	}
}
