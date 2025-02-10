﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
