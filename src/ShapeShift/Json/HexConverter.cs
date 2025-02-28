// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma warning disable SA1600

using System.Runtime.CompilerServices;

namespace ShapeShift.Json;

internal static class HexConverter
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static char ToCharUpper(int value)
	{
		value &= 0xF;
		value += '0';

		if (value > '9')
		{
			value += 'A' - ('9' + 1);
		}

		return (char)value;
	}
}
