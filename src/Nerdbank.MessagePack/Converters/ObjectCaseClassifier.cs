// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Chooses one object case by the serialized names of its required members.
/// </summary>
/// <remarks>
/// A case matches when every required name is present. Unknown names do not reject a case.
/// When several cases match, the unique case whose required names are a strict superset of the others is chosen.
/// A case with no required names is the catch-all and is used only when nothing else matches.
/// </remarks>
internal sealed class ObjectCaseClassifier
{
	private readonly string unionName;
	private readonly Case[] cases;
	private readonly int catchAllIndex;

	/// <summary>
	/// Initializes a new instance of the <see cref="ObjectCaseClassifier"/> class.
	/// </summary>
	/// <param name="unionType">The union type, used in error messages.</param>
	/// <param name="cases">The cases, in declaration order. Required names must be sorted ordinally and contain no duplicates.</param>
	/// <param name="namingPolicyApplied"><see langword="true"/> when <paramref name="cases"/> uses serialized names; <see langword="false"/> when they are still shape names.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="namingPolicyApplied"/> is <see langword="false"/> and two cases cannot be distinguished.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown when <paramref name="namingPolicyApplied"/> is <see langword="true"/> and two cases cannot be distinguished.</exception>
	internal ObjectCaseClassifier(Type unionType, Case[] cases, bool namingPolicyApplied)
	{
		this.unionName = unionType.FullName ?? unionType.Name;
		this.cases = cases;
		this.catchAllIndex = ThrowIfIndistinguishable(unionType, cases, namingPolicyApplied);
	}

	/// <summary>
	/// Selects the case whose required names are satisfied by <paramref name="present"/>.
	/// </summary>
	/// <param name="present">The property names present on the payload. Comparison is ordinal.</param>
	/// <returns>The index of the selected case.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown when no case matches, or when several matches have no unique most-specific case.</exception>
	internal int Select(HashSet<string> present)
	{
		int matchCount = 0;
		int[] matches = new int[this.cases.Length];
		for (int i = 0; i < this.cases.Length; i++)
		{
			if (i == this.catchAllIndex)
			{
				continue;
			}

			if (ContainsAll(this.cases[i].RequiredNames, present))
			{
				matches[matchCount++] = i;
			}
		}

		if (matchCount == 0)
		{
			if (this.catchAllIndex >= 0)
			{
				return this.catchAllIndex;
			}

			throw new MessagePackSerializationException($"The payload does not match a duck-typing case of '{this.unionName}'.");
		}

		if (matchCount == 1)
		{
			return matches[0];
		}

		int undominatedCount = 0;
		int[] undominated = new int[matchCount];
		for (int i = 0; i < matchCount; i++)
		{
			bool dominated = false;
			string[] mine = this.cases[matches[i]].RequiredNames;
			for (int j = 0; j < matchCount; j++)
			{
				if (i != j && IsStrictSuperset(this.cases[matches[j]].RequiredNames, mine))
				{
					dominated = true;
					break;
				}
			}

			if (!dominated)
			{
				undominated[undominatedCount++] = matches[i];
			}
		}

		if (undominatedCount == 1)
		{
			return undominated[0];
		}

		throw new MessagePackSerializationException($"The payload matches multiple duck-typing cases of '{this.unionName}' and none is more specific: {FormatTypes(this.cases, undominated, undominatedCount)}.");
	}

	/// <summary>
	/// Rejects unions that have two catch-all cases or two cases with the same required names.
	/// </summary>
	/// <param name="unionType">The union type.</param>
	/// <param name="cases">The cases to validate.</param>
	/// <param name="namingPolicyApplied"><see langword="true"/> when names are serialized names.</param>
	/// <returns>The index of the catch-all case, or -1 when there is none.</returns>
	private static int ThrowIfIndistinguishable(Type unionType, Case[] cases, bool namingPolicyApplied)
	{
		Dictionary<string, int> signatures = new(StringComparer.Ordinal);
		int catchAllIndex = -1;
		int catchAllCount = 0;
		int[] catchAlls = new int[cases.Length];
		for (int i = 0; i < cases.Length; i++)
		{
			if (cases[i].RequiredNames.Length == 0)
			{
				catchAlls[catchAllCount++] = i;
				catchAllIndex = i;
				continue;
			}

			string signature = Signature(cases[i].RequiredNames);
			if (signatures.TryGetValue(signature, out int previous))
			{
				throw CreateCollisionException(
					namingPolicyApplied,
					$"Duck typing for '{unionType.FullName}' cannot tell '{cases[previous].Type.FullName}' from '{cases[i].Type.FullName}' because they require the same properties: {FormatNames(cases[i].RequiredNames)}.");
			}

			signatures.Add(signature, i);
		}

		if (catchAllCount > 1)
		{
			throw CreateCollisionException(
				namingPolicyApplied,
				$"Duck typing for '{unionType.FullName}' has more than one case with no required properties: {FormatTypes(cases, catchAlls, catchAllCount)}. At most one catch-all is allowed.");
		}

		return catchAllCount == 0 ? -1 : catchAllIndex;
	}

