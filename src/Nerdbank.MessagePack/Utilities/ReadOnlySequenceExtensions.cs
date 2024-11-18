// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This is a copy of the Sequence<T> class from the Nerdbank.Streams library.
namespace Nerdbank.MessagePack.Utilities;

/// <summary>
/// Extension methods for the <see cref="ReadOnlySequence{T}"/> type.
/// </summary>
internal static class ReadOnlySequenceExtensions
{
	/// <summary>
	/// Copies the content of one <see cref="ReadOnlySequence{T}"/> to another that is backed by its own
	/// memory buffers.
	/// </summary>
	/// <typeparam name="T">The type of element in the sequence.</typeparam>
	/// <param name="template">The sequence to copy from.</param>
	/// <returns>A shallow copy of the sequence, backed by buffers which will never be recycled.</returns>
	/// <remarks>
	/// This method is useful for retaining data that is backed by buffers that will be reused later.
	/// </remarks>
	internal static ReadOnlySequence<T> Clone<T>(this ReadOnlySequence<T> template)
	{
		Sequence<T> sequence = new();
		sequence.Write(template);
		return sequence;
	}
}
