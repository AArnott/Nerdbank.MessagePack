﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A visitor that wraps the result of another visitor with a reference-preserving converter.
/// </summary>
/// <param name="inner">The inner visitor.</param>
internal class ReferencePreservingVisitor(TypeShapeVisitor inner) : TypeShapeVisitor
{
	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> inner.VisitEnum(enumShape, state);

	/// <inheritdoc/>
	public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null)
		=> inner.VisitOptional(optionalShape, state);

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> ((IMessagePackConverterInternal)inner.VisitDictionary(dictionaryShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
		=> ((IMessagePackConverterInternal)inner.VisitEnumerable(enumerableShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
		=> ((IMessagePackConverterInternal)inner.VisitObject(objectShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null)
		=> ((IMessagePackConverterInternal)inner.VisitUnion(unionShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null)
		=> inner.VisitUnionCase(unionCaseShape, state);

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
		=> ((IMessagePackConverterInternal)inner.VisitSurrogate(surrogateShape, state)!).WrapWithReferencePreservation();
}
