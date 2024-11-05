// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MessagePack;

/// <summary>
/// Context that flows through the serialization process.
/// </summary>
[DebuggerDisplay($"Depth remaining = {{{nameof(MaxDepth)}}}")]
public struct SerializationContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationContext"/> struct.
	/// </summary>
	public SerializationContext()
	{
	}

	/// <summary>
	/// Gets or sets the remaining depth of the object graph to serialize or deserialize.
	/// </summary>
	/// <value>The default value is 64.</value>
	/// <remarks>
	/// Exceeding this depth will result in a <see cref="MessagePackSerializationException"/> being thrown
	/// from <see cref="DepthStep"/>.
	/// </remarks>
	public int MaxDepth { get; set; } = 64;

	/// <summary>
	/// Decrements the depth remaining.
	/// </summary>
	/// <remarks>
	/// Converters that (de)serialize nested objects should invoke this once <em>before</em> passing the context to nested (de)serializers.
	/// </remarks>
	/// <exception cref="MessagePackSerializationException">Thrown if the depth limit has been exceeded.</exception>
	public void DepthStep()
	{
		if (--this.MaxDepth < 0)
		{
			throw new MessagePackSerializationException("Exceeded maximum depth of object graph.");
		}
	}
}
