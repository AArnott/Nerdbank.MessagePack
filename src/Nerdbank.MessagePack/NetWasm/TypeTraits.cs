// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Type queries that also work on NetWasm (netwasm0.1), whose <see cref="Type"/> exposes no IsValueType/IsEnum/etc.
/// </summary>
internal static class TypeTraits
{
	/// <summary>Gets a value indicating whether <typeparamref name="T"/> is a value type.</summary>
	/// <typeparam name="T">The type to test.</typeparam>
	/// <returns><see langword="true"/> if <typeparamref name="T"/> is a value type.</returns>
	internal static bool IsValueType<T>() =>
#if NETWASM
		default(T) is not null || Nullable.GetUnderlyingType(typeof(T)) is not null;
#else
		typeof(T).IsValueType;
#endif

	/// <summary>Gets a value indicating whether <paramref name="type"/> is an enum type.</summary>
	/// <param name="type">The type to test.</param>
	/// <returns><see langword="true"/> if <paramref name="type"/> is an enum.</returns>
	internal static bool IsEnum(Type type)
	{
#if NETWASM
		try
		{
			Enum.GetUnderlyingType(type);
			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
#else
		return type.IsEnum;
#endif
	}
}
