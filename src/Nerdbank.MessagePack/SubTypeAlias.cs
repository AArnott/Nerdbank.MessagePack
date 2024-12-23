﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Acts as a type union between a <see cref="string"/> and an <see cref="int"/>, which are the allowed types for sub-type aliases.
/// </summary>
internal struct SubTypeAlias : IEquatable<SubTypeAlias>
{
	private string? stringAlias;
	private ReadOnlyMemory<byte> utfAlias;
	private ReadOnlyMemory<byte> msgpackAlias;
	private int? intAlias;

	/// <inheritdoc cref="SubTypeAlias(string)"/>
	internal SubTypeAlias(int alias)
	{
		this.intAlias = alias;
		byte[] msgpack = new byte[5]; // maximum possible value can be encoded in this buffer.
		Assumes.True(MessagePackPrimitives.TryWrite(msgpack, alias, out int bytesWritten));
		this.msgpackAlias = msgpack.AsMemory(0, bytesWritten);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SubTypeAlias"/> struct.
	/// </summary>
	/// <param name="alias">The alias.</param>
	internal SubTypeAlias(string alias)
	{
		this.stringAlias = alias;
		StringEncoding.GetEncodedStringBytes(alias, out this.utfAlias, out this.msgpackAlias);
	}

	/// <summary>
	/// The types of values that are allowed for use as aliases.
	/// </summary>
	internal enum AliasType
	{
		/// <summary>
		/// The struct is uninitialized. This constitutes an internal error.
		/// </summary>
		None,

		/// <summary>
		/// The alias is an <see cref="int"/>.
		/// </summary>
		Integer,

		/// <summary>
		/// The alias is a <see cref="string"/>.
		/// </summary>
		String,
	}

	/// <summary>
	/// Gets the type of this alias.
	/// </summary>
	public AliasType Type => this.stringAlias is not null ? AliasType.String : this.intAlias is not null ? AliasType.Integer : AliasType.None;

	/// <summary>
	/// Gets the <see cref="string"/> alias.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Type"/> is not <see cref="AliasType.String"/>.</exception>
	public string StringAlias => this.stringAlias ?? throw new InvalidOperationException();

	/// <summary>
	/// Gets the <see cref="int"/> alias.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Type"/> is not <see cref="AliasType.Integer"/>.</exception>
	public int IntAlias => this.intAlias ?? throw new InvalidOperationException();

	/// <summary>
	/// Gets the msgpack encoding of the alias.
	/// </summary>
	public ReadOnlyMemory<byte> MsgPackAlias => this.msgpackAlias;

	/// <summary>
	/// Gets the UTF-8 encoding of the <see cref="string"/> alias.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Type"/> is not <see cref="AliasType.String"/>.</exception>
	public ReadOnlyMemory<byte> Utf8Alias => this.stringAlias is not null ? this.utfAlias : throw new InvalidOperationException();

	public static implicit operator SubTypeAlias(string alias) => new(alias);

	public static implicit operator SubTypeAlias(int alias) => new(alias);

	/// <inheritdoc/>
	public bool Equals(SubTypeAlias other) => this.stringAlias == other.stringAlias && this.intAlias == other.intAlias;

	/// <inheritdoc/>
	public override bool Equals([NotNullWhen(true)] object? obj) => obj is SubTypeAlias other && this.Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode() => this.stringAlias?.GetHashCode() ?? this.intAlias?.GetHashCode() ?? 0;
}
