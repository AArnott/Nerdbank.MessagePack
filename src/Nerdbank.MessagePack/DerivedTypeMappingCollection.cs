// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable collection of <see cref="DerivedShapeMapping{TBase}"/> objects.
/// </summary>
/// <remarks>
/// Since the <see cref="DerivedTypeMapping"/> object is mutable and this collection must be immutable,
/// we freeze the result of each mapping as it is added, and that is what is used during serialization.
/// If the original <see cref="DerivedTypeMapping"/> object mutates later, serialization will be unaffected.
/// </remarks>
[CollectionBuilder(typeof(DerivedTypeMappingCollection), nameof(Create))]
public class DerivedTypeMappingCollection : IReadOnlyCollection<DerivedTypeMapping>
{
	private readonly ImmutableArray<DerivedTypeMapping> derivedTypeMappings = [];

	private DerivedTypeMappingCollection(FrozenDictionary<Type, FrozenDictionary<DerivedTypeIdentifier, ITypeShape>> map, ImmutableArray<DerivedTypeMapping> mappings)
	{
		this.Map = map;
		this.derivedTypeMappings = mappings;
	}

	/// <inheritdoc/>
	public int Count => this.Map.Count;

	private FrozenDictionary<Type, FrozenDictionary<DerivedTypeIdentifier, ITypeShape>> Map { get; }

	/// <summary>
	/// Constructs a new <see cref="DerivedTypeMappingCollection"/> from a collection of <see cref="DerivedShapeMapping{TBase}"/> objects.
	/// </summary>
	/// <param name="derivedTypeMappings">The mappings to fill the collection with.</param>
	/// <returns>A new instance of <see cref="DerivedTypeMappingCollection"/>.</returns>
	public static DerivedTypeMappingCollection Create(ReadOnlySpan<DerivedTypeMapping> derivedTypeMappings)
	{
		Dictionary<Type, FrozenDictionary<DerivedTypeIdentifier, ITypeShape>> map = [];

		foreach (DerivedTypeMapping mapping in derivedTypeMappings)
		{
			map.Add(mapping.BaseType, mapping.CreateDerivedTypesMapping());
		}

		return new(map.ToFrozenDictionary(), derivedTypeMappings.ToImmutableArray());
	}

	/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
	public ImmutableArray<DerivedTypeMapping>.Enumerator GetEnumerator() => this.derivedTypeMappings.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator<DerivedTypeMapping> IEnumerable<DerivedTypeMapping>.GetEnumerator() => ((IReadOnlyList<DerivedTypeMapping>)this.derivedTypeMappings).GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<DerivedTypeMapping>)this).GetEnumerator();

	/// <summary>
	/// Tries to retrieve a derived type mapping for a given base type.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="derivedTypes">Receives the mapping, if available.</param>
	/// <returns>A value indicating whether a mapping was found.</returns>
	internal bool TryGetDerivedTypeMapping(Type baseType, [NotNullWhen(true)] out FrozenDictionary<DerivedTypeIdentifier, ITypeShape>? derivedTypes) => this.Map.TryGetValue(baseType, out derivedTypes);
}
