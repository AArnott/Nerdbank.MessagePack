// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
namespace Serde.IO;

internal interface IBufReader
{
	/// <summary>
	/// Span into the buffer.
	/// </summary>
	ReadOnlySpan<byte> Span { get; }

	/// <summary>
	/// Move the buffer forward by the given number of bytes. Invalid to advance past the end of the buffer.
	/// </summary>
	void Advance(int count);

	/// <summary>
	/// Fill the buffer with 1 <= fillCount <= n bytes. Returns false if the end of the stream is reached.
	/// </summary>
	bool FillBuffer(int fillCount);
}
