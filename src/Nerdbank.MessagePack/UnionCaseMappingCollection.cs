// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable collection of per-union identifier mappings that supplies C# union case identifiers.
/// </summary>
/// <remarks>
/// Each mapping is frozen when added to the collection, before the collection can be assigned to a serializer.
/// </remarks>
[CollectionBuilder(typeof(UnionCaseMappingCollection), nameof(Create))]
public sealed class UnionCaseMappingCollection : IUnionTypeAliasProvider, IReadOnlyCollection<UnionCaseMapping>
{
	private readonly ImmutableArray<UnionCaseMapping> mappings;
	private readonly FrozenDictionary<Type, UnionCaseMapping> mappingsByType;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnionCaseMappingCollection"/> class and freezes its mappings.
	/// </summary>
	/// <param name="mappings">The mappings. Each union type may occur only once.</param>
	/// <exception cref="ArgumentException">Multiple mappings target the same union type.</exception>
	public UnionCaseMappingCollection(ReadOnlySpan<UnionCaseMapping> mappings)
	{
		Dictionary<Type, UnionCaseMapping> map = [];
		ImmutableArray<UnionCaseMapping>.Builder builder = ImmutableArray.CreateBuilder<UnionCaseMapping>(mappings.Length);
		foreach (UnionCaseMapping mapping in mappings)
		{
			Requires.NotNull(mapping);
			map.Add(mapping.UnionType, mapping);
			mapping.Freeze();
			builder.Add(mapping);
		}

		this.mappings = builder.MoveToImmutable();
		this.mappingsByType = map.ToFrozenDictionary();
	}

	/// <inheritdoc/>
	public int Count => this.mappings.Length;

	/// <summary>
	/// Creates an immutable collection and freezes its mappings.
	/// </summary>
	/// <param name="mappings">The mappings to include.</param>
	/// <returns>The immutable collection.</returns>
	public static UnionCaseMappingCollection Create(ReadOnlySpan<UnionCaseMapping> mappings) => new(mappings);

	/// <inheritdoc/>
	public DerivedTypeIdentifier? GetAlias(IUnionTypeShape unionShape, IUnionCaseShape caseShape)
	{
		Requires.NotNull(unionShape);
		Requires.NotNull(caseShape);
		return this.mappingsByType.TryGetValue(unionShape.Type, out UnionCaseMapping? mapping)
			? mapping.GetAlias(caseShape.UnionCaseType.Type)
			: null;
	}

	/// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
	public ImmutableArray<UnionCaseMapping>.Enumerator GetEnumerator() => this.mappings.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator<UnionCaseMapping> IEnumerable<UnionCaseMapping>.GetEnumerator() => ((IEnumerable<UnionCaseMapping>)this.mappings).GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.mappings).GetEnumerator();

	/// <summary>
	/// Checks that all registered payload types belong to a union.
	/// </summary>
	/// <param name="unionShape">The union whose mapping should be checked.</param>
	/// <returns>An error if a registered payload type does not belong to the union; otherwise <see langword="null"/>.</returns>
	internal VisitorError? ValidateUnionCases(IUnionTypeShape unionShape)
	{
		if (this.mappingsByType.TryGetValue(unionShape.Type, out UnionCaseMapping? mapping))
		{
			foreach (KeyValuePair<Type, DerivedTypeIdentifier> entry in mapping)
			{
				if (!unionShape.UnionCases.Any(c => c.UnionCaseType.Type == entry.Key))
				{
					return new ArgumentException($"Type '{entry.Key}' is not a case of union '{unionShape.Type}'.");
				}
			}
		}

		return null;
	}
}
