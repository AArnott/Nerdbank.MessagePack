// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer;

/// <summary>
/// A visitor that wraps the result of another visitor with a reference-preserving converter.
/// </summary>
/// <param name="inner">The inner visitor.</param>
internal class ReferencePreservingVisitor(ITypeShapeVisitor inner, IReferencePreservingManager manager) : TypeShapeVisitor
{
	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> inner.VisitEnum(enumShape, state);

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null)
		=> inner.VisitNullable(nullableShape, state);

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> manager.WrapWithReferencePreservingConverter((Converter)inner.VisitDictionary(dictionaryShape, state)!);

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
		=> manager.WrapWithReferencePreservingConverter((Converter)inner.VisitEnumerable(enumerableShape, state)!);

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
		=> manager.WrapWithReferencePreservingConverter((Converter)inner.VisitObject(objectShape, state)!);

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
		=> manager.WrapWithReferencePreservingConverter((Converter)inner.VisitSurrogate(surrogateShape, state)!);
}
