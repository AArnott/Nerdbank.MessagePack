// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// An enumeration of all the msgpack primitive value types.
/// </summary>
public enum MessagePackValueKind : byte
{
	/// <summary>
	/// A msgpack nil value.
	/// </summary>
	Nil,

	/// <summary>
	/// A non-negative integer.
	/// </summary>
	UnsignedInteger,

	/// <summary>
	/// A negative integer.
	/// </summary>
	SignedInteger,

	/// <summary>
	/// A boolean value.
	/// </summary>
	Boolean,

	/// <summary>
	/// A 32-bit floating point value.
	/// </summary>
	Single,

	/// <summary>
	/// A 64-bit floating point value.
	/// </summary>
	Double,

	/// <summary>
	/// A string value.
	/// </summary>
	String,

	/// <summary>
	/// A binary blob.
	/// </summary>
	Binary,

	/// <summary>
	/// An array.
	/// </summary>
	Array,

	/// <summary>
	/// A map.
	/// </summary>
	Map,

	/// <summary>
	/// A msgpack extension.
	/// </summary>
	Extension,

	/// <summary>
	/// A <see cref="DateTime"/> value.
	/// </summary>
	DateTime,
}
