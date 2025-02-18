// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.Utilities;
using ShapeShift.MessagePack.Converters;

namespace ShapeShift.MessagePack;

/// <summary>
/// A messagepack-specific implementation of <see cref="StandardVisitor"/>.
/// </summary>
/// <param name="owner"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='owner']"/></param>
/// <param name="context"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='context']"/></param>
internal class MessagePackVisitor(ConverterCache owner, TypeGenerationContext context)
	: StandardVisitor(owner, context)
{
	/// <inheritdoc/>
	protected override bool TryGetPrimitiveConverter<T>([NotNullWhen(true)] out Converter<T>? converter)
		=> MsgPackPrimitiveConverterLookup.TryGetPrimitiveConverter(out converter) || base.TryGetPrimitiveConverter(out converter);

	/// <inheritdoc/>
	protected override bool TryGetArrayOfPrimitivesConverter<TArray, TElement>(Func<TArray, IEnumerable<TElement>> getEnumerable, SpanConstructor<TElement, TArray> constructor, [NotNullWhen(true)] out Converter<TArray>? converter)
	{
		if (ArraysOfPrimitivesConverters.TryGetConverter(getEnumerable, constructor, out Converter<TArray>? msgpackConverter))
		{
			converter = msgpackConverter;
			return true;
		}
		else
		{
			converter = null;
			return false;
		}
	}

#if NET
	/// <inheritdoc/>
	protected override bool TryGetHardwareAcceleratedConverter<TArray, TElement>(out Converter<TArray>? converter)
	{
		if (HardwareAccelerated.TryGetConverter<TArray, TElement>(out Converter<TArray>? msgpackConverter))
		{
			converter = msgpackConverter;
			return true;
		}

		converter = null;
		return false;
	}
#endif
}
