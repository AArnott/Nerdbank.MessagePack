// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Describes a mapping between a base type and its known sub-types, along with the aliases that identify them.
/// </summary>
/// <typeparam name="TBase">The base type or interface that all sub-types derive from or implement.</typeparam>
/// <remarks>
/// This class requires a type shape or type shape provider to be provided for each sub-type explicitly.
/// Use <see cref="DerivedTypeMapping{TBase}"/> for a more convenient API that allows you to add sub-types by their type alone.
/// </remarks>
public class DerivedShapeMapping<TBase> : DerivedTypeMapping, IEnumerable<KeyValuePair<DerivedTypeIdentifier, ITypeShape>>
{
	private readonly Dictionary<DerivedTypeIdentifier, ITypeShape> map = new();
	private readonly HashSet<Type> addedTypes = new();

	/// <summary>
	/// Gets the number of subtypes described by the mapping.
	/// </summary>
	public int Count => this.map.Count;

	/// <inheritdoc/>
	public override Type BaseType => typeof(TBase);

	/// <summary>
	/// Adds a known sub-type to the mapping.
	/// </summary>
	/// <typeparam name="TDerived">The sub-type.</typeparam>
	/// <param name="alias">The alias for the sub-type.</param>
	/// <param name="typeShape">The shape of the sub-type.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="alias"/> or the <see cref="Type"/> described by <paramref name="typeShape"/> have already been added to this mapping.</exception>
	public void Add<TDerived>(DerivedTypeIdentifier alias, ITypeShape<TDerived> typeShape)
		where TDerived : TBase
		=> this.Add(alias, (ITypeShape)typeShape);

	/// <summary>
	/// Adds a known sub-type to the mapping.
	/// </summary>
	/// <typeparam name="TDerived">The sub-type.</typeparam>
	/// <param name="alias">The alias for the sub-type.</param>
	/// <param name="provider"><inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="alias"/> or <typeparamref name="TDerived"/> has already been added to this mapping.</exception>
	public void Add<TDerived>(DerivedTypeIdentifier alias, ITypeShapeProvider provider)
		where TDerived : TBase
	{
		Requires.NotNull(provider);
		this.Add(alias, provider.Resolve<TDerived>());
	}

#if NET
	/// <inheritdoc cref="Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" />
	public void Add<TDerived>(DerivedTypeIdentifier alias)
		where TDerived : TBase, IShapeable<TDerived> => this.Add(alias, TDerived.GetShape());

	/// <inheritdoc cref="Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/summary" />
	/// <inheritdoc cref="Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/exception" />
	/// <param name="alias"><inheritdoc cref="Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
	/// <typeparam name="TDerived"><inheritdoc cref="Add{TDerived}(DerivedTypeIdentifier, ITypeShape{TDerived})" path="/typeparam[@name='TDerived']"/></typeparam>
	/// <typeparam name="TProvider">The witness class that provides a type shape for <typeparamref name="TDerived"/>.</typeparam>
	public void Add<TDerived, TProvider>(DerivedTypeIdentifier alias)
		where TDerived : TBase
		where TProvider : IShapeable<TDerived>
		=> this.Add(alias, TProvider.GetShape());
#endif

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<DerivedTypeIdentifier, ITypeShape>> GetEnumerator() => this.map.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	/// <inheritdoc />
	internal override FrozenDictionary<DerivedTypeIdentifier, ITypeShape> CreateDerivedTypesMapping() => this.map.ToFrozenDictionary();

	/// <summary>
	/// Adds a type shape to a mapping using a specified identifier, ensuring that the type has not been previously added.
	/// </summary>
	/// <param name="alias">Identifies the type shape being added to the mapping.</param>
	/// <param name="typeShape">Represents the structure and characteristics of the type being added.</param>
	/// <exception cref="ArgumentException">Thrown when the type being added has already been included in the mapping.</exception>
	/// <remarks>
	/// The caller should ensure that the shape's type is actually assignable to <typeparamref name="TBase"/>.
	/// </remarks>
	protected void Add(DerivedTypeIdentifier alias, ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);
		this.map.Add(alias, typeShape);
		if (!this.addedTypes.Add(typeShape.Type))
		{
			this.map.Remove(alias);
			throw new ArgumentException($"The type {typeShape.Type} has already been added to the mapping.", nameof(alias));
		}
	}

	/// <summary>
	/// Adds or re-sets the type shape in the mapping using a specified identifier.
	/// </summary>
	/// <param name="alias">Identifies the type shape being added to the mapping.</param>
	/// <param name="typeShape">Represents the structure and characteristics of the type being added.</param>
	/// <remarks>
	/// The caller should ensure that the shape's type is actually assignable to <typeparamref name="TBase"/>.
	/// </remarks>
	protected void Set(DerivedTypeIdentifier alias, ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);

		if (this.addedTypes.Add(typeShape.Type))
		{
			if (this.map.TryGetValue(alias, out ITypeShape? existing))
			{
				// Another type shape is already at this alias. Remove it.
				this.addedTypes.Remove(existing.Type);
			}

			this.map[alias] = typeShape;
		}
		else
		{
			// The type is already in the map somewhere. Is it with the same alias?
			if (this.map.TryGetValue(alias, out ITypeShape? existing) && existing.Type == typeShape.Type)
			{
				// Yes, same alias. So just replace the shape.
				this.map[alias] = typeShape;
			}
			else
			{
				// The type is already in the map with a different alias.
				throw new ArgumentException($"The type {typeShape.Type} has already been added to the mapping.", nameof(alias));
			}
		}

		this.map[alias] = typeShape;
		this.addedTypes.Add(typeShape.Type);
	}

	/// <summary>
	/// Gets the type shape for a given alias.
	/// </summary>
	/// <param name="alias">The alias.</param>
	/// <returns>The type shape.</returns>
	/// <exception cref="KeyNotFoundException">Thrown if the alias is not recognized.</exception>
	protected ITypeShape Get(DerivedTypeIdentifier alias) => this.map[alias];
}
