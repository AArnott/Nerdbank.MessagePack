// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.PolySerializer;

/// <summary>
/// Extension methods for the <see cref="DecodeResult"/> enum.
/// </summary>
public static class DecodeResultExtensions
{
	/// <summary>
	/// Processes a given <see cref="DecodeResult"/> value and returns a boolean indicating whether more bytes are needed.
	/// </summary>
	/// <param name="result">The <see cref="DecodeResult"/> value.</param>
	/// <returns>
	/// <see langword="true"/> if the value is <see cref="DecodeResult.InsufficientBuffer"/>;
	/// <see langword="false"/> if the value is <see cref="DecodeResult.Success"/>.
	/// </returns>
	/// <exception cref="EndOfStreamException">Thrown if the value is <see cref="DecodeResult.EmptyBuffer"/>.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown if the value is <see cref="DecodeResult.TokenMismatch"/>.</exception>
	public static bool NeedsMoreBytes(this DecodeResult result)
	{
		return result switch
		{
			DecodeResult.Success => false,
			DecodeResult.InsufficientBuffer => true,
			DecodeResult.EmptyBuffer => throw new EndOfStreamException(),
			DecodeResult.TokenMismatch => throw new MessagePackSerializationException("Unexpected token encountered."), // TODO: Include the unexpected token into the exception message by packing MessagePackType into the DecodeResult.
			_ => throw new UnreachableException(),
		};
	}
}