	/// <summary>
	/// Builds an injective signature for a sorted set of required names.
	/// </summary>
	/// <param name="names">The required names. A name may itself contain NUL.</param>
	/// <returns>A length-prefixed encoding that differs when the name sequences differ.</returns>
	private static string Signature(string[] names)
	{
		StringBuilder builder = new();
		for (int i = 0; i < names.Length; i++)
		{
			builder.Append(names[i].Length);
			builder.Append(':');
			builder.Append(names[i]);
			builder.Append('\0');
		}

		return builder.ToString();
	}

	/// <summary>
	/// Creates the construction-time failure for indistinguishable cases.
	/// </summary>
	/// <param name="namingPolicyApplied"><see langword="true"/> to throw <see cref="MessagePackSerializationException"/>.</param>
	/// <param name="message">The error message.</param>
	/// <returns>The exception to throw.</returns>
	private static Exception CreateCollisionException(bool namingPolicyApplied, string message)
		=> namingPolicyApplied ? new MessagePackSerializationException(message) : new ArgumentException(message);

	/// <summary>
	/// Determines whether every name in <paramref name="required"/> is in <paramref name="present"/>.
	/// </summary>
	/// <param name="required">The required names.</param>
	/// <param name="present">The names present on the payload.</param>
	/// <returns><see langword="true"/> when every required name is present.</returns>
	private static bool ContainsAll(string[] required, HashSet<string> present)
	{
		for (int i = 0; i < required.Length; i++)
		{
			if (!present.Contains(required[i]))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Determines whether <paramref name="candidate"/> contains every name in <paramref name="smaller"/> and at least one more.
	/// </summary>
	/// <param name="candidate">The sorted names that may be a superset.</param>
	/// <param name="smaller">The sorted names that would be the subset.</param>
	/// <returns><see langword="true"/> when <paramref name="candidate"/> is a strict superset.</returns>
	private static bool IsStrictSuperset(string[] candidate, string[] smaller)
	{
		if (candidate.Length <= smaller.Length)
		{
			return false;
		}

		int i = 0;
		for (int j = 0; j < smaller.Length; j++)
		{
			while (i < candidate.Length && string.CompareOrdinal(candidate[i], smaller[j]) < 0)
			{
				i++;
			}

			if (i == candidate.Length || !string.Equals(candidate[i], smaller[j], StringComparison.Ordinal))
			{
				return false;
			}

			i++;
		}

		return true;
	}

	/// <summary>
	/// Formats case type names for an error message.
	/// </summary>
	/// <param name="cases">The cases.</param>
	/// <param name="indexes">Indexes into <paramref name="cases"/>.</param>
	/// <param name="count">How many indexes to format.</param>
	/// <returns>A comma-separated list of quoted type names.</returns>
	private static string FormatTypes(Case[] cases, int[] indexes, int count)
	{
		StringBuilder builder = new();
		for (int i = 0; i < count; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append('\'');
			builder.Append(cases[indexes[i]].Type.FullName);
			builder.Append('\'');
		}

		return builder.ToString();
	}

	/// <summary>
	/// Formats property names for an error message.
	/// </summary>
	/// <param name="names">The property names.</param>
	/// <returns>A comma-separated list.</returns>
	private static string FormatNames(string[] names)
	{
		StringBuilder builder = new();
		for (int i = 0; i < names.Length; i++)
		{
			if (i > 0)
			{
				builder.Append(", ");
			}

			builder.Append(names[i]);
		}

		return builder.ToString();
	}

	/// <summary>
	/// One object case and the serialized names that must be present for it to match.
	/// </summary>
	/// <param name="type">The case type.</param>
	/// <param name="requiredNames">Required serialized names, sorted ordinally, with no duplicates.</param>
	internal readonly struct Case(Type type, string[] requiredNames)
	{
		/// <summary>
		/// Gets the case type.
		/// </summary>
		internal Type Type { get; } = type;

		/// <summary>
		/// Gets the required serialized names, sorted ordinally.
		/// </summary>
		internal string[] RequiredNames { get; } = requiredNames;
	}
}
