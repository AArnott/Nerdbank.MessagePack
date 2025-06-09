// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An interface that may be implemented by user-defined types in order to provide their own
/// deep (i.e. giving all values a chance to contribute) hash code.
/// </summary>
/// <typeparam name="T">The same type that is implementing this interface.</typeparam>
/// <remarks>
/// <para>
/// When a type implements this interface, <see cref="GetHashCode"/> and <see cref="StructuralEquals(T?)"/>
/// is used to determine equality and hash codes for the type by the
/// <see cref="StructuralEqualityComparer"/> equality comparer
/// instead of the deep by-value automatic implementation.
/// </para>
/// </remarks>
public interface IStructuralSecureEqualityComparer<T>
{
	/// <summary>
	/// Tests deep equality of this object with another object.
	/// </summary>
	/// <param name="other">The other object.</param>
	/// <returns><see langword="true" /> if the two objects are deeply equal.</returns>
	/// <remarks>
	/// An implementation may use <see cref="StructuralEqualityComparer.GetDefault{T}(ITypeShape{T})"/> to obtain equality comparers for any sub-values that must be tested.
	/// </remarks>
	bool StructuralEquals(T? other);

	/// <summary>
	/// Gets a collision resistant hash code for this object.
	/// </summary>
	/// <returns>A 64-bit integer.</returns>
	/// <exception cref="NotSupportedException">May be thrown if not supported.</exception>
	/// <remarks>
	/// This method should be compatible with <see cref="StructuralEquals(T?)"/>
	/// such that if two objects are structurally equal, they will return the same hash code.
	/// </remarks>
	long GetSecureHashCode();

	/// <summary>
	/// Gets a hash code for this object, which may not be collision resistant.
	/// </summary>
	/// <returns>A 32-bit integer.</returns>
	/// <remarks>
	/// <para>
	/// This method should be compatible with <see cref="StructuralEquals(T?)"/>
	/// such that if two objects are structurally equal, they will return the same hash code.
	/// On types that implement <see cref="object.Equals(object?)"/> with shallow equality,
	/// this means this <see cref="GetHashCode"/> method may need to be implemented as an
	/// explicit interface implementation so it can have structural equality semantics
	/// while <see cref="object.GetHashCode"/> can match the default shallow equality semantics.
	/// </para>
	/// <para>
	/// On .NET, the default implementation of this method is to truncate the result of <see cref="GetSecureHashCode"/>.
	/// </para>
	/// </remarks>
	int GetHashCode()
#if NET
		=> unchecked((int)this.GetSecureHashCode());
#else
		;
#endif
}
