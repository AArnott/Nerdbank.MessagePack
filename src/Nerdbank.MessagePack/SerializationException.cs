// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer;

/// <summary>
/// Represents errors that occur during MessagePack serialization.
/// </summary>
public class SerializationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class.
	/// </summary>
	public SerializationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public SerializationException(string? message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
	public SerializationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Throws an exception explaining that nil was unexpectedly encountered while deserializing a value type.
	/// </summary>
	/// <typeparam name="T">The value type that was being deserialized.</typeparam>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	internal static SerializationException ThrowUnexpectedNilWhileDeserializing<T>() => throw new SerializationException("Unexpected nil encountered while deserializing " + typeof(T).FullName);
}
