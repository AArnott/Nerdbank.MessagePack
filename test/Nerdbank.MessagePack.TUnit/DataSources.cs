// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

internal static class DataSources
{
	public static IEnumerable<bool> BooleanValues()
	{
		yield return true;
		yield return false;
	}
}

internal static class EnumDataSource<TEnum>
	where TEnum : struct, Enum
{
	public static IEnumerable<TEnum> Values()
	{
#if NET
		foreach (TEnum value in Enum.GetValues<TEnum>())
#else
		foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
#endif
		{
			yield return value;
		}
	}
}
