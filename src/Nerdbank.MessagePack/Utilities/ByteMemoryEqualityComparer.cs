// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Utilities;

/// <summary>Defines an equality comparer for byte spans.</summary>
internal class ByteMemoryEqualityComparer : IEqualityComparer<ReadOnlyMemory<byte>>
{
	/// <summary>Gets the default ordinal equality comparer for byte spans.</summary>
	internal static ByteMemoryEqualityComparer Ordinal { get; } = new ByteMemoryEqualityComparer();

	/// <inheritdoc/>
	public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
		=> x.Span.SequenceEqual(y.Span);

	/// <inheritdoc/>
	public int GetHashCode(ReadOnlyMemory<byte> buffer)
	{
		var hc = default(HashCode);
#if NET
		hc.AddBytes(buffer.Span);
#else
		for (int i = 0; i < buffer.Length; i++)
		{
			hc.Add(buffer.Span[i]);
		}
#endif
		return hc.ToHashCode();
	}
}
