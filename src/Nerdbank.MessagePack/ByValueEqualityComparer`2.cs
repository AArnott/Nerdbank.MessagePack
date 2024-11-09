// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack.SecureHash;

namespace Nerdbank.MessagePack;

/// <summary>
/// Provides deep by-value implementations of <see cref="IEqualityComparer{T}"/> for arbitrary data types.
/// </summary>
/// <typeparam name="T">The data type to be hashed or compared by value.</typeparam>
/// <typeparam name="TProvider">The provider of the type shape.</typeparam>
/// <remarks>
/// <para>
/// The deep walking of the object graph for deep by-value equality and hashing is based on the same
/// <see cref="GenerateShapeAttribute"/> that is used to generate MessagePack serializers.
/// The implementation therefore considers all the same properties for equality and hashing that would
/// be included in a serialized copy.
/// </para>
/// <para>
/// This implementation is not suitable for all types. Specifically, it is not suitable for types that
/// have multiple memory representations that are considered equal.
/// An invariant for <see cref="IEqualityComparer{T}" /> behavior must be that if
/// <c>x.Equals(y)</c> then <c>x.GetHashCode() == y.GetHashCode()</c>.
/// For an auto-generated implementation of these methods for arbitrary types such as this,
/// no specialization for multiple values that are considered equal is possible.
/// </para>
/// <para>
/// For example, a <see cref="double"/> value has distinct memory representations for <c>0.0</c> and <c>-0.0</c>,
/// yet these two values are considered equal and must have the same hash code.
/// In this case and for several other common data types included with .NET, special consideration is built-in
/// for correct operation.
/// But this cannot be done automatically for any user-defined types.
/// </para>
/// <para>
/// When using user-defined types for which this implementation is inappropriate,
/// a custom implementation of <see cref="IEqualityComparer{T}"/> may be used if the type is used directly.
/// But if the type is referenced in a type reference graph such that is used for by-value comparison,
/// implementing <see cref="IDeepSecureEqualityComparer{T}"/> on that type will allow the type to take control
/// of just its contribution to the hash code and equality comparison.
/// </para>
/// <para>
/// Types that define no (public or opted in) properties and do not implement <see cref="IDeepSecureEqualityComparer{T}"/> will throw a <see cref="NotSupportedException"/> when attempting to create an equality comparer.
/// </para>
/// <para>
/// This implementation should only be used for acyclic graphs, since cyclic graphs will cause a
/// <see cref="StackOverflowException"/> while performing the comparison.
/// </para>
/// <para>
/// Another consideration is that types used as keys in collections should generally not have a changing hash code
/// or the collections internal data structures may become corrupted by a key that is stored in the wrong hash bucket.
/// Keys should generally be immutable to prevent this, or at least immutable in the elements that contribute to the hash code.
/// In an automated equality comparer such as the one produced by this class, all public properties contribute to the hash code,
/// even if they are mutable.
/// Care should therefore be taken to not mutate properties on objects used as keys in collections.
/// </para>
/// <para>
/// The values are compared by their declared types rather than polymorphism.
/// If some type has a property of type Foo, and the actual value at runtime derives from Foo, only the properties on Foo will be considered.
/// If between two object graphs being equality checked, their runtime types do not match, the equality check will return <see langword="false" />.
/// </para>
/// </remarks>
public static class ByValueEqualityComparer<T, TProvider>
	where TProvider : IShapeable<T>
{
	private static IEqualityComparer<T>? defaultEqualityComparer;

	private static IEqualityComparer<T>? hashResistantEqualityComparer;

	/// <summary>
	/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, without hash collision resistance.
	/// </summary>
	/// <remarks>
	/// See the remarks on the class for important notes about correctness of this implementation.
	/// </remarks>
	public static IEqualityComparer<T> Default => defaultEqualityComparer ??= (IEqualityComparer<T>)TProvider.GetShape().Accept(new ByValueVisitor())!;

	/// <summary>
	/// Gets a deep by-value equality comparer for the type <typeparamref name="T"/>, with hash collision resistance.
	/// </summary>
	/// <remarks>
	/// See the remarks on the class for important notes about correctness of this implementation.
	/// </remarks>
	public static IEqualityComparer<T> HashResistant => hashResistantEqualityComparer ??= (IEqualityComparer<T>)TProvider.GetShape().Accept(new SecureVisitor())!;
}
