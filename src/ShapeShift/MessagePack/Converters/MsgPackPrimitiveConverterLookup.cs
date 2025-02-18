// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.MessagePack.Converters;

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
internal static class MsgPackPrimitiveConverterLookup
{
	private static Converter? _DateTimeConverter;
	private static Converter? _DateTimeOffsetConverter;
	private static Converter? _StringConverter;

	/// <summary>
	/// Gets a built-in converter for the given type, if one is available.
	/// </summary>
	/// <typeparam name="T">The type to get a converter for.</typeparam>
	/// <param name="converter">Receives the converter, if one is available.</param>
	/// <returns><see langword="true" /> if a converter was found; <see langword="false" /> otherwise.</returns>
	internal static bool TryGetPrimitiveConverter<T>([NotNullWhen(true)] out Converter<T>? converter)
	{
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

		if (typeof(T) == typeof(string))
		{
			converter = (Converter<T>)(_StringConverter ??= new StringConverter());
			return true;
		}

		converter = null;
		return false;
	}
}
