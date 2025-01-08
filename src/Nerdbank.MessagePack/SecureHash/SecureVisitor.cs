// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A visitor that creates a hash collision resistant <see cref="IEqualityComparer{T}"/> for a given type shape
/// that compares values by value (deeply).
/// </summary>
internal class SecureVisitor(TypeGenerationContext context) : TypeShapeVisitor, ITypeShapeFunc
{
	/// <summary>
	/// A dictionary of primitive types and their corresponding hash-resistant equality comparers.
	/// </summary>
	internal static readonly FrozenDictionary<Type, object> HashCollisionResistantPrimitiveEqualityComparers = new Dictionary<Type, object>()
	{
		{ typeof(char), new CollisionResistantHasherUnmanaged<char>() },
		{ typeof(byte), new CollisionResistantHasherUnmanaged<byte>() },
		{ typeof(ushort), new CollisionResistantHasherUnmanaged<ushort>() },
		{ typeof(uint), new CollisionResistantHasherUnmanaged<uint>() },
		{ typeof(ulong), new CollisionResistantHasherUnmanaged<ulong>() },
		{ typeof(sbyte), new CollisionResistantHasherUnmanaged<sbyte>() },
		{ typeof(short), new CollisionResistantHasherUnmanaged<short>() },
		{ typeof(int), new CollisionResistantHasherUnmanaged<int>() },
		{ typeof(long), new CollisionResistantHasherUnmanaged<long>() },
		{ typeof(BigInteger), new HashCollisionResistantPrimitives.BigIntegerEqualityComparer() },
		{ typeof(string), new HashCollisionResistantPrimitives.StringEqualityComparer() },
		{ typeof(bool), new HashCollisionResistantPrimitives.BooleanEqualityComparer() },
		{ typeof(Version), new HashCollisionResistantPrimitives.VersionEqualityComparer() },
		{ typeof(Uri), new HashCollisionResistantPrimitives.AlreadySecureEqualityComparer<Uri>() },
		{ typeof(float), new HashCollisionResistantPrimitives.SingleEqualityComparer() },
		{ typeof(double), new HashCollisionResistantPrimitives.DoubleEqualityComparer() },
		{ typeof(decimal), new HashCollisionResistantPrimitives.DecimalEqualityComparer() },
		{ typeof(DateTime), new HashCollisionResistantPrimitives.DateTimeEqualityComparer() },
		{ typeof(DateTimeOffset), new HashCollisionResistantPrimitives.DateTimeOffsetEqualityComparer() },
		{ typeof(TimeSpan), new CollisionResistantHasherUnmanaged<TimeSpan>() },
		{ typeof(Guid), new CollisionResistantHasherUnmanaged<Guid>() },
#if NET
		{ typeof(Int128), new CollisionResistantHasherUnmanaged<Int128>() },
		{ typeof(UInt128), new CollisionResistantHasherUnmanaged<UInt128>() },
		{ typeof(Rune), new CollisionResistantHasherUnmanaged<Rune>() },
		{ typeof(Half), new HashCollisionResistantPrimitives.HalfEqualityComparer() },
		{ typeof(TimeOnly), new CollisionResistantHasherUnmanaged<TimeOnly>() },
		{ typeof(DateOnly), new CollisionResistantHasherUnmanaged<DateOnly>() },
#endif
	}.ToFrozenDictionary();

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
	{
		// Check if the type has a built-in converter.
		if (HashCollisionResistantPrimitiveEqualityComparers.TryGetValue(typeof(T), out object? defaultComparer))
		{
			return defaultComparer;
		}

		// Otherwise, build a converter using the visitor.
		return typeShape.Accept(this);
	}

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (HashCollisionResistantPrimitiveEqualityComparers.TryGetValue(objectShape.Type, out object? primitiveEqualityComparer))
		{
			return primitiveEqualityComparer;
		}

		if (typeof(T) == typeof(byte[]))
		{
			return HashCollisionResistantPrimitives.ByteArrayEqualityComparer.Default;
		}

		if (typeof(IDeepSecureEqualityComparer<T>).IsAssignableFrom(objectShape.Type))
		{
			return SecureCustomEqualityComparer<T>.Default;
		}

		SecureAggregatingEqualityComparer<T> aggregatingEqualityComparer = new([
			.. from property in objectShape.Properties
			   where property.HasGetter
			   select (SecureEqualityComparer<T>)property.Accept(this, null)!]);

		if (aggregatingEqualityComparer.IsEmpty)
		{
			throw new NotSupportedException($"The type {objectShape.Type} has no properties to compare by value.");
		}

		return aggregatingEqualityComparer;
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
		=> new SecurePropertyEqualityComparer<TDeclaringType, TPropertyType>(propertyShape.GetGetter(), this.GetEqualityComparer(propertyShape.PropertyType));

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
		=> typeof(IReadOnlyList<TElement>).IsAssignableFrom(typeof(TEnumerable)) ? new SecureIReadOnlyListEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType)) :
		new SecureEnumerableEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType), enumerableShape.GetGetEnumerable());

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> new SecureDictionaryEqualityComparer<TDictionary, TKey, TValue>(dictionaryShape.GetGetDictionary(), this.GetEqualityComparer(dictionaryShape.KeyType), this.GetEqualityComparer(dictionaryShape.ValueType));

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> new HashCollisionResistantPrimitives.CollisionResistantEnumHasher<TEnum, TUnderlying>(this.GetEqualityComparer(enumShape.UnderlyingType));

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null)
		=> new SecureNullableEqualityComparer<T>(this.GetEqualityComparer(nullableShape.ElementType));

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
