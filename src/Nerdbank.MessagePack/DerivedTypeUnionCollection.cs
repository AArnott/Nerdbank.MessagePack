// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable collection of <see cref="DerivedTypeUnion"/> objects.
/// </summary>
/// <remarks>
/// Since the <see cref="DerivedTypeUnion"/> object is mutable and this collection must be immutable,
/// we freeze the result of each union as it is added, and that is what is used during serialization.
/// If the original <see cref="DerivedTypeUnion"/> object mutates later, serialization will be unaffected.
/// </remarks>
[CollectionBuilder(typeof(DerivedTypeUnionCollection), nameof(Create))]
public class DerivedTypeUnionCollection : IReadOnlyCollection<DerivedTypeUnion>
{
	private readonly ImmutableArray<DerivedTypeUnion> derivedTypeUnions = [];
	private readonly FrozenDictionary<Type, DerivedTypeUnion> map;

	private DerivedTypeUnionCollection(FrozenDictionary<Type, DerivedTypeUnion> map, ImmutableArray<DerivedTypeUnion> derivedTypeUnions)
	{
		this.map = map;
		this.derivedTypeUnions = derivedTypeUnions;
	}

	/// <inheritdoc/>
	public int Count => this.derivedTypeUnions.Length;

	/// <summary>
	/// Constructs a new <see cref="DerivedTypeUnionCollection"/> from a collection of <see cref="DerivedShapeMapping{TBase}"/> objects.
	/// </summary>
	/// <param name="derivedTypeUnions">The unions to fill the collection with. These objects become frozen when they are added to this collection such that any attempt to mutate them later will throw an exception.</param>
	/// <returns>A new instance of <see cref="DerivedTypeUnionCollection"/>.</returns>
	public static DerivedTypeUnionCollection Create(ReadOnlySpan<DerivedTypeUnion> derivedTypeUnions)
	{
		Dictionary<Type, DerivedTypeUnion> map = [];
		ImmutableArray<DerivedTypeUnion>.Builder arrayBuilder = ImmutableArray.CreateBuilder<DerivedTypeUnion>(derivedTypeUnions.Length);

		foreach (DerivedTypeUnion union in derivedTypeUnions)
		{
			union.Freeze();
			map.Add(union.BaseType, union);
			arrayBuilder.Add(union);
		}

		return new(map.ToFrozenDictionary(), arrayBuilder.MoveToImmutable());
	}

	/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
	public ImmutableArray<DerivedTypeUnion>.Enumerator GetEnumerator() => this.derivedTypeUnions.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator<DerivedTypeUnion> IEnumerable<DerivedTypeUnion>.GetEnumerator() => ((IReadOnlyList<DerivedTypeUnion>)this.derivedTypeUnions).GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<DerivedTypeUnion>)this).GetEnumerator();

	/// <summary>
	/// Tries to retrieve a derived type mapping for a given base type.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="union">Receives the union information, if available.</param>
	/// <returns>A value indicating whether a mapping was found.</returns>
	internal bool TryGetDerivedTypeUnion(Type baseType, [NotNullWhen(true)] out DerivedTypeUnion? union) => this.map.TryGetValue(baseType, out union);
}
