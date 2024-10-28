// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MessagePack;

/// <summary>
/// Context that flows through the serialization process.
/// </summary>
/// <param name="maxDepth">The maximum depth of the object graph to serialize or deserialize.</param>
[DebuggerDisplay($"Depth remaining = {{{nameof(depthRemaining)}}}")]
public struct SerializationContext(int maxDepth)
{
	/// <summary>
	/// The remaining depth of the object graph to serialize or deserialize.
	/// </summary>
	private int depthRemaining = maxDepth;

	/// <summary>
	/// Decrements the depth remaining.
	/// </summary>
	/// <remarks>
	/// Converters that (de)serialize nested objects should invoke this once <em>before</em> passing the context to nested (de)serializers.
	/// </remarks>
	/// <exception cref="MessagePackSerializationException">Thrown if the depth limit has been exceeded.</exception>
	public void DepthStep()
	{
		if (--this.depthRemaining < 0)
		{
			throw new MessagePackSerializationException("Exceeded maximum depth of object graph.");
		}
	}
}
