﻿// Copyright (c) Andrew Arnott. All rights reserved.
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
	public abstract bool VectorsMustHaveLengthPrefix { get; }

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

	/// <summary>
	/// Introduces a collection with a prefixed size.
	/// </summary>
	/// <param name="writer">The writer.</param>
	/// <param name="length">The number of elements in the collection.</param>
	public abstract void WriteStartVector(ref Writer writer, int length);

	/// <summary>
	/// Writes a separator between two array elements (if the format requires it).
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteVectorElementSeparator(ref Writer writer);

	/// <summary>
	/// Writes a trailer after the last array element (if the format requires it).
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteEndVector(ref Writer writer);

	/// <summary>
	/// Introduces a map with a prefixed size.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	/// <param name="count">The number of key=value pairs in the map.</param>
	public abstract void WriteStartMap(ref Writer writer, int count);

	/// <summary>
	/// Writes a separator between two key=value pairs (if the format requires it).
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteMapPairSeparator(ref Writer writer);

	/// <summary>
	/// Writes a separator between a key and matching value (if the format requires it).
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteMapKeyValueSeparator(ref Writer writer);

	/// <summary>
	/// Writes a marker that indicates the end of a map has been reached (if the format requires it).
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteEndMap(ref Writer writer);

	/// <summary>
	/// Writes the token representing a <see langword="null" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	public abstract void WriteNull(ref Writer writer);

	/// <summary>
	/// Writes a <see langword="bool" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="WriteStartVector(ref Writer, int)" path="/param[@name='writer']"/></param>
	/// <param name="value">The value to write.</param>
	public abstract void Write(ref Writer writer, bool value);

	/// <summary>
	/// Writes a <see langword="char" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, char value);

	/// <summary>
	/// Writes a <see langword="byte" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, byte value);

	/// <summary>
	/// Writes a <see langword="sbyte" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, sbyte value);

	/// <summary>
	/// Writes a <see langword="ushort" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, ushort value);

	/// <summary>
	/// Writes a <see langword="short" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, short value);

	/// <summary>
	/// Writes a <see langword="uint" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, uint value);

	/// <summary>
	/// Writes a <see langword="int" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, int value);

	/// <summary>
	/// Writes a <see langword="ulong" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, ulong value);

	/// <summary>
	/// Writes a <see langword="long" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, long value);

	/// <summary>
	/// Writes a <see langword="float" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, float value);

	/// <summary>
	/// Writes a <see langword="double" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, double value);

	/// <summary>
	/// Writes a <see langword="string" /> value.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, string? value);

	/// <summary>
	/// Writes a span of characters as a <see cref="TokenType.String"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, scoped ReadOnlySpan<char> value);

	/// <summary>
	/// Writes a span of bytes as a <see cref="TokenType.Binary"/> blob.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void Write(ref Writer writer, scoped ReadOnlySpan<byte> value);

	/// <summary>
	/// Writes a sequence of bytes as a <see cref="TokenType.Binary"/> blob.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
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

	/// <summary>
	/// Writes a span of bytes as a <see cref="TokenType.String"/> token that has already been encoded with <see cref="Encoding"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void WriteEncodedString(ref Writer writer, scoped ReadOnlySpan<byte> value);

	/// <summary>
	/// Writes a sequence of bytes as a <see cref="TokenType.String"/> token that has already been encoded with <see cref="Encoding"/>.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="value"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='value']"/></param>
	public abstract void WriteEncodedString(ref Writer writer, in ReadOnlySequence<byte> value);

	/// <summary>
	/// Writes the header that introduces a binary buffer in a binary-encoded format.
	/// </summary>
	/// <param name="writer"><inheritdoc cref="Write(ref Writer, bool)" path="/param[@name='writer']"/></param>
	/// <param name="length">The length in bytes of the binary data that will be subsequently written directly to the buffer.</param>
	/// <returns><see langword="true" /> if the format allows for raw binary blobs; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// When this method returns <see langword="false"/>, the caller should use <see cref="Write(ref Writer, ReadOnlySpan{byte})"/>
	/// or <see cref="Write(ref Writer, in ReadOnlySequence{byte})"/> instead.
	/// </remarks>
	public virtual bool TryWriteStartBinary(ref Writer writer, int length) => false;
}
