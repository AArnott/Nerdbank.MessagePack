// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <inheritdoc cref="DerivedShapeMapping{TBase}" path="/summary"/>
/// <inheritdoc cref="DerivedShapeMapping{TBase}" path="/typeparam"/>
/// <param name="provider">The type shape provider to use for a type when added without an express shape or provider.</param>
public class DerivedTypeMapping<TBase>(ITypeShapeProvider provider) : DerivedShapeMapping<TBase>
{
	/// <inheritdoc/>
	public override Type BaseType => typeof(TBase);

	/// <summary>
	/// Gets or sets a sub-type's <see cref="Type"/> by its alias.
	/// </summary>
	/// <param name="alias">The alias of the sub-type.</param>
	/// <returns>The <see cref="Type"/> associated with the specified alias.</returns>
	/// <remarks>
	/// When adding a subtype via this indexer, the <see cref="ITypeShape{T}"/> for the <paramref name="value"/> will be obtained from the <see cref="ITypeShapeProvider"/> provided to the constructor.
	/// </remarks>
	public Type this[DerivedTypeIdentifier alias]
	{
		get => this.Get(alias).Type;
		set
		{
			Requires.Argument(typeof(TBase).IsAssignableFrom(value), nameof(value), $"Type must be assignable to {typeof(TBase).Name}.");
			this.Set(alias, provider.GetTypeShapeOrThrow(value));
		}
	}

	/// <summary>
	/// Adds a known sub-type to the mapping.
	/// </summary>
	/// <param name="alias">The alias for the sub-type.</param>
	/// <param name="type">The <see cref="Type"/> of the sub-type.</param>
	/// <remarks>
	/// The <see cref="ITypeShape{T}"/> for the <paramref name="type"/> will be obtained from the <see cref="ITypeShapeProvider"/> provided to the constructor.
	/// </remarks>
	public void Add(DerivedTypeIdentifier alias, Type type)
	{
		Requires.Argument(typeof(TBase).IsAssignableFrom(type), nameof(type), $"Type must be assignable to {typeof(TBase).Name}.");
		this.Add(alias, provider.GetTypeShapeOrThrow(type));
	}
}
