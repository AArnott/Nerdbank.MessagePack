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
/// <para>This class is carefully crafted to avoid assembly loads by testing type names
/// rather than types directly for types declared in assemblies that may not be loaded.</para>
/// <para>On .NET, this class is also carefully crafted to help trimming be effective by avoiding type references
/// to types that are not used in the application.
/// Although the retrieval method references all the types, the fact that it is generic gives the
/// JIT/AOT compiler the opportunity to only reference types that match the type argument
/// (at least for the value types).
/// The generic method itself leads to more methods to JIT at runtime when NativeAOT is *not* used.
/// It's a trade-off, which is why we never use the generic method on .NET Framework where NativeAOT isn't even an option.
/// </para>
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
	private static IMessagePackConverterInternal? _ExceptionConverter;
	private static IMessagePackConverterInternal? _SystemGlobalizationCultureInfoConverter;
	private static IMessagePackConverterInternal? _SystemTextEncodingConverter;
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
	private static IMessagePackConverterInternal? _ExtensionConverter;
#if NET
	private static IMessagePackConverterInternal? _RuneConverter;
	private static IMessagePackConverterInternal? _Int128Converter;
	private static IMessagePackConverterInternal? _UInt128Converter;
	private static IMessagePackConverterInternal? _HalfConverter;
	private static IMessagePackConverterInternal? _TimeOnlyConverter;
	private static IMessagePackConverterInternal? _DateOnlyConverter;
#endif

#if NET
	/// <summary>
	/// Gets a built-in converter for the given type, if one is available.
	/// </summary>
	/// <typeparam name="T">The type to get a converter for.</typeparam>
	/// <param name="referencePreserving">Indicates whether a reference-preserving converter is requested.</param>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter<T>(ReferencePreservationMode referencePreserving, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
#else
	/// <summary>
	/// Gets a built-in converter for the given type, if one is available.
	/// </summary>
	/// <param name="type">The type to get a converter for.</param>
	/// <param name="referencePreserving">Indicates whether a reference-preserving converter is requested.</param>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter(Type type, ReferencePreservationMode referencePreserving, [NotNullWhen(true)] out MessagePackConverter? converter)
#endif
	{
#if NET
		if (typeof(T) == typeof(char))
#else
		if (type == typeof(char))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_CharConverter ??= new CharConverter());
#else
			converter = (MessagePackConverter)(_CharConverter ??= new CharConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(byte))
#else
		if (type == typeof(byte))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_ByteConverter ??= new ByteConverter());
#else
			converter = (MessagePackConverter)(_ByteConverter ??= new ByteConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(sbyte))
#else
		if (type == typeof(sbyte))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_SByteConverter ??= new SByteConverter());
#else
			converter = (MessagePackConverter)(_SByteConverter ??= new SByteConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(short))
#else
		if (type == typeof(short))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_Int16Converter ??= new Int16Converter());
#else
			converter = (MessagePackConverter)(_Int16Converter ??= new Int16Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(ushort))
#else
		if (type == typeof(ushort))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_UInt16Converter ??= new UInt16Converter());
#else
			converter = (MessagePackConverter)(_UInt16Converter ??= new UInt16Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(int))
#else
		if (type == typeof(int))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_Int32Converter ??= new Int32Converter());
#else
			converter = (MessagePackConverter)(_Int32Converter ??= new Int32Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(uint))
#else
		if (type == typeof(uint))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_UInt32Converter ??= new UInt32Converter());
#else
			converter = (MessagePackConverter)(_UInt32Converter ??= new UInt32Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(long))
#else
		if (type == typeof(long))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_Int64Converter ??= new Int64Converter());
#else
			converter = (MessagePackConverter)(_Int64Converter ??= new Int64Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(ulong))
#else
		if (type == typeof(ulong))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_UInt64Converter ??= new UInt64Converter());
#else
			converter = (MessagePackConverter)(_UInt64Converter ??= new UInt64Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(bool))
#else
		if (type == typeof(bool))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_BooleanConverter ??= new BooleanConverter());
