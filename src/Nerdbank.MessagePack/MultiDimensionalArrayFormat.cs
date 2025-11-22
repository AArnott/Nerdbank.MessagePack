// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Enumerates the possible formats for serializing multi-dimensional arrays.
/// </summary>
/// <remarks>
/// The msgpack spec doesn't define how to encode multi-dimensional arrays,
/// so we offer a couple options.
/// </remarks>
public enum MultiDimensionalArrayFormat
{
	/// <summary>
	/// A format that nests arrays for each dimension.
	/// </summary>
	Nested,

	/// <summary>
	/// A high performance format that serializes all dimensions in a single array.
	/// </summary>
	Flat,
}
