// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Sdk;

internal static class Skip
{
	internal static void If(bool condition, string? message = null)
	{
		if (condition)
		{
			throw SkipException.ForSkip(message ?? "Skipped.");
		}
	}

	internal static void IfNot(bool condition, string? message = null)
	{
		if (!condition)
		{
			throw SkipException.ForSkip(message ?? "Skipped.");
		}
	}
}
