// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft;

namespace ShapeShift;

/// <summary>
/// A .NET string together with its pre-formatted bytes, to optimize serialization of well-known, often seen strings
/// such as property names used in a serialized map object.
/// </summary>
/// <remarks>
/// This class is <em>not</em> a substitute for string interning.
/// Activate string interning via the <see cref="SerializerBase.InternStrings"/> property.
/// This class avoids the need to encode/decode strings, which is even more efficient than interning strings,
/// but it is meant only for a finite sized set of well-known strings.
/// </remarks>
[DebuggerDisplay("{" + nameof(Value) + ",nq}")]
public class PreformattedString : IEquatable<PreformattedString>
{
	/// <summary>
	/// The maximum length of a string that may be allocated on the stack.
	/// </summary>
	internal const int ReasonablyLengthStackString = 1024;

	/// <summary>
	/// Initializes a new instance of the <see cref="PreformattedString"/> class.
	/// </summary>
	/// <param name="value">The string to pre-format for serialization.</param>
	/// <param name="formatter">The formatter to use to encode the string.</param>
	public PreformattedString(string value, Formatter formatter)
	{
		Requires.NotNull(value);
		Requires.NotNull(formatter);

		this.Formatter = formatter;
		this.Value = value;
		formatter.GetEncodedStringBytes(value, out ReadOnlyMemory<byte> encoded, out ReadOnlyMemory<byte> formatted);
		this.Encoded = encoded;
		this.Formatted = formatted;
	}

	/// <summary>
	/// Gets the string value behind this instance.
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// Gets the formatter used to pre-format the string.
	/// </summary>
	public Formatter Formatter { get; }

	/// <summary>
	/// Gets the encoded bytes of the string, without formatter-specific header and footer.
	/// </summary>
	public ReadOnlyMemory<byte> Encoded { get; }

	/// <summary>
	/// Gets the formatted bytes of the string, including any formatter-specific bytes (like length headers, quotation marks, etc.).
	/// </summary>
	/// <remarks>
	/// The value of this property is suitable for providing to the <see cref="BufferWriter.Write(ReadOnlySpan{byte})"/> method
	/// on <see cref="Writer.Buffer"/>.
	/// </remarks>
	public ReadOnlyMemory<byte> Formatted { get; }

	/// <summary>
	/// Checks whether a given encoded string matches the string in <see cref="Value"/>.
	/// </summary>
	/// <param name="encodedString">
	/// The string to test against.
	/// The string must be encoded with <see cref="Formatter.Encoding"/> as given on <see cref="Formatter"/>.
	/// </param>
	/// <returns><see langword="true" /> if the strings match; otherwise <see langword="false" />.</returns>
	/// <remarks>
	/// This method is allocation free.
	/// </remarks>
	public bool IsMatch(ReadOnlySpan<byte> encodedString) => this.Encoded.Span.SequenceEqual(encodedString);

	/// <inheritdoc cref="IsMatch(ReadOnlySpan{byte})"/>
	public bool IsMatch(ReadOnlySequence<byte> encodedString) => encodedString.SequenceEqual(this.Encoded.Span);

	/// <summary>
	/// Checks the string at the current reader position for equality with this string.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <returns><see langword="true" /> if the string matched; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// <para>The reader's position will be advanced if and only if the next token is a matching string.</para>
	/// <para>No exception is thrown if the reader is positioned at a non-string token.</para>
	/// <para>This method never allocates memory nor encodes/decodes strings.</para>
	/// </remarks>
	/// <exception cref="EndOfStreamException">Thrown if the reader has no more tokens, or the buffer contains an incomplete string token.</exception>
	public bool TryRead(ref Reader reader)
	{
		Requires.Argument(reader.Deformatter.Encoding == this.Formatter.Encoding, nameof(reader), "Reader's encoding must match this formatter's encoding.");

		switch (reader.NextTypeCode)
		{
			case TokenType.Null:
				return false;
			case TokenType.String:
				Reader peekReader = reader;
				bool match;
				if (peekReader.TryReadStringSpan(out ReadOnlySpan<byte> str))
				{
					match = this.IsMatch(str);
				}
				else
				{
					byte[]? array = null;
					try
					{
						peekReader.GetMaxStringLength(out _, out int maxBytes);
						Span<byte> scratchBuffer = maxBytes > Converter.MaxStackStringCharLength ? array = ArrayPool<byte>.Shared.Rent(maxBytes) : stackalloc byte[maxBytes];
						int byteCount = peekReader.ReadString(scratchBuffer);
						match = this.IsMatch(scratchBuffer[..byteCount]);
					}
					finally
					{
						if (array is not null)
						{
							ArrayPool<byte>.Shared.Return(array);
						}
					}
				}

				if (match)
				{
					reader = peekReader;
				}

				return match;
			default:
				return false;
		}
	}

	/// <inheritdoc />
	public bool Equals(PreformattedString? other) => this.Value == other?.Value;

	/// <inheritdoc />
	public override bool Equals(object? obj) => this.Equals(obj as PreformattedString);

	/// <inheritdoc />
	public override int GetHashCode() => this.Value.GetHashCode();
}
