// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Nerdbank.MessagePack;

/// <summary>
/// Provides built-in property naming conventions.
/// </summary>
public static class NamingConventions
{
	/// <summary>
	/// Gets a naming convention that converts .NET PascalCase property names to camelCase for msgpack.
	/// </summary>
	/// <remarks>
	/// This convention respects explicit property names specified via <see cref="PropertyShapeAttribute.Name"/>
	/// or <see cref="System.Runtime.Serialization.DataMemberAttribute.Name"/>.
	/// </remarks>
	public static readonly NamingConvention CamelCase = CamelCaseTransform;

	/// <summary>
	/// Checks if a property has an explicitly specified name that should not be transformed.
	/// </summary>
	/// <param name="property">The property to check.</param>
	/// <returns><see langword="true"/> if the property has an explicit name; otherwise, <see langword="false"/>.</returns>
	public static bool HasExplicitPropertyName(IPropertyShape property)
	{
		ArgumentNullException.ThrowIfNull(property);

		if (property.AttributeProvider is null)
		{
			return false;
		}

		// Check for PropertyShapeAttribute with a Name specified
		if (property.AttributeProvider.GetCustomAttributes(typeof(PropertyShapeAttribute), false).FirstOrDefault() is PropertyShapeAttribute { Name: not null })
		{
			return true;
		}

		// Check for DataMemberAttribute with a Name specified
		if (property.AttributeProvider.GetCustomAttributes(typeof(System.Runtime.Serialization.DataMemberAttribute), false).FirstOrDefault() is System.Runtime.Serialization.DataMemberAttribute { Name: not null })
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
}
