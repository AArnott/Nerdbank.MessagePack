﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A visitor that creates a hash collision resistant <see cref="IEqualityComparer{T}"/> for a given type shape
/// that compares values by value (deeply).
/// </summary>
internal class SecureVisitor : TypeShapeVisitor
{
	/// <summary>
	/// A dictionary of primitive types and their corresponding hash-resistant equality comparers.
	/// </summary>
	internal static readonly FrozenDictionary<Type, object> HashResistantPrimitiveEqualityComparers = new Dictionary<Type, object>()
	{
		{ typeof(char), new CollisionResistantHasherUnmanaged<char>() },
		{ typeof(Rune), new CollisionResistantHasherUnmanaged<Rune>() },
		{ typeof(byte), new CollisionResistantHasherUnmanaged<byte>() },
		{ typeof(ushort), new CollisionResistantHasherUnmanaged<ushort>() },
		{ typeof(uint), new CollisionResistantHasherUnmanaged<uint>() },
		{ typeof(ulong), new CollisionResistantHasherUnmanaged<ulong>() },
		{ typeof(sbyte), new CollisionResistantHasherUnmanaged<sbyte>() },
		{ typeof(short), new CollisionResistantHasherUnmanaged<short>() },
		{ typeof(int), new CollisionResistantHasherUnmanaged<int>() },
		{ typeof(long), new CollisionResistantHasherUnmanaged<long>() },
		{ typeof(BigInteger), new HashResistantPrimitives.BigIntegerEqualityComparer() },
		{ typeof(Int128), new CollisionResistantHasherUnmanaged<Int128>() },
		{ typeof(UInt128), new CollisionResistantHasherUnmanaged<UInt128>() },
		{ typeof(string), new HashResistantPrimitives.StringEqualityComparer() },
		{ typeof(bool), new HashResistantPrimitives.BooleanEqualityComparer() },
		{ typeof(Version), new HashResistantPrimitives.VersionEqualityComparer() },
		{ typeof(Uri), new HashResistantPrimitives.AlreadySecureEqualityComparer<Uri>() },
		{ typeof(Half), new HashResistantPrimitives.HalfEqualityComparer() },
		{ typeof(float), new HashResistantPrimitives.SingleEqualityComparer() },
		{ typeof(double), new HashResistantPrimitives.DoubleEqualityComparer() },
		{ typeof(decimal), new HashResistantPrimitives.DecimalEqualityComparer() },
		{ typeof(TimeOnly), new CollisionResistantHasherUnmanaged<TimeOnly>() },
		{ typeof(DateOnly), new CollisionResistantHasherUnmanaged<DateOnly>() },
		{ typeof(DateTime), new HashResistantPrimitives.DateTimeEqualityComparer() },
		{ typeof(DateTimeOffset), new HashResistantPrimitives.DateTimeOffsetEqualityComparer() },
		{ typeof(TimeSpan), new CollisionResistantHasherUnmanaged<TimeSpan>() },
		{ typeof(Guid), new CollisionResistantHasherUnmanaged<Guid>() },
	}.ToFrozenDictionary();

	private readonly TypeDictionary equalityComparers = new();

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (HashResistantPrimitiveEqualityComparers.TryGetValue(objectShape.Type, out object? primitiveEqualityComparer))
		{
			return primitiveEqualityComparer;
		}

		if (typeof(T) == typeof(byte[]))
		{
			return HashResistantPrimitives.ByteArrayEqualityComparer.Default;
		}

		if (typeof(IDeepSecureEqualityComparer<T>).IsAssignableFrom(objectShape.Type))
		{
			return SecureCustomEqualityComparer<T>.Default;
		}

		SecureAggregatingEqualityComparer<T> aggregatingEqualityComparer = new([
			.. from property in objectShape.GetProperties()
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
		=> new SecureEnumerableEqualityComparer<TEnumerable, TElement>(this.GetEqualityComparer(enumerableShape.ElementType), enumerableShape.GetGetEnumerable());

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
		=> new SecureDictionaryEqualityComparer<TDictionary, TKey, TValue>(dictionaryShape.GetGetDictionary(), this.GetEqualityComparer(dictionaryShape.KeyType), this.GetEqualityComparer(dictionaryShape.ValueType));

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> new HashResistantPrimitives.CollisionResistantEnumHasher<TEnum, TUnderlying>(this.GetEqualityComparer(enumShape.UnderlyingType));

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
		=> this.equalityComparers.GetOrAdd<SecureEqualityComparer<T>>(shape, this, box => new DelayedEqualityComparer<T>(box));

	private class DelayedEqualityComparer<T>(ResultBox<SecureEqualityComparer<T>> inner) : SecureEqualityComparer<T>
	{
		public override bool Equals(T? x, T? y) => inner.Result.Equals(x, y);

		public override long GetSecureHashCode([DisallowNull] T obj) => inner.Result.GetSecureHashCode(obj);
	}
}