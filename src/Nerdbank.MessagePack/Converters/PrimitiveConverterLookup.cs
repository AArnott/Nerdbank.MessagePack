// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

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
	private static IMessagePackConverterInternal? _CharConverter;
	private static IMessagePackConverterInternal? _ByteConverter;
	private static IMessagePackConverterInternal? _SByteConverter;
	private static IMessagePackConverterInternal? _Int16Converter;
	private static IMessagePackConverterInternal? _UInt16Converter;
	private static IMessagePackConverterInternal? _Int32Converter;
	private static IMessagePackConverterInternal? _UInt32Converter;
	private static IMessagePackConverterInternal? _Int64Converter;
	private static IMessagePackConverterInternal? _UInt64Converter;
	private static IMessagePackConverterInternal? _BigIntegerConverter;
	private static IMessagePackConverterInternal? _BooleanConverter;
	private static IMessagePackConverterInternal? _SingleConverter;
	private static IMessagePackConverterInternal? _DoubleConverter;
	private static IMessagePackConverterInternal? _DecimalConverter;
	private static IMessagePackConverterInternal? _DateTimeConverter;
	private static IMessagePackConverterInternal? _DateTimeOffsetConverter;
	private static IMessagePackConverterInternal? _TimeSpanConverter;
	private static IMessagePackConverterInternal? _SystemDrawingColorConverter;
	private static IMessagePackConverterInternal? _SystemDrawingPointConverter;
	private static IMessagePackConverterInternal? _MemoryOfByteConverter;
	private static IMessagePackConverterInternal? _ReadOnlyMemoryOfByteConverter;
	private static IMessagePackConverterInternal? _GuidAsBinaryConverter;
	private static IMessagePackConverterInternal? _StringConverter;
	private static IMessagePackConverterInternal? _StringConverterReferencePreserving;
	private static IMessagePackConverterInternal? _VersionConverter;
	private static IMessagePackConverterInternal? _VersionConverterReferencePreserving;
	private static IMessagePackConverterInternal? _UriConverter;
	private static IMessagePackConverterInternal? _UriConverterReferencePreserving;
	private static IMessagePackConverterInternal? _ByteArrayConverter;
	private static IMessagePackConverterInternal? _ByteArrayConverterReferencePreserving;
	private static IMessagePackConverterInternal? _RawMessagePackConverter;
	private static IMessagePackConverterInternal? _MessagePackValueConverter;
#if NET
	private static IMessagePackConverterInternal? _RuneConverter;
	private static IMessagePackConverterInternal? _Int128Converter;
	private static IMessagePackConverterInternal? _UInt128Converter;
	private static IMessagePackConverterInternal? _HalfConverter;
	private static IMessagePackConverterInternal? _TimeOnlyConverter;
	private static IMessagePackConverterInternal? _DateOnlyConverter;
