// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift;

/// <summary>
/// Defines a transformation for property names from .NET to msgpack.
/// </summary>
public abstract class NamingPolicy
{
	/// <summary>
	/// Gets a naming policy that converts a .NET PascalCase property to camelCase for msgpack.
	/// </summary>
	public static NamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

	/// <summary>
	/// Gets a naming policy that converts a .NET camelCase property to PascalCase for msgpack.
	/// </summary>
	public static NamingPolicy PascalCase { get; } = new PascalCaseNamingPolicy();

	/// <summary>
	/// Transforms a property name as defined in .NET to a property name as it should be serialized to MessagePack.
	/// </summary>
	/// <param name="name">The .NET property name.</param>
	/// <returns>The msgpack property name.</returns>
	public abstract string ConvertName(string name);

	private class CamelCaseNamingPolicy : NamingPolicy
	{
		/// <summary>
		/// Converts a PascalCase identifier to camelCase.
		/// </summary>
		/// <param name="name">The PascalCase identifier.</param>
		/// <returns>The camelCase identifier. </returns>
		public override string ConvertName(string name)
		{
			if (name.Length == 0 || !char.IsUpper(name[0]))
			{
				return name;
			}

#if NET
			return string.Create(name.Length, name, static (span, name) =>
			{
				span[0] = char.ToLowerInvariant(name[0]);
				name.AsSpan(1).CopyTo(span.Slice(1));
			});
#else
			return char.ToLowerInvariant(name[0]) + name.Substring(1);
#endif
		}
	}

	private class PascalCaseNamingPolicy : NamingPolicy
	{
		/// <summary>
		/// Converts a camelCase identifier to PascalCase.
		/// </summary>
		/// <param name="name">The camelCase identifier.</param>
		/// <returns>The PascalCase identifier. </returns>
		public override string ConvertName(string name)
		{
			if (name.Length == 0 || !char.IsLower(name[0]))
			{
				return name;
			}

#if NET
			return string.Create(name.Length, name, static (span, name) =>
			{
				span[0] = char.ToUpperInvariant(name[0]);
				name.AsSpan(1).CopyTo(span.Slice(1));
			});
#else
			return char.ToUpperInvariant(name[0]) + name.Substring(1);
#endif
		}
	}
}
