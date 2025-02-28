// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable SA1600

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ShapeShift.Json;

internal static class ThrowHelper
{
	[DoesNotReturn]
	internal static void ThrowArgumentException_InvalidUTF16(int charAsInt)
	{
		throw new ArgumentException($"Cannot encode invalid UTF-16: 0x{charAsInt:X2}");
	}

	[DoesNotReturn]
	internal static void ThrowArgumentException_InvalidUTF8(ReadOnlySpan<byte> value)
	{
		var builder = new StringBuilder();

		int printFirst10 = Math.Min(value.Length, 10);

		for (int i = 0; i < printFirst10; i++)
		{
			byte nextByte = value[i];
			if (IsPrintable(nextByte))
			{
				builder.Append((char)nextByte);
			}
			else
			{
				builder.Append($"0x{nextByte:X2}");
			}
		}

		if (printFirst10 < value.Length)
		{
			builder.Append("...");
		}

		throw new ArgumentException($"Cannot encode invalid UTF-8: {builder}");
	}

	private static bool IsPrintable(byte value) => value >= 0x20 && value < 0x7F;
}
