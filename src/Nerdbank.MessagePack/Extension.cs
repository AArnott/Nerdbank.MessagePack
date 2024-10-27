// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

public struct Extension
{
	public Extension(sbyte typeCode, Memory<byte> data)
	{
		this.TypeCode = typeCode;
		this.Data = new ReadOnlySequence<byte>(data);
	}

	public Extension(sbyte typeCode, ReadOnlySequence<byte> data)
	{
		this.TypeCode = typeCode;
		this.Data = data;
	}

	public sbyte TypeCode { get; private set; }

	public ReadOnlySequence<byte> Data { get; private set; }

	public ExtensionHeader Header => new ExtensionHeader(this.TypeCode, (uint)this.Data.Length);
}
