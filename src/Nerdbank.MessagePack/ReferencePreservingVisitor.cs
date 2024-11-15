// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A visitor that wraps the result of another visitor with a reference-preserving converter.
/// </summary>
/// <param name="inner">The inner visitor.</param>
internal class ReferencePreservingVisitor(ITypeShapeVisitor inner) : TypeShapeVisitor
{
	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> inner.VisitEnum(enumShape, state);

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null)
		=> inner.VisitNullable(nullableShape, state);

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> ((IMessagePackConverter)inner.VisitDictionary(dictionaryShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
		=> ((IMessagePackConverter)inner.VisitEnumerable(enumerableShape, state)!).WrapWithReferencePreservation();

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
		=> ((IMessagePackConverter)inner.VisitObject(objectShape, state)!).WrapWithReferencePreservation();
}
