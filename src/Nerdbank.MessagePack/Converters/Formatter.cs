// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

internal abstract class Formatter
{
	protected internal abstract void GetEncodedStringBytes(string value, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
}
