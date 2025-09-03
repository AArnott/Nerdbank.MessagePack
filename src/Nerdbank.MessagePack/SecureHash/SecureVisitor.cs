// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A visitor that creates a hash collision resistant <see cref="IEqualityComparer{T}"/> for a given type shape
/// that compares values by value (deeply).
/// </summary>
internal class SecureVisitor(TypeGenerationContext context) : TypeShapeVisitor, ITypeShapeFunc
{
	private static readonly object IsUnionSentinel = new();

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
	{
		// Check if the type has a built-in converter.
		if (CollisionResistantHasherLookup.TryGetPrimitiveHasher(out SecureEqualityComparer<T>? defaultComparer))
		{
			return defaultComparer;
		}

		// Otherwise, build a converter using the visitor.
		return typeShape.Accept(this, state);
	}

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (CollisionResistantHasherLookup.TryGetPrimitiveHasher(out SecureEqualityComparer<T>? primitiveEqualityComparer))
		{
			return primitiveEqualityComparer;
		}

		if (typeof(T) == typeof(byte[]))
		{
			return HashCollisionResistantPrimitives.ByteArrayEqualityComparer.Default;
		}

		if (typeof(IStructuralSecureEqualityComparer<T>).IsAssignableFrom(objectShape.Type))
		{
			return SecureCustomEqualityComparer<T>.Default;
		}

		// Do NOT blindly propagate state to the properties, because we don't want the Union sentinel
		// to be applied from this object (which may be a union case) to its properties.
		SecureAggregatingEqualityComparer<T> aggregatingEqualityComparer = new([
			.. from property in objectShape.Properties
			   where property.HasGetter
			   select (SecureEqualityComparer<T>)property.Accept(this, null)!]);

		if (aggregatingEqualityComparer.IsEmpty && state != IsUnionSentinel)
		{
			throw new NotSupportedException($"The type {objectShape.Type} has no properties to compare by value.");
		}

		return aggregatingEqualityComparer;
	}

	/// <inheritdoc/>
	public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null)
	{
		Getter<TUnion, int> getUnionCaseIndex = unionShape.GetGetUnionCaseIndex();
		SecureEqualityComparer<TUnion> baseComparer = (SecureEqualityComparer<TUnion>)unionShape.BaseType.Invoke(this, IsUnionSentinel)!;
		SecureEqualityComparer<TUnion>[] comparers = [.. unionShape.UnionCases.Select(
			unionCase => (SecureEqualityComparer<TUnion>)unionCase.Accept(this, IsUnionSentinel)!)];
		return new SecureUnionEqualityComparer<TUnion>(
			(ref TUnion value) => getUnionCaseIndex(ref value) is int idx && idx >= 0 ? (comparers[idx], idx) : (baseComparer, null));
	}

	/// <inheritdoc/>
	public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null)
	{
		// NB: don't use the cached converter for TUnionCase, as it might equal TUnion.
		var caseComparer = (SecureEqualityComparer<TUnionCase>)unionCaseShape.Type.Invoke(this, IsUnionSentinel)!;
		return new SecureUnionCaseEqualityComparer<TUnionCase, TUnion>(caseComparer, unionCaseShape.Marshaler);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
		=> new SecurePropertyEqualityComparer<TDeclaringType, TPropertyType>(propertyShape.GetGetter(), this.GetEqualityComparer(propertyShape.PropertyType));

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
	{
		if (enumerableShape.IsAsyncEnumerable)
		{
			throw new NotSupportedException("IAsyncEnumerable<T> cannot be effectively compared by value.");
		}

		return typeof(IReadOnlyList<TElement>).IsAssignableFrom(typeof(TEnumerable)) ? new SecureIReadOnlyListEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType)) :
			new SecureEnumerableEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType), enumerableShape.GetGetEnumerable());
	}

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> new SecureDictionaryEqualityComparer<TDictionary, TKey, TValue>(dictionaryShape.GetGetDictionary(), this.GetEqualityComparer(dictionaryShape.KeyType), this.GetEqualityComparer(dictionaryShape.ValueType));

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> new HashCollisionResistantPrimitives.CollisionResistantEnumHasher<TEnum, TUnderlying>(this.GetEqualityComparer(enumShape.UnderlyingType));

	/// <inheritdoc/>
	public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null)
		=> new SecureOptionalEqualityComparer<TOptional, TElement>(this.GetEqualityComparer(optionalShape.ElementType, state), optionalShape.GetDeconstructor());

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
		=> new SurrogateSecureEqualityComparer<T, TSurrogate>(surrogateShape.Marshaler, this.GetEqualityComparer(surrogateShape.SurrogateType, state));

	/// <inheritdoc/>
	public override object? VisitFunction<TFunction, TArgumentState, TResult>(IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape, object? state = null)
		=> throw new NotSupportedException("Delegate typed properties cannot be compared.");

	/// <summary>
	/// Gets or creates an equality comparer for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the equality comparer.</param>
	/// <returns>The equality comparer.</returns>
	protected SecureEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape, object? state = null)
		=> (SecureEqualityComparer<T>)context.GetOrAdd(shape, state)!;

	/// <summary>
	/// A factory that creates delayed equality comparers.
	/// </summary>
	internal class DelayedEqualityComparerFactory : IDelayedValueFactory
	{
		/// <inheritdoc/>
		public DelayedValue Create<T>(ITypeShape<T> typeShape)
			=> new DelayedValue<SecureEqualityComparer<T>>(self => new DelayedEqualityComparer<T>(self));

		private class DelayedEqualityComparer<T>(DelayedValue<SecureEqualityComparer<T>> inner) : SecureEqualityComparer<T>
		{
			/// <inheritdoc/>
			public override bool Equals(T? x, T? y) => inner.Result.Equals(x, y);

			/// <inheritdoc/>
			public override long GetSecureHashCode([DisallowNull] T obj) => inner.Result.GetSecureHashCode(obj);
		}
	}
}
