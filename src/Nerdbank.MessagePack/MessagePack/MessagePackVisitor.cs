// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Nerdbank.PolySerializer.Converters;
using Nerdbank.PolySerializer.MessagePack.Converters;
using PolyType.Utilities;

namespace Nerdbank.PolySerializer.MessagePack;

internal class MessagePackVisitor : StandardVisitor
{
	private static readonly InterningStringConverter InterningStringConverter = new();
	private static readonly MessagePackConverter<string> ReferencePreservingInterningStringConverter = (MessagePackConverter<string>)InterningStringConverter.WrapWithReferencePreservation();

	public MessagePackVisitor(ConverterCache owner, TypeGenerationContext context)
		: base(owner, context)
	{
	}

	protected override Converter GetInterningStringConverter() => InterningStringConverter;

	protected override Converter GetReferencePreservingInterningStringConverter() => ReferencePreservingInterningStringConverter;

	protected override bool TryGetPrimitiveConverter<T>(bool preserveReferences, out Converter<T>? converter)
	{
		if (PrimitiveConverterLookup.TryGetPrimitiveConverter<T>(preserveReferences, out MessagePackConverter<T>? msgpackConverter))
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

	protected override Converter CreateSubTypeUnionConverter<T>(SubTypes unionTypes, Converter<T> baseConverter)
		=> new SubTypeUnionConverter<T>(unionTypes, (MessagePackConverter<T>)baseConverter);

	protected override bool TryGetArrayOfPrimitivesConverter<TArray, TElement>(Func<TArray, IEnumerable<TElement>> getEnumerable, SpanConstructor<TElement, TArray> constructor, [NotNullWhen(true)] out Converter<TArray>? converter)
	{
		if (ArraysOfPrimitivesConverters.TryGetConverter(getEnumerable, constructor, out MessagePackConverter<TArray>? msgpackConverter))
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
		if (HardwareAccelerated.TryGetConverter<TArray, TElement>(out MessagePackConverter<TArray>? msgpackConverter))
		{
			converter = msgpackConverter;
			return true;
		}

		converter = null;
		return false;
	}
#endif

	protected override Converter CreateEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter)
		=> new EnumerableConverter<TEnumerable, TElement>(getEnumerable, (MessagePackConverter<TElement>)elementConverter);

	protected override Converter CreateMutableEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, Setter<TEnumerable, TElement> addElement, Func<TEnumerable> defaultConstructor)
		=> new MutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, (MessagePackConverter<TElement>)elementConverter, addElement, defaultConstructor);

	protected override Converter CreateEnumerableEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, Func<IEnumerable<TElement>, TEnumerable> enumerableConstructor)
		=> new EnumerableEnumerableConverter<TEnumerable, TElement>(getEnumerable, (MessagePackConverter<TElement>)elementConverter, enumerableConstructor);

	protected override Converter CreateSpanEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, SpanConstructor<TElement, TEnumerable> spanConstructor)
		=> new SpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, (MessagePackConverter<TElement>)elementConverter, spanConstructor);

#if NET
	protected override Converter CreateArrayWithFlattenedDimensionsConverter<TArray, TElement>(Converter<TElement> elementConverter)
		=> new ArrayWithFlattenedDimensionsConverter<TArray, TElement>((MessagePackConverter<TElement>)elementConverter);

	protected override Converter CreateArrayWithNestedDimensionsConverter<TArray, TElement>(Converter<TElement> elementConverter, int rank)
		=> new ArrayWithNestedDimensionsConverter<TArray, TElement>((MessagePackConverter<TElement>)elementConverter, rank);
#endif
}
