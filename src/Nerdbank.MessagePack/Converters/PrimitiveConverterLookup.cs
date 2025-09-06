// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
	/// <param name="primitiveType">The type to get a converter for.</param>
	/// <param name="referencePreserving">Indicates whether a reference-preserving converter is requested.</param>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter(Type primitiveType, ReferencePreservationMode referencePreserving, [NotNullWhen(true)] out MessagePackConverter? converter)
	{
		string? primitiveTypeName = primitiveType.FullName;

		if (primitiveType == typeof(char))
		{
			converter = (MessagePackConverter)(_CharConverter ??= new CharConverter());
			return true;
		}

		if (primitiveType == typeof(byte))
		{
			converter = (MessagePackConverter)(_ByteConverter ??= new ByteConverter());
			return true;
		}

		if (primitiveType == typeof(sbyte))
		{
			converter = (MessagePackConverter)(_SByteConverter ??= new SByteConverter());
			return true;
		}

		if (primitiveType == typeof(short))
		{
			converter = (MessagePackConverter)(_Int16Converter ??= new Int16Converter());
			return true;
		}

		if (primitiveType == typeof(ushort))
		{
			converter = (MessagePackConverter)(_UInt16Converter ??= new UInt16Converter());
			return true;
		}

		if (primitiveType == typeof(int))
		{
			converter = (MessagePackConverter)(_Int32Converter ??= new Int32Converter());
			return true;
		}

		if (primitiveType == typeof(uint))
		{
			converter = (MessagePackConverter)(_UInt32Converter ??= new UInt32Converter());
			return true;
		}

		if (primitiveType == typeof(long))
		{
			converter = (MessagePackConverter)(_Int64Converter ??= new Int64Converter());
			return true;
		}

		if (primitiveType == typeof(ulong))
		{
			converter = (MessagePackConverter)(_UInt64Converter ??= new UInt64Converter());
			return true;
		}

		if (primitiveTypeName == "System.Numerics.BigInteger")
		{
			converter = (MessagePackConverter)(_BigIntegerConverter ??= CreateBigIntegerConverter());
			return true;
		}

		if (primitiveType == typeof(bool))
		{
			converter = (MessagePackConverter)(_BooleanConverter ??= new BooleanConverter());
			return true;
		}

		if (primitiveType == typeof(float))
		{
			converter = (MessagePackConverter)(_SingleConverter ??= new SingleConverter());
			return true;
		}

		if (primitiveType == typeof(double))
		{
			converter = (MessagePackConverter)(_DoubleConverter ??= new DoubleConverter());
			return true;
		}

		if (primitiveType == typeof(decimal))
		{
			converter = (MessagePackConverter)(_DecimalConverter ??= new DecimalConverter());
			return true;
		}

		if (primitiveType == typeof(DateTime))
		{
			converter = (MessagePackConverter)(_DateTimeConverter ??= new DateTimeConverter());
			return true;
		}

		if (primitiveType == typeof(DateTimeOffset))
		{
			converter = (MessagePackConverter)(_DateTimeOffsetConverter ??= new DateTimeOffsetConverter());
			return true;
		}

		if (primitiveType == typeof(TimeSpan))
		{
			converter = (MessagePackConverter)(_TimeSpanConverter ??= new TimeSpanConverter());
			return true;
		}

		if (primitiveTypeName == "System.Drawing.Color")
		{
			converter = (MessagePackConverter)(_SystemDrawingColorConverter ??= CreateSystemDrawingColorConverter());
			return true;
		}

		if (primitiveTypeName == "System.Drawing.Point")
		{
			converter = (MessagePackConverter)(_SystemDrawingPointConverter ??= CreateSystemDrawingPointConverter());
			return true;
		}

		if (primitiveType == typeof(Memory<byte>))
		{
			converter = (MessagePackConverter)(_MemoryOfByteConverter ??= new MemoryOfByteConverter());
			return true;
		}

		if (primitiveType == typeof(ReadOnlyMemory<byte>))
		{
			converter = (MessagePackConverter)(_ReadOnlyMemoryOfByteConverter ??= new ReadOnlyMemoryOfByteConverter());
			return true;
		}

		if (primitiveType == typeof(Guid))
		{
			converter = (MessagePackConverter)(_GuidAsBinaryConverter ??= new GuidAsBinaryConverter());
			return true;
		}

		if (primitiveType == typeof(string))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter)(_StringConverterReferencePreserving ??= new StringConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter)(_StringConverter ??= new StringConverter());
			}

			return true;
		}

		if (primitiveType == typeof(Version))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter)(_VersionConverterReferencePreserving ??= new VersionConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter)(_VersionConverter ??= new VersionConverter());
			}

			return true;
		}

		if (primitiveType == typeof(Uri))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter)(_UriConverterReferencePreserving ??= new UriConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter)(_UriConverter ??= new UriConverter());
			}

			return true;
		}

		if (primitiveType == typeof(byte[]))
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
				converter = (MessagePackConverter)(_ByteArrayConverterReferencePreserving ??= new ByteArrayConverter().WrapWithReferencePreservation());
			}
			else
			{
				converter = (MessagePackConverter)(_ByteArrayConverter ??= new ByteArrayConverter());
			}

			return true;
		}

		if (primitiveType == typeof(Nerdbank.MessagePack.RawMessagePack))
		{
			converter = (MessagePackConverter)(_RawMessagePackConverter ??= new RawMessagePackConverter());
			return true;
		}

		if (primitiveType == typeof(Nerdbank.MessagePack.MessagePackValue))
		{
			converter = (MessagePackConverter)(_MessagePackValueConverter ??= new MessagePackValueConverter());
			return true;
		}

#if NET
		if (primitiveType == typeof(System.Text.Rune))
		{
			converter = (MessagePackConverter)(_RuneConverter ??= new RuneConverter());
			return true;
		}

		if (primitiveType == typeof(Int128))
		{
			converter = (MessagePackConverter)(_Int128Converter ??= new Int128Converter());
			return true;
		}

		if (primitiveType == typeof(UInt128))
		{
			converter = (MessagePackConverter)(_UInt128Converter ??= new UInt128Converter());
			return true;
		}

		if (primitiveType == typeof(Half))
		{
			converter = (MessagePackConverter)(_HalfConverter ??= new HalfConverter());
			return true;
		}

		if (primitiveType == typeof(TimeOnly))
		{
			converter = (MessagePackConverter)(_TimeOnlyConverter ??= new TimeOnlyConverter());
			return true;
		}

		if (primitiveType == typeof(DateOnly))
		{
			converter = (MessagePackConverter)(_DateOnlyConverter ??= new DateOnlyConverter());
			return true;
		}

#endif
		converter = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IMessagePackConverterInternal CreateBigIntegerConverter() => new BigIntegerConverter();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IMessagePackConverterInternal CreateSystemDrawingColorConverter() => new SystemDrawingColorConverter();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IMessagePackConverterInternal CreateSystemDrawingPointConverter() => new SystemDrawingPointConverter();
}
