// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// An interface that may be optionally implemented by a <see cref="MessagePackConverter{T}"/> to support deserializing into an existing collection instance.
/// </summary>
/// <typeparam name="TCollectionType">The concrete collection type supported by the converter.</typeparam>
/// <remarks>
/// This enables the scenario of a user type that declares a read-only property of a mutable collection type,
/// such that the deserializer can only add elements to the existing collection rather than creating a new collection.
/// </remarks>
internal interface IDeserializeInto<TCollectionType>
{
	/// <summary>
	/// Deserializes a collection into an existing collection instance.
	/// </summary>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="collection">The collection instance to inflate with elements.</param>
	/// <param name="context">The deserialization context.</param>
	/// <remarks>
	/// Implementations may assume that the messagepack token is not <see cref="MessagePackCode.Nil"/>.
	/// </remarks>
	void DeserializeInto(ref MessagePackReader reader, ref TCollectionType collection, SerializationContext context);

	/// <summary>
	/// Deserializes a collection into an existing collection instance.
	/// </summary>
	/// <param name="reader">The reader to deserialize from.</param>
	/// <param name="collection">The collection instance to inflate with elements.</param>
	/// <param name="context">The deserialization context.</param>
	/// <returns>A task that tracks the async operation.</returns>
	/// <remarks>
	/// Implementations may assume that the messagepack token is not <see cref="MessagePackCode.Nil"/>.
	/// </remarks>
	[Experimental("NBMsgPackAsync")]
	ValueTask DeserializeIntoAsync(MessagePackAsyncReader reader, TCollectionType collection, SerializationContext context);
}
