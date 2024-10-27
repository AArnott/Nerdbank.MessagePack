// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Utilities;

/// <summary>Defines a span-based equality comparer.</summary>
internal interface ISpanEqualityComparer<T>
{
	/// <summary>Gets the hash code for the specified buffer.</summary>
	int GetHashCode(ReadOnlySpan<T> buffer);
	/// <summary>Checks the two buffers for equality.</summary>
	bool Equals(ReadOnlySpan<T> x, ReadOnlySpan<T> y);
}

/// <summary>Defines an equality comparer for byte spans.</summary>
internal static class ByteSpanEqualityComparer
{
	/// <summary>Gets the default ordinal equality comparer for byte spans.</summary>
	internal static ISpanEqualityComparer<byte> Ordinal { get; } = new OrdinalEqualityComparer();

	private sealed class OrdinalEqualityComparer : ISpanEqualityComparer<byte>
	{
		public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
			=> x.SequenceEqual(y);

		public int GetHashCode(ReadOnlySpan<byte> buffer)
		{
			var hc = new HashCode();
			hc.AddBytes(buffer);
			return hc.ToHashCode();
		}
	}
}

/// <summary>Defines an equality comparer for char spans.</summary>
internal static class CharSpanEqualityComparer
{
	/// <summary>Gets the default ordinal equality comparer for char spans.</summary>
	internal static ISpanEqualityComparer<char> Ordinal { get; } = new StringComparisonEqualityComparer(StringComparison.Ordinal);
	/// <summary>Gets the default case insensitive ordinal equality comparer for char spans.</summary>
	internal static ISpanEqualityComparer<char> OrdinalIgnoreCase { get; } = new StringComparisonEqualityComparer(StringComparison.OrdinalIgnoreCase);

	private sealed class StringComparisonEqualityComparer(StringComparison comparison) : ISpanEqualityComparer<char>
	{
		public int GetHashCode(ReadOnlySpan<char> buffer)
			=> string.GetHashCode(buffer, comparison);

		public bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
			=> x.Equals(y, comparison);
	}
}
