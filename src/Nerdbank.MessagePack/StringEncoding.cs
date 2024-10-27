// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.MessagePack;

/// <summary>
/// Shareable values related to string encoding.
/// </summary>
internal static class StringEncoding
{
	/// <summary>
	/// UTF-8 encoding without a byte order mark.
	/// </summary>
	internal static readonly Encoding UTF8 = new UTF8Encoding(false);
}