#else
			converter = (MessagePackConverter)(_BooleanConverter ??= new BooleanConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(float))
#else
		if (type == typeof(float))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_SingleConverter ??= new SingleConverter());
#else
			converter = (MessagePackConverter)(_SingleConverter ??= new SingleConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(double))
#else
		if (type == typeof(double))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_DoubleConverter ??= new DoubleConverter());
#else
			converter = (MessagePackConverter)(_DoubleConverter ??= new DoubleConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(decimal))
#else
		if (type == typeof(decimal))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_DecimalConverter ??= new DecimalConverter());
#else
			converter = (MessagePackConverter)(_DecimalConverter ??= new DecimalConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(DateTime))
#else
		if (type == typeof(DateTime))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_DateTimeConverter ??= new DateTimeConverter());
#else
			converter = (MessagePackConverter)(_DateTimeConverter ??= new DateTimeConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(DateTimeOffset))
#else
		if (type == typeof(DateTimeOffset))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_DateTimeOffsetConverter ??= new DateTimeOffsetConverter());
#else
			converter = (MessagePackConverter)(_DateTimeOffsetConverter ??= new DateTimeOffsetConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(TimeSpan))
#else
		if (type == typeof(TimeSpan))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_TimeSpanConverter ??= new TimeSpanConverter());
#else
			converter = (MessagePackConverter)(_TimeSpanConverter ??= new TimeSpanConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(System.Exception))
#else
		if (type == typeof(System.Exception))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_ExceptionConverter ??= new ExceptionConverter());
#else
			converter = (MessagePackConverter)(_ExceptionConverter ??= new ExceptionConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(System.Globalization.CultureInfo))
#else
		if (type == typeof(System.Globalization.CultureInfo))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_SystemGlobalizationCultureInfoConverter ??= new SystemGlobalizationCultureInfoConverter());
#else
			converter = (MessagePackConverter)(_SystemGlobalizationCultureInfoConverter ??= new SystemGlobalizationCultureInfoConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(System.Text.Encoding))
#else
		if (type == typeof(System.Text.Encoding))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_SystemTextEncodingConverter ??= new SystemTextEncodingConverter());
#else
			converter = (MessagePackConverter)(_SystemTextEncodingConverter ??= new SystemTextEncodingConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Memory<byte>))
#else
		if (type == typeof(Memory<byte>))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_MemoryOfByteConverter ??= new MemoryOfByteConverter());
#else
			converter = (MessagePackConverter)(_MemoryOfByteConverter ??= new MemoryOfByteConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(ReadOnlyMemory<byte>))
#else
		if (type == typeof(ReadOnlyMemory<byte>))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_ReadOnlyMemoryOfByteConverter ??= new ReadOnlyMemoryOfByteConverter());
#else
			converter = (MessagePackConverter)(_ReadOnlyMemoryOfByteConverter ??= new ReadOnlyMemoryOfByteConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Guid))
#else
		if (type == typeof(Guid))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_GuidAsBinaryConverter ??= new GuidAsBinaryConverter());
#else
			converter = (MessagePackConverter)(_GuidAsBinaryConverter ??= new GuidAsBinaryConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(string))
#else
		if (type == typeof(string))
#endif
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
#if NET
				converter = (MessagePackConverter<T>)(_StringConverterReferencePreserving ??= new StringConverter().WrapWithReferencePreservation());
#else
				converter = (MessagePackConverter)(_StringConverterReferencePreserving ??= new StringConverter().WrapWithReferencePreservation());
#endif
			}
			else
			{
#if NET
				converter = (MessagePackConverter<T>)(_StringConverter ??= new StringConverter());
#else
				converter = (MessagePackConverter)(_StringConverter ??= new StringConverter());
#endif
			}

			return true;
		}

#if NET
		if (typeof(T) == typeof(Version))
#else
		if (type == typeof(Version))
