// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Describes a mapping between a base type and its known sub-types, along with the aliases that identify them.
/// </summary>
/// <typeparam name="TBase">The base type or interface that all sub-types derive from or implement.</typeparam>
public class DerivedTypeMapping<TBase> : IDerivedTypeMapping
{
	private readonly Dictionary<DerivedTypeIdentifier, ITypeShape> map = new();
	private readonly HashSet<Type> addedTypes = new();

	/// <summary>
	/// Adds a known sub-type to the mapping.
	/// </summary>
	/// <typeparam name="TDerived">The sub-type.</typeparam>
	/// <param name="alias">The alias for the sub-type.</param>
	/// <param name="typeShape">The shape of the sub-type.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="alias"/> or the <see cref="Type"/> described by <paramref name="typeShape"/> have already been added to this mapping.</exception>
	public void Add<TDerived>(int alias, ITypeShape<TDerived> typeShape)
		where TDerived : TBase
	{
		Requires.NotNull(typeShape);
		this.map.Add(alias, typeShape);
		if (!this.addedTypes.Add(typeof(TDerived)))
		{
			this.map.Remove(alias);
			throw new ArgumentException($"The type {typeof(TDerived)} has already been added to the mapping.", nameof(alias));
		}
	}

	/// <summary>
	/// Adds a known sub-type to the mapping.
	/// </summary>
	/// <typeparam name="TDerived">The sub-type.</typeparam>
	/// <param name="alias">The alias for the sub-type.</param>
	/// <param name="typeShape">The shape of the sub-type.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="alias"/> or the <see cref="Type"/> described by <paramref name="typeShape"/> have already been added to this mapping.</exception>
	public void Add<TDerived>(string alias, ITypeShape<TDerived> typeShape)
		where TDerived : TBase
	{
		Requires.NotNull(typeShape);
		this.map.Add(alias, typeShape);
		if (!this.addedTypes.Add(typeof(TDerived)))
		{
			this.map.Remove(alias);
			throw new ArgumentException($"The type {typeof(TDerived)} has already been added to the mapping.", nameof(alias));
		}
	}

	/// <inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/summary" />
	/// <inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/exception" />
	/// <param name="alias"><inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
	/// <param name="provider"><inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	public void Add<TDerived>(int alias, ITypeShapeProvider provider)
		where TDerived : TBase
	{
		Requires.NotNull(provider);

		ITypeShape<TDerived>? shape = (ITypeShape<TDerived>?)provider.GetShape(typeof(TDerived));
		Requires.Argument(shape is not null, nameof(provider), "The provider did not provide a shape for the given type.");
		this.Add(alias, shape);
	}

	/// <inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/summary" />
	/// <inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/exception" />
	/// <param name="alias"><inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
	/// <param name="provider"><inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	public void Add<TDerived>(string alias, ITypeShapeProvider provider)
		where TDerived : TBase
	{
		Requires.NotNull(provider);

		ITypeShape<TDerived>? shape = (ITypeShape<TDerived>?)provider.GetShape(typeof(TDerived));
		Requires.Argument(shape is not null, nameof(provider), "The provider did not provide a shape for the given type.");
		this.Add(alias, shape);
	}

#if NET
	/// <inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" />
	public void Add<TDerived>(int alias)
		where TDerived : TBase, IShapeable<TDerived> => this.Add(alias, TDerived.GetShape());

	/// <inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/summary" />
	/// <inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/exception" />
	/// <param name="alias"><inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
	/// <typeparam name="TDerived"><inheritdoc cref="Add{TDerived}(int, ITypeShape{TDerived})" path="/typeparam[@name='TDerived']"/></typeparam>
	/// <typeparam name="TProvider">The witness class that provides a type shape for <typeparamref name="TDerived"/>.</typeparam>
	public void Add<TDerived, TProvider>(int alias)
		where TDerived : TBase
		where TProvider : IShapeable<TDerived>
		=> this.Add(alias, TProvider.GetShape());

	/// <inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" />
	public void Add<TDerived>(string alias)
		where TDerived : TBase, IShapeable<TDerived> => this.Add(alias, TDerived.GetShape());

	/// <inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/summary" />
	/// <inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/exception" />
	/// <param name="alias"><inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/param[@name='alias']" /></param>
	/// <typeparam name="TDerived"><inheritdoc cref="Add{TDerived}(string, ITypeShape{TDerived})" path="/typeparam[@name='TDerived']"/></typeparam>
	/// <typeparam name="TProvider">The witness class that provides a type shape for <typeparamref name="TDerived"/>.</typeparam>
	public void Add<TDerived, TProvider>(string alias)
		where TDerived : TBase
		where TProvider : IShapeable<TDerived>
		=> this.Add(alias, TProvider.GetShape());
#endif

	/// <inheritdoc />
	FrozenDictionary<DerivedTypeIdentifier, ITypeShape> IDerivedTypeMapping.CreateDerivedTypesMapping() => this.map.ToFrozenDictionary();
}
