// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.Utilities;
using ShapeShift.MessagePack.Converters;

namespace ShapeShift.MessagePack;

/// <summary>
/// A messagepack-specific implementation of <see cref="StandardVisitor"/>.
/// </summary>
/// <param name="owner">The owning converter cache.</param>
/// <param name="context">The context given by the <see cref="MultiProviderTypeCache"/> factory.</param>
/// <param name="formatter">The formatter.</param>
/// <param name="deformatter">The deformatter.</param>
internal class MessagePackVisitor(ConverterCache owner, TypeGenerationContext context, MsgPackFormatter formatter, MsgPackDeformatter deformatter)
	: StandardVisitor(owner, context)
{
	/// <inheritdoc/>
	internal override Formatter Formatter => formatter;

	/// <inheritdoc/>
	internal override Deformatter Deformatter => deformatter;

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