#endif
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
#if NET
				converter = (MessagePackConverter<T>)(_VersionConverterReferencePreserving ??= new VersionConverter().WrapWithReferencePreservation());
#else
				converter = (MessagePackConverter)(_VersionConverterReferencePreserving ??= new VersionConverter().WrapWithReferencePreservation());
#endif
			}
			else
			{
#if NET
				converter = (MessagePackConverter<T>)(_VersionConverter ??= new VersionConverter());
#else
				converter = (MessagePackConverter)(_VersionConverter ??= new VersionConverter());
#endif
			}

			return true;
		}

#if NET
		if (typeof(T) == typeof(Uri))
#else
		if (type == typeof(Uri))
#endif
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
#if NET
				converter = (MessagePackConverter<T>)(_UriConverterReferencePreserving ??= new UriConverter().WrapWithReferencePreservation());
#else
				converter = (MessagePackConverter)(_UriConverterReferencePreserving ??= new UriConverter().WrapWithReferencePreservation());
#endif
			}
			else
			{
#if NET
				converter = (MessagePackConverter<T>)(_UriConverter ??= new UriConverter());
#else
				converter = (MessagePackConverter)(_UriConverter ??= new UriConverter());
#endif
			}

			return true;
		}

#if NET
		if (typeof(T) == typeof(byte[]))
#else
		if (type == typeof(byte[]))
#endif
		{
			if (referencePreserving != ReferencePreservationMode.Off)
			{
#if NET
				converter = (MessagePackConverter<T>)(_ByteArrayConverterReferencePreserving ??= new ByteArrayConverter().WrapWithReferencePreservation());
#else
				converter = (MessagePackConverter)(_ByteArrayConverterReferencePreserving ??= new ByteArrayConverter().WrapWithReferencePreservation());
#endif
			}
			else
			{
#if NET
				converter = (MessagePackConverter<T>)(_ByteArrayConverter ??= new ByteArrayConverter());
#else
				converter = (MessagePackConverter)(_ByteArrayConverter ??= new ByteArrayConverter());
#endif
			}

			return true;
		}

#if NET
		if (typeof(T) == typeof(Nerdbank.MessagePack.RawMessagePack))
#else
		if (type == typeof(Nerdbank.MessagePack.RawMessagePack))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_RawMessagePackConverter ??= new RawMessagePackConverter());
#else
			converter = (MessagePackConverter)(_RawMessagePackConverter ??= new RawMessagePackConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Nerdbank.MessagePack.MessagePackValue))
#else
		if (type == typeof(Nerdbank.MessagePack.MessagePackValue))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_MessagePackValueConverter ??= new MessagePackValueConverter());
#else
			converter = (MessagePackConverter)(_MessagePackValueConverter ??= new MessagePackValueConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Nerdbank.MessagePack.Extension))
#else
		if (type == typeof(Nerdbank.MessagePack.Extension))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_ExtensionConverter ??= new ExtensionConverter());
#else
			converter = (MessagePackConverter)(_ExtensionConverter ??= new ExtensionConverter());
#endif
			return true;
		}

#if NET
#if NET
		if (typeof(T) == typeof(System.Text.Rune))
#else
		if (type == typeof(System.Text.Rune))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_RuneConverter ??= new RuneConverter());
#else
			converter = (MessagePackConverter)(_RuneConverter ??= new RuneConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Int128))
#else
		if (type == typeof(Int128))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_Int128Converter ??= new Int128Converter());
#else
			converter = (MessagePackConverter)(_Int128Converter ??= new Int128Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(UInt128))
#else
		if (type == typeof(UInt128))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_UInt128Converter ??= new UInt128Converter());
#else
			converter = (MessagePackConverter)(_UInt128Converter ??= new UInt128Converter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(Half))
#else
		if (type == typeof(Half))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_HalfConverter ??= new HalfConverter());
#else
			converter = (MessagePackConverter)(_HalfConverter ??= new HalfConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(TimeOnly))
#else
		if (type == typeof(TimeOnly))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_TimeOnlyConverter ??= new TimeOnlyConverter());
