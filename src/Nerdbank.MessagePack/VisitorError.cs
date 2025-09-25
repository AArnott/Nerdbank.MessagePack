// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nerdbank.MessagePack;

/// <summary>
/// Represents an error encountered while constructing or traversing a converter graph.
/// This type forms a linked list of contextual messages and/or an original exception
/// that caused the failure. It can produce a combined textual path description or
/// throw a <see cref="MessagePackSerializationException"/> that wraps the original exception.
/// </summary>
internal class VisitorError
{
	/// <summary>
	/// The inner (previous) error in the chain, if any.
	/// </summary>
	private VisitorError? inner;

	/// <summary>
	/// The original exception associated with this error node, if any.
	/// </summary>
	private Exception? exception;

	/// <summary>
	/// A contextual message describing this error node, if any.
	/// </summary>
	private string? message;

	/// <summary>
	/// Initializes a new instance of the <see cref="VisitorError"/> class from an exception.
	/// </summary>
	/// <param name="exception">The exception that caused the error.</param>
	/// <param name="inner">An optional inner <see cref="VisitorError"/> representing additional context.</param>
	internal VisitorError(Exception exception, VisitorError? inner = null) => (this.exception, this.inner) = (exception, inner);

	/// <summary>
	/// Initializes a new instance of the <see cref="VisitorError"/> class from a message.
	/// </summary>
	/// <param name="message">A contextual message describing the error.</param>
	/// <param name="inner">An optional inner <see cref="VisitorError"/> representing additional context.</param>
	internal VisitorError(string message, VisitorError? inner = null) => (this.message, this.inner) = (message, inner);

	/// <summary>
	/// Implicitly converts an <see cref="Exception"/> to a <see cref="VisitorError"/>.
	/// </summary>
	/// <param name="exception">The exception to wrap.</param>
	/// <returns>A new <see cref="VisitorError"/> instance containing the provided exception.</returns>
	public static implicit operator VisitorError(Exception exception) => new(exception);

	/// <summary>
	/// Implicitly converts a textual message to a <see cref="VisitorError"/>.
	/// </summary>
	/// <param name="message">The message to wrap.</param>
	/// <returns>A new <see cref="VisitorError"/> instance containing the provided message.</returns>
	public static implicit operator VisitorError(string message) => new(message);

	/// <inheritdoc/>
	public override string ToString()
	{
		this.Prepare(out StringBuilder builder, out Exception? originalException);
		return builder.ToString();
	}

	/// <summary>
	/// Throws a <see cref="MessagePackSerializationException"/> describing the error chain.
	/// This method never returns.
	/// </summary>
	/// <exception cref="MessagePackSerializationException">
	/// Always thrown to report the prepared error message and, if available, the original exception.
	/// </exception>
	[DoesNotReturn]
	internal void ThrowException()
	{
		this.Prepare(out StringBuilder builder, out Exception? originalException);
		builder.Insert(0, "An error occurred while preparing the converter graph. ");
		throw new MessagePackSerializationException(builder.ToString(), originalException);
	}

	/// <summary>
	/// Prepares a textual description of the error path and extracts the original exception, if any.
	/// </summary>
	/// <param name="pathBuilder">
	/// When this method returns, contains a <see cref="StringBuilder"/> whose content is a human-readable
	/// description of the error path (prefixed with "Path: " if any path information exists).
	/// </param>
	/// <param name="originalException">
	/// When this method returns, contains the first non-null <see cref="Exception"/> found in the error chain,
	/// or <see langword="null"/> if none was present.
	/// </param>
	private void Prepare(out StringBuilder pathBuilder, out Exception? originalException)
	{
		pathBuilder = new();
		VisitorError? current = this;
		originalException = null;
		while (current is not null)
		{
			if (current.message is not null)
			{
				if (pathBuilder.Length > 0)
				{
					pathBuilder.Append(" -> ");
				}

				pathBuilder.Append(current.message);
			}

			originalException ??= current.exception;
			current = current.inner;
		}

		if (pathBuilder.Length > 0)
		{
			pathBuilder.Insert(0, "Path: ");
			pathBuilder.Append('.');
		}
	}
}
