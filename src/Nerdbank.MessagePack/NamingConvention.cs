// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Serialization;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A delegate that transforms property names from .NET to msgpack.
/// </summary>
/// <param name="propertyShape">The property shape providing metadata about the property.</param>
/// <returns>The transformed property name to use in msgpack.</returns>
public delegate string NamingConvention(IPropertyShape propertyShape);

/// <summary>
/// Provides built-in naming conventions for property name transformation.
/// </summary>
public static class NamingConventions
{
	/// <summary>
	/// Gets a naming convention that converts PascalCase property names to camelCase.
	/// </summary>
	public static readonly NamingConvention CamelCase = CamelCaseTransform;

	/// <summary>
	/// Gets a naming convention that converts camelCase property names to PascalCase.
	/// </summary>
	public static readonly NamingConvention PascalCase = PascalCaseTransform;

	/// <summary>
	/// Determines whether a property has an explicitly defined name that should not be transformed.
	/// </summary>
	/// <param name="property">The property shape to check.</param>
	/// <returns><see langword="true"/> if the property has an explicit name; otherwise, <see langword="false"/>.</returns>
	public static bool HasExplicitPropertyName(IPropertyShape property)
	{
		Requires.NotNull(property);

		if (property.AttributeProvider is null)
		{
			return false;
		}

		// Check for PropertyShapeAttribute with Name specified
		if (property.AttributeProvider.GetCustomAttributes(typeof(PropertyShapeAttribute), false).FirstOrDefault() is PropertyShapeAttribute { Name: not null })
		{
			return true;
		}

		// Check for DataMemberAttribute with Name specified
		if (property.AttributeProvider.GetCustomAttributes(typeof(DataMemberAttribute), false).FirstOrDefault() is DataMemberAttribute { Name: not null })
		{
			return true;
		}

		return false;
	}

	private static string CamelCaseTransform(IPropertyShape property)
	{
		if (HasExplicitPropertyName(property))
		{
			return property.Name;
		}

		string name = property.Name;
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

	private static string PascalCaseTransform(IPropertyShape property)
	{
		if (HasExplicitPropertyName(property))
		{
			return property.Name;
		}

		string name = property.Name;
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
