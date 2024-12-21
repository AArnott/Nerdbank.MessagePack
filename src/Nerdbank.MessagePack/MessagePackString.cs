// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A .NET string together with its msgpack encoding parts, to optimize serialization of well-known, often seen strings
/// such as property names used in a msgpack map object.
/// </summary>
/// <remarks>
/// This class is <em>not</em> a substitute for string interning.
/// Activate string interning via the <see cref="MessagePackSerializer.InternStrings"/> property.
/// This class avoids the need to encode/decode strings, which is even more efficient than interning strings,
/// but it is meant only for a finite sized set of well-known strings.
/// </remarks>
[DebuggerDisplay("{" + nameof(Value) + ",nq}")]
public class MessagePackString : IEquatable<MessagePackString>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackString"/> class.
	/// </summary>
	/// <param name="value">The string to pre-encode for msgpack serialization.</param>
	public MessagePackString(string value)
	{
		Requires.NotNull(value);

		this.Value = value;
		StringEncoding.GetEncodedStringBytes(value, out ReadOnlyMemory<byte> utf8, out ReadOnlyMemory<byte> msgpack);
		this.Utf8 = utf8;
		this.MsgPack = msgpack;
	}

	/// <summary>
	/// Gets the string value behind this instance.
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// Gets the UTF-8 encoded bytes of the string.
	/// </summary>
	public ReadOnlyMemory<byte> Utf8 { get; }

	/// <summary>
	/// Gets the msgpack encoded bytes of the string.
	/// </summary>
	/// <value>
	/// The msgpack encoded bytes are the UTF-8 encoded bytes of the string, prefixed with the msgpack encoding of the string length.
	/// </value>
	/// <remarks>
	/// The value of this property is suitable for providing to the <see cref="MessagePackWriter.WriteRaw(ReadOnlySpan{byte})"/> method.
	/// </remarks>
	public ReadOnlyMemory<byte> MsgPack { get; }

	/// <summary>
	/// Checks whether a given UTF-8 encoded string matches the string in <see cref="Value"/>.
	/// </summary>
	/// <param name="utf8String">The UTF-8 encoded string to test against.</param>
	/// <returns><see langword="true" /> if the strings match; otherwise <see langword="false" />.</returns>
	/// <remarks>
	/// This method is allocation free.
	/// </remarks>
	public bool IsMatch(ReadOnlySpan<byte> utf8String)
	{
		if (this.Utf8.Length != utf8String.Length)
		{
			return false;
		}

		return this.Utf8.Span.SequenceEqual(utf8String);
	}

	/// <inheritdoc cref="IsMatch(ReadOnlySpan{byte})"/>
	public bool IsMatch(ReadOnlySequence<byte> utf8String)
	{
		if (utf8String.IsSingleSegment)
		{
			return this.IsMatch(utf8String.First.Span);
		}

		// Avoid calling ReadOnlySequence<byte>.Length because that can be expensive,
		// and it involves enumerating each segment anyway, which we're already going to do.
		ReadOnlySpan<byte> remainingUtf8 = this.Utf8.Span;
		foreach (ReadOnlyMemory<byte> segment in utf8String)
		{
			if (remainingUtf8.Length < segment.Length)
			{
				return false;
			}

			if (!segment.Span.SequenceEqual(remainingUtf8[..segment.Length]))
			{
				return false;
			}

			remainingUtf8 = remainingUtf8[segment.Length..];
		}

		return remainingUtf8.IsEmpty;
	}

	/// <summary>
	/// Checks the string at the current reader position for equality with this string.
	/// </summary>
	/// <param name="reader">The reader. The reader's position will be advanced if and only if the next msgpack token is a matching string.</param>
	/// <returns><see langword="true" /> if the string matched; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// <para>No exception is thrown if the reader is positioned at a non-string token.</para>
	/// <para>This method never allocates memory nor encodes/decodes strings.</para>
	/// </remarks>
	/// <exception cref="EndOfStreamException">Thrown if the reader has no more tokens, or the buffer contains an incomplete string token.</exception>
	public bool TryRead(ref MessagePackReader reader)
	{
		switch (reader.NextMessagePackType)
		{
			case MessagePackType.Nil:
				return false;
			case MessagePackType.String:
				MessagePackReader peekReader = reader.CreatePeekReader();
				bool success = peekReader.TryReadStringSpan(out ReadOnlySpan<byte> span)
					? this.IsMatch(span)
					: this.IsMatch(peekReader.ReadStringSequence()!.Value);
				if (success)
				{
					reader = peekReader;
				}

				return success;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public bool Equals(MessagePackString? other) => this.Value == other?.Value;

	/// <inheritdoc />
	public override bool Equals(object? obj) => this.Equals(obj as MessagePackString);

	/// <inheritdoc />
	public override int GetHashCode() => this.Value.GetHashCode();
}
