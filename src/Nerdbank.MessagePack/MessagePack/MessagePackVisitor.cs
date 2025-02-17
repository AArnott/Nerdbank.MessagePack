// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Nerdbank.PolySerializer.MessagePack.Converters;
using PolyType.Utilities;

namespace Nerdbank.PolySerializer.MessagePack;

internal class MessagePackVisitor(ConverterCache owner, TypeGenerationContext context) : StandardVisitor(owner, context)
{
	internal override Formatter Formatter => MessagePackSerializer.MyFormatter;

	internal override Deformatter Deformatter => MessagePackSerializer.MyDeformatter;

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
