// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable SA1600 // Elements should have documentation

namespace ShapeShift.Json;

internal static class JsonConstants
{
	internal const byte OpenBrace = (byte)'{';
	internal const byte CloseBrace = (byte)'}';
	internal const byte OpenBracket = (byte)'[';
	internal const byte CloseBracket = (byte)']';
	internal const byte Space = (byte)' ';
	internal const byte CarriageReturn = (byte)'\r';
	internal const byte LineFeed = (byte)'\n';
	internal const byte Tab = (byte)'\t';
	internal const byte ListSeparator = (byte)',';
	internal const byte KeyValueSeparator = (byte)':';
	internal const byte Quote = (byte)'"';
	internal const byte BackSlash = (byte)'\\';
	internal const byte Slash = (byte)'/';
	internal const byte BackSpace = (byte)'\b';
	internal const byte FormFeed = (byte)'\f';
	internal const byte Asterisk = (byte)'*';
	internal const byte Colon = (byte)':';

	// In the worst case, an ASCII character represented as a single utf-8 byte could expand 6x when escaped.
	// For example: '+' becomes '\u0043'
	// Escaping surrogate pairs (represented by 3 or 4 utf-8 bytes) would expand to 12 bytes (which is still <= 6x).
	// The same factor applies to utf-16 characters.
	internal const int MaxExpansionFactorWhileEscaping = 6;
}
