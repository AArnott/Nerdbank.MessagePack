// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.MessagePack;

internal static class StringEncoding
{
	internal static readonly Encoding UTF8 = new UTF8Encoding(false);
}
