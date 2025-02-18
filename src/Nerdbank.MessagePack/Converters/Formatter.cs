// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A class for writing structures to a  with a particular format as determined by a derived type.
/// </summary>
/// <remarks>
/// Derived types should be implemented in a thread-safe way, ideally by being immutable.
/// </remarks>
public abstract record Formatter
{
	/// <summary>
	/// Gets the name of the format implemented by the derived type.
	/// </summary>
	public abstract string FormatName { get; }

	/// <summary>
	/// Gets the text encoding used by this formatter.
	/// </summary>
	public abstract Encoding Encoding { get; }

	/// <summary>
	/// Gets a value indicating whether the format requires arrays must be prefixed with their length.
	/// </summary>
	public abstract bool ArrayLengthRequiredInHeader { get; }

	/// <summary>
	/// Encodes and formats a given string.
	/// </summary>
	/// <param name="value">The string to be formatted.</param>
	/// <param name="encodedBytes">Receives the encoded characters (e.g. UTF-8) without any header or footer.</param>
	/// <param name="formattedBytes">Receives the formatted bytes, which is a superset of <paramref name="encodedBytes"/> that adds a header and/or footer as required by the formatter.</param>
	/// <remarks>
	/// This is useful as an optimization so that common strings need not be repeatedly encoded/decoded.
	/// </remarks>
	public abstract void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> encodedBytes, out ReadOnlyMemory<byte> formattedBytes);

	public abstract void WriteArrayStart(ref Writer writer, int length);

	public abstract void WriteArrayElementSeparator(ref Writer writer);

	public abstract void WriteArrayEnd(ref Writer writer);

	public abstract void WriteMapStart(ref Writer writer, int length);

	public abstract void WriteMapKeyValueSeparator(ref Writer writer);

	public abstract void WriteMapValueTrailer(ref Writer writer);

	public abstract void WriteMapEnd(ref Writer writer);

	public abstract void WriteNull(ref Writer writer);

	public abstract void Write(ref Writer writer, bool value);

	public abstract void Write(ref Writer writer, char value);

	public abstract void Write(ref Writer writer, byte value);

	public abstract void Write(ref Writer writer, sbyte value);

	public abstract void Write(ref Writer writer, ushort value);

	public abstract void Write(ref Writer writer, short value);

	public abstract void Write(ref Writer writer, uint value);

	public abstract void Write(ref Writer writer, int value);

	public abstract void Write(ref Writer writer, ulong value);

	public abstract void Write(ref Writer writer, long value);

	public abstract void Write(ref Writer writer, float value);

	public abstract void Write(ref Writer writer, double value);

	public abstract void Write(ref Writer writer, string? value);

	public abstract void Write(ref Writer writer, scoped ReadOnlySpan<char> value);

	public abstract void Write(ref Writer writer, scoped ReadOnlySpan<byte> value);

	public abstract void Write(ref Writer writer, in ReadOnlySequence<byte> value);

	/// <summary>
	/// Get the number of bytes required to format a value.
	/// </summary>
	/// <param name="value">The value to format.</param>
	/// <returns>The byte length.</returns>
	public abstract int GetEncodedLength(long value);

	/// <summary>
	/// Get the number of bytes required to encode a value.
	/// </summary>
	/// <param name="value">The value to encode.</param>
	/// <returns>The byte length.</returns>
	public abstract int GetEncodedLength(ulong value);

	public abstract void WriteEncodedString(ref Writer writer, scoped ReadOnlySpan<byte> value);

	public abstract void WriteEncodedString(ref Writer writer, in ReadOnlySequence<byte> value);

	public virtual bool TryWriteBinHeader(ref Writer writer, int length) => false;
}
