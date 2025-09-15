// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic abstract base class for a type union that is centered on a common base type.
/// </summary>
/// <remarks>
/// Users should create instances of the derived <see cref="DerivedShapeMapping{TBase}"/> or <see cref="DerivedTypeMapping{TBase}"/> classes
/// to define unions at runtime,
/// or use the <see cref="CreateDisabled(Type)"/> method to disable an attribute-described union.
/// </remarks>
public abstract class DerivedTypeUnion
{
	/// <summary>
	/// Gets the base union type.
	/// </summary>
	public abstract Type BaseType { get; }

	/// <summary>
	/// Gets a value indicating whether the union behavior is disabled on the <see cref="BaseType"/>.
	/// </summary>
	/// <value>The default value is <see langword="false"/>.</value>
	/// <remarks>
	/// As the default behavior is non-union behavior anyway,
	/// this property is primarily useful for forcing the serializer to ignore any and all
	/// <see cref="DerivedTypeShapeAttribute"/> that may be present on the <see cref="BaseType"/>.
	/// </remarks>
	public virtual bool Disabled => false;

	/// <summary>
	/// Gets a value indicating whether this instance has been frozen to prevent further mutation.
	/// </summary>
	internal bool IsFrozen { get; private set; }

	/// <summary>
	/// Creates a <see cref="DerivedTypeUnion"/> instance that disables a derived type union
	/// that may be discovered from one or more <see cref="DerivedTypeShapeAttribute"/> that
	/// may be found on the given <paramref name="baseType"/>.
	/// </summary>
	/// <param name="baseType">The base type to treat as an ordinary class instead of the base of a union.</param>
	/// <returns>The union disabling object.</returns>
	public static DerivedTypeUnion CreateDisabled(Type baseType) => new DisabledUnion(baseType);

	/// <summary>
	/// Freezes this instance to prevent further mutation.
	/// </summary>
	internal void Freeze() => this.IsFrozen = true;

	/// <summary>
	/// This member is only present to ensure that no externally declared derived types may exist.
	/// </summary>
	internal abstract void InternalDerivationsOnly();

	/// <summary>
	/// Throws an <see cref="InvalidOperationException"/> if this instance has been frozen.
	/// </summary>
	protected void ThrowIfFrozen() => Verify.Operation(!this.IsFrozen, "This instance has been frozen and may not be changed further.");

	private class DisabledUnion(Type baseType) : DerivedTypeUnion
	{
		public override Type BaseType => baseType;

		public override bool Disabled => true;

		internal override void InternalDerivationsOnly() => throw new NotImplementedException();
	}
}
