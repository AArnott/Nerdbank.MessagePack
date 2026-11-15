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
	private readonly string[] evidenceNames;
	private readonly byte[][] evidenceUtf8;
	private readonly ulong[] caseMasks;
	private readonly int wordCount;

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
		(this.evidenceNames, this.evidenceUtf8, this.caseMasks, this.wordCount) = BuildEvidence(cases);
	}

	/// <summary>
	/// Gets the number of <see cref="ulong"/> words in a presence mask.
	/// </summary>
	internal int WordCount => this.wordCount;

	/// <summary>
	/// Determines whether <paramref name="candidate"/> contains every name in <paramref name="smaller"/> and at least one more.
	/// </summary>
	/// <param name="candidate">The sorted names that may be a superset.</param>
	/// <param name="smaller">The sorted names that would be the subset.</param>
	/// <returns><see langword="true"/> when <paramref name="candidate"/> is a strict superset.</returns>
	internal static bool IsStrictSuperset(string[] candidate, string[] smaller)
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
	/// Selects the case whose required names are satisfied by <paramref name="present"/>.
	/// </summary>
	/// <param name="present">The property names present on the payload. Comparison is ordinal.</param>
	/// <returns>The index of the selected case.</returns>
	/// <exception cref="MessagePackSerializationException">
	/// Thrown with <see cref="MessagePackSerializationException.ErrorCode.NoMatchingUnionCase"/> when no case matches,
	/// or with <see cref="MessagePackSerializationException.ErrorCode.AmbiguousUnionCase"/> when several matches have no unique most-specific case.
	/// </exception>
	internal int Select(HashSet<string> present)
	{
		Span<ulong> mask = this.wordCount <= 4 ? stackalloc ulong[4] : new ulong[this.wordCount];
		mask = mask[..this.wordCount];
		mask.Clear();
		for (int i = 0; i < this.evidenceNames.Length; i++)
		{
			if (present.Contains(this.evidenceNames[i]))
			{
				mask[i / 64] |= 1UL << (i % 64);
			}
		}

		return this.Select(mask);
	}

	/// <summary>
	/// Finds the evidence bit for a UTF-8 property name.
	/// </summary>
	/// <param name="utf8Name">The property name, without a MessagePack string header.</param>
	/// <returns>The evidence index, or -1 when the name is not required by any case.</returns>
	internal int MatchEvidence(ReadOnlySpan<byte> utf8Name)
	{
		for (int i = 0; i < this.evidenceUtf8.Length; i++)
		{
			if (utf8Name.SequenceEqual(this.evidenceUtf8[i]))
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Selects the case whose required names are satisfied by <paramref name="present"/>.
	/// </summary>
	/// <param name="present">One bit per evidence name. Comparison is ordinal.</param>
	/// <returns>The index of the selected case.</returns>
	/// <exception cref="MessagePackSerializationException">
	/// Thrown with <see cref="MessagePackSerializationException.ErrorCode.NoMatchingUnionCase"/> when no case matches,
	/// or with <see cref="MessagePackSerializationException.ErrorCode.AmbiguousUnionCase"/> when several matches have no unique most-specific case.
	/// </exception>
	internal int Select(ReadOnlySpan<ulong> present)
	{
		int matchCount = 0;
		Span<int> matches = this.cases.Length <= 16 ? stackalloc int[16] : new int[this.cases.Length];
		matches = matches[..this.cases.Length];
		for (int i = 0; i < this.cases.Length; i++)
		{
			if (i == this.catchAllIndex || !this.Covers(present, i))
			{
				continue;
			}

			matches[matchCount++] = i;
		}

		if (matchCount == 0)
		{
			if (this.catchAllIndex >= 0)
			{
				return this.catchAllIndex;
			}

			throw new MessagePackSerializationException($"The payload does not match a duck-typing case of '{this.unionName}'.")
			{
				Code = MessagePackSerializationException.ErrorCode.NoMatchingUnionCase,
			};
		}

		if (matchCount == 1)
		{
			return matches[0];
		}

		int undominatedCount = 0;
		Span<int> undominated = matchCount <= 16 ? stackalloc int[16] : new int[matchCount];
		undominated = undominated[..matchCount];
		for (int i = 0; i < matchCount; i++)
		{
			bool dominated = false;
			for (int j = 0; j < matchCount; j++)
			{
				if (i != j && this.IsStrictSuperset(matches[j], matches[i]))
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

		throw new MessagePackSerializationException($"The payload matches multiple duck-typing cases of '{this.unionName}' and none is more specific: {FormatTypes(this.cases, undominated[..undominatedCount])}.")
		{
			Code = MessagePackSerializationException.ErrorCode.AmbiguousUnionCase,
		};
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
				$"Duck typing for '{unionType.FullName}' has more than one case with no required properties: {FormatTypes(cases, catchAlls.AsSpan(0, catchAllCount))}. At most one catch-all is allowed.");
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
	/// Assigns one bit to each distinct required name and records which bits each case needs.
	/// </summary>
	/// <param name="cases">The cases. Required names are already sorted and distinct within a case.</param>
	/// <returns>The evidence names, their UTF-8 bytes, the per-case masks, and the mask width in words.</returns>
	private static (string[] Names, byte[][] Utf8, ulong[] Masks, int WordCount) BuildEvidence(Case[] cases)
	{
		Dictionary<string, int> index = new(StringComparer.Ordinal);
		for (int i = 0; i < cases.Length; i++)
		{
			string[] names = cases[i].RequiredNames;
			for (int j = 0; j < names.Length; j++)
			{
				if (!index.ContainsKey(names[j]))
				{
					index.Add(names[j], index.Count);
				}
			}
		}

		string[] evidenceNames = new string[index.Count];
		foreach (KeyValuePair<string, int> pair in index)
		{
			evidenceNames[pair.Value] = pair.Key;
		}

		byte[][] evidenceUtf8 = new byte[evidenceNames.Length][];
		for (int i = 0; i < evidenceNames.Length; i++)
		{
			evidenceUtf8[i] = StringEncoding.UTF8.GetBytes(evidenceNames[i]);
		}

		int wordCount = evidenceNames.Length == 0 ? 0 : ((evidenceNames.Length - 1) / 64) + 1;
		ulong[] masks = new ulong[cases.Length * Math.Max(wordCount, 1)];
		if (wordCount > 0)
		{
			for (int i = 0; i < cases.Length; i++)
			{
				string[] names = cases[i].RequiredNames;
				for (int j = 0; j < names.Length; j++)
				{
					int bit = index[names[j]];
					masks[(i * wordCount) + (bit / 64)] |= 1UL << (bit % 64);
				}
			}
		}

		return (evidenceNames, evidenceUtf8, masks, wordCount);
	}

	/// <summary>
	/// Formats case type names for an error message.
	/// </summary>
	/// <param name="cases">The cases.</param>
	/// <param name="indexes">Indexes into <paramref name="cases"/>.</param>
	/// <returns>A comma-separated list of quoted type names.</returns>
	private static string FormatTypes(Case[] cases, ReadOnlySpan<int> indexes)
	{
		StringBuilder builder = new();
		for (int i = 0; i < indexes.Length; i++)
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
	/// Determines whether the present names include every name required by a case.
	/// </summary>
	/// <param name="present">The presence mask.</param>
	/// <param name="caseIndex">The case index.</param>
	/// <returns><see langword="true"/> when every required bit is set.</returns>
	private bool Covers(ReadOnlySpan<ulong> present, int caseIndex)
	{
		int offset = caseIndex * this.wordCount;
		for (int word = 0; word < this.wordCount; word++)
		{
			ulong required = this.caseMasks[offset + word];
			if ((present[word] & required) != required)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Determines whether one matching case requires every name of another and at least one more.
	/// </summary>
	/// <param name="candidate">The case that may be more specific.</param>
	/// <param name="other">The case that would be less specific.</param>
	/// <returns><see langword="true"/> when <paramref name="candidate"/> is a strict superset.</returns>
	private bool IsStrictSuperset(int candidate, int other)
	{
		bool extra = false;
		int candidateOffset = candidate * this.wordCount;
		int otherOffset = other * this.wordCount;
		for (int word = 0; word < this.wordCount; word++)
		{
			ulong left = this.caseMasks[candidateOffset + word];
			ulong right = this.caseMasks[otherOffset + word];
			if ((left & right) != right)
			{
				return false;
			}

			if (left != right)
			{
				extra = true;
			}
		}

		return extra;
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