#endif

	/// <summary>
	/// Gets a built-in converter for the given type, if one is available.
	/// </summary>
	/// <typeparam name="T">The type to get a converter for.</typeparam>
	/// <param name="referencePreserving">Indicates whether a reference-preserving converter is requested.</param>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter<T>(ReferencePreservationMode referencePreserving, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (typeof(T) == typeof(char))
		{
			converter = (MessagePackConverter<T>)(_CharConverter ??= new CharConverter());
			return true;
		}

		if (typeof(T) == typeof(byte))
		{
			converter = (MessagePackConverter<T>)(_ByteConverter ??= new ByteConverter());
			return true;
		}

		if (typeof(T) == typeof(sbyte))
		{
			converter = (MessagePackConverter<T>)(_SByteConverter ??= new SByteConverter());
			return true;
		}

		if (typeof(T) == typeof(short))
		{
			converter = (MessagePackConverter<T>)(_Int16Converter ??= new Int16Converter());
			return true;
		}

		if (typeof(T) == typeof(ushort))
		{
			converter = (MessagePackConverter<T>)(_UInt16Converter ??= new UInt16Converter());
			return true;
		}

		if (typeof(T) == typeof(int))
		{
			converter = (MessagePackConverter<T>)(_Int32Converter ??= new Int32Converter());
			return true;
		}

		if (typeof(T) == typeof(uint))
		{
			converter = (MessagePackConverter<T>)(_UInt32Converter ??= new UInt32Converter());
			return true;
		}

		if (typeof(T) == typeof(long))
		{
			converter = (MessagePackConverter<T>)(_Int64Converter ??= new Int64Converter());
			return true;
		}

		if (typeof(T) == typeof(ulong))
		{
			converter = (MessagePackConverter<T>)(_UInt64Converter ??= new UInt64Converter());
			return true;
		}

		if (typeof(T) == typeof(System.Numerics.BigInteger))
		{
			converter = (MessagePackConverter<T>)(_BigIntegerConverter ??= new BigIntegerConverter());
			return true;
		}

		if (typeof(T) == typeof(bool))
		{
			converter = (MessagePackConverter<T>)(_BooleanConverter ??= new BooleanConverter());
			return true;
		}

		if (typeof(T) == typeof(float))
		{
			converter = (MessagePackConverter<T>)(_SingleConverter ??= new SingleConverter());
			return true;
		}

		if (typeof(T) == typeof(double))
		{
			converter = (MessagePackConverter<T>)(_DoubleConverter ??= new DoubleConverter());
			return true;
		}

		if (typeof(T) == typeof(decimal))
		{
			converter = (MessagePackConverter<T>)(_DecimalConverter ??= new DecimalConverter());
			return true;
		}

		if (typeof(T) == typeof(DateTime))
		{
			converter = (MessagePackConverter<T>)(_DateTimeConverter ??= new DateTimeConverter());
			return true;
		}

		if (typeof(T) == typeof(DateTimeOffset))
		{
			converter = (MessagePackConverter<T>)(_DateTimeOffsetConverter ??= new DateTimeOffsetConverter());
			return true;
		}

		if (typeof(T) == typeof(TimeSpan))
		{
			converter = (MessagePackConverter<T>)(_TimeSpanConverter ??= new TimeSpanConverter());
			return true;
		}

		if (typeof(T) == typeof(System.Drawing.Color))
		{
			converter = (MessagePackConverter<T>)(_SystemDrawingColorConverter ??= new SystemDrawingColorConverter());
			return true;
		}

		if (typeof(T) == typeof(System.Drawing.Point))
		{
			converter = (MessagePackConverter<T>)(_SystemDrawingPointConverter ??= new SystemDrawingPointConverter());
			return true;
		}

		if (typeof(T) == typeof(Memory<byte>))
		{
			converter = (MessagePackConverter<T>)(_MemoryOfByteConverter ??= new MemoryOfByteConverter());
			return true;
		}

		if (typeof(T) == typeof(ReadOnlyMemory<byte>))
		{
			converter = (MessagePackConverter<T>)(_ReadOnlyMemoryOfByteConverter ??= new ReadOnlyMemoryOfByteConverter());
			return true;
		}

		if (typeof(T) == typeof(Guid))
		{
			converter = (MessagePackConverter<T>)(_GuidAsBinaryConverter ??= new GuidAsBinaryConverter());
			return true;
		}

		if (typeof(T) == typeof(string))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter<T>)(_StringConverterReferencePreserving ??= new StringConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter<T>)(_StringConverter ??= new StringConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(Version))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter<T>)(_VersionConverterReferencePreserving ??= new VersionConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter<T>)(_VersionConverter ??= new VersionConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(Uri))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter<T>)(_UriConverterReferencePreserving ??= new UriConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter<T>)(_UriConverter ??= new UriConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(byte[]))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter<T>)(_ByteArrayConverterReferencePreserving ??= new ByteArrayConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter<T>)(_ByteArrayConverter ??= new ByteArrayConverter());
			}

			return true;
		}

		if (typeof(T) == typeof(Nerdbank.MessagePack.RawMessagePack))
		{
			converter = (MessagePackConverter<T>)(_RawMessagePackConverter ??= new RawMessagePackConverter());
			return true;
		}

		if (typeof(T) == typeof(Nerdbank.MessagePack.MessagePackValue))
		{
			converter = (MessagePackConverter<T>)(_MessagePackValueConverter ??= new MessagePackValueConverter());
			return true;
		}

#if NET
		if (typeof(T) == typeof(System.Text.Rune))
		{
			converter = (MessagePackConverter<T>)(_RuneConverter ??= new RuneConverter());
			return true;
		}

		if (typeof(T) == typeof(Int128))
		{
			converter = (MessagePackConverter<T>)(_Int128Converter ??= new Int128Converter());
			return true;
		}

		if (typeof(T) == typeof(UInt128))
		{
			converter = (MessagePackConverter<T>)(_UInt128Converter ??= new UInt128Converter());
			return true;
		}

		if (typeof(T) == typeof(Half))
		{
			converter = (MessagePackConverter<T>)(_HalfConverter ??= new HalfConverter());
			return true;
		}

		if (typeof(T) == typeof(TimeOnly))
		{
			converter = (MessagePackConverter<T>)(_TimeOnlyConverter ??= new TimeOnlyConverter());
			return true;
		}

		if (typeof(T) == typeof(DateOnly))
		{
			converter = (MessagePackConverter<T>)(_DateOnlyConverter ??= new DateOnlyConverter());
			return true;
		}

#endif
		converter = null;
		return false;
	}
}
