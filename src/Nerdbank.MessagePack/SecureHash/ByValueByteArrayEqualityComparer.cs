// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// A full content comparison and hash of a byte buffer.
/// </summary>
internal class ByValueByteArrayEqualityComparer : IEqualityComparer<byte[]>
{
	/// <summary>
	/// The singleton to use.
	/// </summary>
	internal static readonly ByValueByteArrayEqualityComparer Default = new();

	private ByValueByteArrayEqualityComparer()
	{
	}

	/// <inheritdoc/>
	public bool Equals(byte[]? x, byte[]? y) => ReferenceEquals(x, y) || (x is null || y is null) ? false : x.SequenceEqual(y);

	/// <inheritdoc/>
	public int GetHashCode([DisallowNull] byte[] obj)
	{
		HashCode hashCode = default;
		hashCode.AddBytes(obj);
		return hashCode.ToHashCode();
	}
}
