// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A visitor that creates an <see cref="IEqualityComparer{T}"/> for a given type shape that compares values by value (deeply).
/// </summary>
internal class ByValueVisitor(TypeGenerationContext context) : TypeShapeVisitor, ITypeShapeFunc
{
	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => typeShape.Accept(this);

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (SecureVisitor.HashCollisionResistantPrimitiveEqualityComparers.ContainsKey(objectShape.Type))
		{
			// The type is a primitive, so we can rely on by-value equality being implemented by the default equality comparer.
			return EqualityComparer<T>.Default;
		}

		if (typeof(T) == typeof(byte[]))
		{
			return ByValueByteArrayEqualityComparer.Default;
		}

		if (typeof(IDeepSecureEqualityComparer<T>).IsAssignableFrom(objectShape.Type))
		{
			return ByValueCustomEqualityComparer<T>.Default;
		}

		ByValueAggregatingEqualityComparer<T> aggregatingEqualityComparer = new([
			.. from property in objectShape.Properties
			   where property.HasGetter
			   select (IEqualityComparer<T>)property.Accept(this, null)!]);

		if (aggregatingEqualityComparer.IsEmpty)
		{
			throw new NotSupportedException($"The type {objectShape.Type} has no properties to compare by value.");
		}

		return aggregatingEqualityComparer;
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
		=> new ByValuePropertyEqualityComparer<TDeclaringType, TPropertyType>(propertyShape.GetGetter(), this.GetEqualityComparer(propertyShape.PropertyType));

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
		=> typeof(IReadOnlyList<TElement>).IsAssignableFrom(typeof(TEnumerable)) ? new ByValueIReadOnlyListEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType)) :
			new ByValueEnumerableEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType), enumerableShape.GetGetEnumerable());

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> new ByValueDictionaryEqualityComparer<TDictionary, TKey, TValue>(dictionaryShape.GetGetDictionary(), this.GetEqualityComparer(dictionaryShape.KeyType), this.GetEqualityComparer(dictionaryShape.ValueType));

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> EqualityComparer<TEnum>.Default;

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null)
		=> new ByValueNullableEqualityComparer<T>(this.GetEqualityComparer(nullableShape.ElementType));

	/// <summary>
	/// Gets or creates an equality comparer for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the equality comparer.</param>
	/// <returns>The equality comparer.</returns>
	protected IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape, object? state = null)
		=> (IEqualityComparer<T>)context.GetOrAdd(shape, state)!;

	/// <summary>
	/// A factory that creates delayed equality comparers.
	/// </summary>
	internal class DelayedEqualityComparerFactory : IDelayedValueFactory
	{
		/// <inheritdoc/>
		public DelayedValue Create<T>(ITypeShape<T> typeShape)
			=> new DelayedValue<IEqualityComparer<T>>(self => new DelayedEqualityComparer<T>(self));

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

		private class DelayedEqualityComparer<T>(DelayedValue<IEqualityComparer<T>> self) : IEqualityComparer<T>
		{
			public bool Equals(T? x, T? y) => self.Result.Equals(x, y);

			public int GetHashCode([DisallowNull] T obj) => self.Result.GetHashCode(obj);
		}
	}
}
