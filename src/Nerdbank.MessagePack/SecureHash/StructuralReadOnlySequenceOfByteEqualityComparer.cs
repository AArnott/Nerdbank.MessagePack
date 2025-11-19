// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A structural comparer for <see cref="ReadOnlySequence{T}"/> of <see cref="byte"/>.
/// </summary>
internal class StructuralReadOnlySequenceOfByteEqualityComparer : IEqualityComparer<ReadOnlySequence<byte>>
{
	/// <summary>
	/// The singleton to use.
	/// </summary>
	internal static readonly StructuralReadOnlySequenceOfByteEqualityComparer Default = new();

	private StructuralReadOnlySequenceOfByteEqualityComparer()
	{
	}

	/// <inheritdoc/>
	public bool Equals(ReadOnlySequence<byte> x, ReadOnlySequence<byte> y) => x.SequenceEqual(y);

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] ReadOnlySequence<byte> obj)
	{
		HashCode hashCode = default;
		foreach (ReadOnlyMemory<byte> segment in obj)
		{
			ReadOnlySpan<byte> span = segment.Span;
#if NET
			hashCode.AddBytes(span);
#else
			for (int i = 0; i < span.Length; i++)
			{
				hashCode.Add(span[i]);
			}
#endif
		}

		return hashCode.ToHashCode();
	}
}
