// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// Enumerates the kinds of tokens that tend to be defined across a variety of formats.
/// </summary>
public enum TypeCode : byte
{
	/// <summary>
	/// An unknown token, either because none has been read or it is a format-specific token type.
	/// </summary>
	Unknown,

	/// <summary>
	/// An integer token, which may be of any length and may be signed.
	/// </summary>
	Integer,

	/// <summary>
	/// A <see langword="null" /> value.
	/// </summary>
	Nil,

	/// <summary>
	/// A <see langword="bool"/> value.
	/// </summary>
	Boolean,

	/// <summary>
	/// A floating point integer (e.g. <see langword="float"/> or <see langword="double" />).
	/// </summary>
	Float,

	/// <summary>
	/// A <see langword="string" /> value.
	/// </summary>
	String,

	/// <summary>
	/// A binary blob that is opaque to the format.
	/// </summary>
	Binary,

	/// <summary>
	/// A collection of values.
	/// </summary>
	Vector,

	/// <summary>
	/// A dictionary of key=value pairs.
	/// </summary>
	Map,
}
