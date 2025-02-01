// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Describes a msgpack extension.
/// </summary>
/// <param name="TypeCode"><inheritdoc cref="ExtensionHeader(sbyte, uint)" path="/param[@name='TypeCode']"/></param>
/// <param name="Data">The data payload, in whatever format is prescribed by the extension as per the <paramref name="TypeCode"/>.</param>
public record struct Extension(sbyte TypeCode, ReadOnlySequence<byte> Data)
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Extension"/> struct.
	/// </summary>
	/// <param name="typeCode"><inheritdoc cref="Extension(sbyte, ReadOnlySequence{byte})" path="/param[@name='TypeCode']"/></param>
	/// <param name="data"><inheritdoc cref="Extension(sbyte, ReadOnlySequence{byte})" path="/param[@name='Data']"/></param>
	public Extension(sbyte typeCode, ReadOnlyMemory<byte> data)
		: this(typeCode, new ReadOnlySequence<byte>(data))
	{
	}

	/// <summary>
	/// Gets the header for the extension that should precede the <see cref="Data"/> in the msgpack encoded format.
	/// </summary>
	public ExtensionHeader Header => new(this.TypeCode, checked((uint)this.Data.Length));
}
