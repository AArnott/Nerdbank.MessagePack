// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Represents errors that occur during MessagePack serialization.
/// </summary>
public class MessagePackSerializationException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
	/// </summary>
	public MessagePackSerializationException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public MessagePackSerializationException(string? message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
	public MessagePackSerializationException(string? message, Exception? innerException)
		: base(message, innerException)
	{
		if (innerException is MessagePackSerializationException inner)
		{
			this.Code = inner.Code;
		}
	}

	/// <summary>
	/// Specified causes for serialization or deserialization failure.
	/// </summary>
	public enum ErrorCode
	{
		/// <summary>
		/// No specific cause for the failure was set.
		/// </summary>
		Unspecified,

		/// <summary>
		/// Deserialization failed because the data was missing a value for a required property.
		/// </summary>
		MissingRequiredProperty,

		/// <summary>
		/// Deserialization failed because the data specified a <see langword="null"/> value for a non-nullable property.
		/// </summary>
		DisallowedNullValue,

		/// <summary>
		/// Deserialization failed because the data specified a <see langword="null"/> token at an unexpected or disallowed place.
		/// </summary>
		UnexpectedNull,

		/// <summary>
		/// Deserialization failed because the data specified multiple values for a single property.
		/// </summary>
		DoublePropertyAssignment,

		/// <summary>
		/// Deserialization failed because the data contained a token that was not expected at this time.
		/// </summary>
		UnexpectedToken,

		/// <summary>
		/// Deserialization failed because a <see cref="ExtensionHeader.TypeCode"/> was encountered
		/// that the deserializer was not expecting.
		/// </summary>
		UnexpectedExtensionTypeCode,
	}

	/// <summary>
	/// Gets the specific cause for a failure, if specified.
	/// </summary>
	public ErrorCode Code { get; init; }

	/// <summary>
	/// Throws an exception explaining that nil was unexpectedly encountered while deserializing a value type.
	/// </summary>
	/// <typeparam name="T">The value type that was being deserialized.</typeparam>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	internal static MessagePackSerializationException ThrowUnexpectedNilWhileDeserializing<T>() => throw new MessagePackSerializationException("Unexpected nil encountered while deserializing " + typeof(T).FullName) { Code = ErrorCode.UnexpectedNull };
}