#else
			converter = (MessagePackConverter)(_TimeOnlyConverter ??= new TimeOnlyConverter());
#endif
			return true;
		}

#if NET
		if (typeof(T) == typeof(DateOnly))
#else
		if (type == typeof(DateOnly))
#endif
		{
#if NET
			converter = (MessagePackConverter<T>)(_DateOnlyConverter ??= new DateOnlyConverter());
#else
			converter = (MessagePackConverter)(_DateOnlyConverter ??= new DateOnlyConverter());
#endif
			return true;
		}

#endif

#if NET
		string primitiveTypeName = typeof(T).Name;
#else
		string primitiveTypeName = type.Name;
#endif
		string? primitiveTypeNamespace = null;

#if NET
		if (primitiveTypeName == "BigInteger" && (primitiveTypeNamespace ??= typeof(T).Namespace) == "System.Numerics")
#else
		if (primitiveTypeName == "BigInteger" && (primitiveTypeNamespace ??= type.Namespace) == "System.Numerics")
#endif
		{
#if NET
			converter = (MessagePackConverter<T>?)(_BigIntegerConverter ??= CreateBigIntegerConverter<T>());
#else
			converter = (MessagePackConverter?)(_BigIntegerConverter ??= CreateBigIntegerConverter(type));
#endif
			return converter is not null;
		}

#if NET
		if (primitiveTypeName == "Color" && (primitiveTypeNamespace ??= typeof(T).Namespace) == "System.Drawing")
#else
		if (primitiveTypeName == "Color" && (primitiveTypeNamespace ??= type.Namespace) == "System.Drawing")
#endif
		{
#if NET
			converter = (MessagePackConverter<T>?)(_SystemDrawingColorConverter ??= CreateSystemDrawingColorConverter<T>());
#else
			converter = (MessagePackConverter?)(_SystemDrawingColorConverter ??= CreateSystemDrawingColorConverter(type));
#endif
			return converter is not null;
		}

#if NET
		if (primitiveTypeName == "Point" && (primitiveTypeNamespace ??= typeof(T).Namespace) == "System.Drawing")
#else
		if (primitiveTypeName == "Point" && (primitiveTypeNamespace ??= type.Namespace) == "System.Drawing")
#endif
		{
#if NET
			converter = (MessagePackConverter<T>?)(_SystemDrawingPointConverter ??= CreateSystemDrawingPointConverter<T>());
#else
			converter = (MessagePackConverter?)(_SystemDrawingPointConverter ??= CreateSystemDrawingPointConverter(type));
#endif
			return converter is not null;
		}

		converter = null;
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
#if NET
	private static IMessagePackConverterInternal? CreateBigIntegerConverter<T>() => typeof(T) == typeof(System.Numerics.BigInteger) ? new BigIntegerConverter() : null;
#else
	private static IMessagePackConverterInternal? CreateBigIntegerConverter(Type type) => type == typeof(System.Numerics.BigInteger) ? new BigIntegerConverter() : null;
#endif

	[MethodImpl(MethodImplOptions.NoInlining)]
#if NET
	private static IMessagePackConverterInternal? CreateSystemDrawingColorConverter<T>() => typeof(T) == typeof(System.Drawing.Color) ? new SystemDrawingColorConverter() : null;
#else
	private static IMessagePackConverterInternal? CreateSystemDrawingColorConverter(Type type) => type == typeof(System.Drawing.Color) ? new SystemDrawingColorConverter() : null;
#endif

	[MethodImpl(MethodImplOptions.NoInlining)]
#if NET
	private static IMessagePackConverterInternal? CreateSystemDrawingPointConverter<T>() => typeof(T) == typeof(System.Drawing.Point) ? new SystemDrawingPointConverter() : null;
#else
	private static IMessagePackConverterInternal? CreateSystemDrawingPointConverter(Type type) => type == typeof(System.Drawing.Point) ? new SystemDrawingPointConverter() : null;
#endif
}
