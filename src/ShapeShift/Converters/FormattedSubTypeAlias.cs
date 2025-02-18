// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace ShapeShift.Converters;

/// <summary>
/// A subtype alias that has been pre-formatted for optimized serialization.
/// </summary>
internal struct FormattedSubTypeAlias : IEquatable<FormattedSubTypeAlias>
{
	private SubTypeAlias alias;
	private ReadOnlyMemory<byte> encodedAlias;
	private ReadOnlyMemory<byte> formattedAlias;

	/// <summary>
	/// Initializes a new instance of the <see cref="FormattedSubTypeAlias"/> struct.
	/// </summary>
	/// <param name="alias">The alias.</param>
	/// <param name="formatter">The formatter to use for pre-formatting.</param>
	internal FormattedSubTypeAlias(SubTypeAlias alias, Formatter formatter)
	{
		this.alias = alias;
		this.Formatter = formatter;

		switch (alias.Type)
		{
			case SubTypeAlias.AliasType.Integer:
				Writer writer = new(new BufferWriter(SequencePool<byte>.Shared, new byte[5]), formatter);
				formatter.Write(ref writer, alias.IntAlias);
				this.formattedAlias = writer.FlushAndGetArray();
				break;
			case SubTypeAlias.AliasType.String:
				formatter.GetEncodedStringBytes(alias.StringAlias, out this.encodedAlias, out this.formattedAlias);
				break;
			default: throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Gets the formatter used to pre-format the alias.
	/// </summary>
	public Formatter Formatter { get; }

	/// <inheritdoc cref="SubTypeAlias.Type"/>
	public SubTypeAlias.AliasType Type => this.alias.Type;

	/// <inheritdoc cref="SubTypeAlias.StringAlias"/>
	public string StringAlias => this.alias.StringAlias;

	/// <inheritdoc cref="SubTypeAlias.IntAlias"/>
	public int IntAlias => this.alias.IntAlias;

	/// <summary>
	/// Gets the formatted alias, whether it is an integer or a string.
	/// </summary>
	public ReadOnlyMemory<byte> FormattedAlias => this.formattedAlias;

	/// <summary>
	/// Gets the UTF-8 encoding of the <see cref="string"/> alias.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if <see cref="Type"/> is not <see cref="SubTypeAlias.AliasType.String"/>.</exception>
	public ReadOnlyMemory<byte> EncodedAlias => this.alias.Type == SubTypeAlias.AliasType.String ? this.encodedAlias : throw new InvalidOperationException();

	/// <inheritdoc/>
	public bool Equals(FormattedSubTypeAlias other) => this.alias.Equals(other.alias);

	/// <inheritdoc/>
	public override bool Equals([NotNullWhen(true)] object? obj) => obj is FormattedSubTypeAlias other && this.Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode() => this.alias.GetHashCode();
}
