// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.Utilities;
using ShapeShift.Json.Converters;

namespace ShapeShift.Json;

/// <summary>
/// A JSON-specific implementation of <see cref="StandardVisitor"/>.
/// </summary>
/// <param name="owner"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='owner']"/></param>
/// <param name="context"><inheritdoc cref="StandardVisitor.StandardVisitor(ShapeShift.ConverterCache, TypeGenerationContext)" path="/param[@name='context']"/></param>
internal class JsonVisitor(ConverterCache owner, TypeGenerationContext context)
	: StandardVisitor(owner, context)
{
	/// <inheritdoc/>
	protected override bool TryGetPrimitiveConverter<T>([NotNullWhen(true)] out Converter<T>? converter)
		=> JsonPrimitiveConverterLookup.TryGetPrimitiveConverter(out converter) || base.TryGetPrimitiveConverter(out converter);
}
