// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A mapping of C# union payload types to custom identifiers.
/// </summary>
/// <remarks>
/// No type shape provider is needed. The union shape supplies the payload shapes and conversions.
/// Types not registered in this mapping retain their normal identifiers.
/// Adding a mapping to a collection freezes it, preventing further mutation.
/// </remarks>
public sealed class UnionCaseMapping : IReadOnlyCollection<KeyValuePair<Type, DerivedTypeIdentifier>>
{
	private readonly Dictionary<Type, DerivedTypeIdentifier> aliases = [];
	private readonly HashSet<DerivedTypeIdentifier> identifiers = [];
	private bool isFrozen;

	/// <summary>
	/// Initializes a new instance of the <see cref="UnionCaseMapping"/> class.
	/// </summary>
	/// <param name="unionType">The containing C# union type.</param>
	public UnionCaseMapping(Type unionType)
	{
		this.UnionType = Requires.NotNull(unionType);
	}

	/// <summary>
	/// Gets the containing union type.
	/// </summary>
	public Type UnionType { get; }

	/// <inheritdoc/>
	public int Count => this.aliases.Count;

	/// <summary>
	/// Adds a payload type and its identifier.
	/// </summary>
	/// <param name="alias">The identifier for the payload type.</param>
	/// <param name="caseType">The payload type. Membership in the union is checked when its converter is constructed.</param>
	/// <exception cref="ArgumentException">The identifier is uninitialized, or the type or identifier is already registered.</exception>
	/// <exception cref="InvalidOperationException">The mapping has been frozen.</exception>
	public void Add(DerivedTypeIdentifier alias, Type caseType)
	{
		Verify.Operation(!this.isFrozen, "This instance has been frozen and may not be changed further.");
		Requires.NotNull(caseType);
		Requires.Argument(alias.Type != DerivedTypeIdentifier.AliasType.None, nameof(alias), "The identifier must be initialized.");
		Requires.Argument(!this.aliases.ContainsKey(caseType), nameof(caseType), "The case type is already registered.");
		Requires.Argument(!this.identifiers.Contains(alias), nameof(alias), "The identifier is already registered.");
		this.aliases.Add(caseType, alias);
		this.identifiers.Add(alias);
	}

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<Type, DerivedTypeIdentifier>> GetEnumerator() => this.aliases.GetEnumerator();

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	/// <summary>
	/// Prevents further changes to this mapping.
	/// </summary>
	internal void Freeze() => this.isFrozen = true;

	/// <summary>
	/// Gets the identifier for a payload type, if registered.
	/// </summary>
	/// <param name="caseType">The payload type.</param>
	/// <returns>The registered identifier, or <see langword="null"/>.</returns>
	internal DerivedTypeIdentifier? GetAlias(Type caseType) => this.aliases.TryGetValue(caseType, out DerivedTypeIdentifier alias) ? alias : (DerivedTypeIdentifier?)null;
}
