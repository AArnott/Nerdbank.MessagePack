// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Converters;

#pragma warning disable SA1205 // Partial elements should declare access

/// <summary>
/// Enumerates the possible outcomes of a read operation.
/// </summary>
public enum DecodeResult
{
	/// <summary>
	/// The token was successfully read from the buffer.
	/// </summary>
	Success,

	/// <summary>
	/// The token read from the buffer did not match the expected token.
	/// </summary>
	TokenMismatch,

	/// <summary>
	/// The buffer is empty and no token could be read.
	/// </summary>
	EmptyBuffer,

	/// <summary>
	/// The token is of the expected type, but the buffer does not include all the bytes needed to read the value.
	/// </summary>
	InsufficientBuffer,
}
