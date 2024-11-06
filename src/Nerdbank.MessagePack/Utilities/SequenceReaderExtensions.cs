// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Extension methods for the <see cref="SequenceReader{T}"/> type.
/// </summary>
internal static class SequenceReaderExtensions
{
	/// <summary>
	/// Advances the reader by the given bytes, provided there are at least that many bytes remaining.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="count">The number of bytes to advance.</param>
	/// <returns><see langword="true" /> if the reader had enough bytes and was advaned; <see langword="false" /> otherwise.</returns>
	internal static bool TryAdvance(ref this SequenceReader<byte> reader, long count)
	{
		if (reader.Remaining >= count)
		{
			reader.Advance(count);
			return true;
		}

		return false;
	}
}
