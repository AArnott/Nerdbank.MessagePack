// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Utilities for array converters.
/// </summary>
internal static class ArrayConverterUtilities
{
	/// <summary>
	/// Reads the array headers necessary to determine the length of each dimension for an array.
	/// </summary>
	/// <param name="reader">The reader. This is <em>not</em> a <see langword="ref" /> so as to not impact the caller's read position.</param>
	/// <param name="dimensions">The dimensional array to initialize.</param>
#pragma warning disable NBMsgPack050 // use "ref MessagePackReader" for parameter type
	internal static void PeekNestedDimensionsLength(MessagePackReader reader, Span<int> dimensions)
#pragma warning restore NBMsgPack050 // use "ref MessagePackReader" for parameter type
	{
		for (int i = 0; i < dimensions.Length; i++)
		{
			dimensions[i] = reader.ReadArrayHeader();
		}
	}

	/// <summary>
	/// Reads an array header and verifies that its length matches the expected length.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="expected">The expected length.</param>
	/// <returns>The <paramref name="expected"/> value.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if the array is not of the <paramref name="expected"/> length.</exception>
	internal static int ReadArrayHeader(ref MessagePackReader reader, int expected)
	{
		int actual = reader.ReadArrayHeader();
		if (expected != actual)
		{
			throw new MessagePackSerializationException($"Expected array length of {expected} but was {actual}.");
		}

		return actual;
	}
}
