// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class PolyfillExtensions
{
	extension(Array)
	{
		public static int MaxLength => 0x7FEFFFFF; // The maximum array length in .NET, which is less than int.MaxValue to avoid overflow when calculating byte offsets.
	}
}
