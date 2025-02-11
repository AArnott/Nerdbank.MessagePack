// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using PolyType.Utilities;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A base class for converters that handle object types.
/// </summary>
/// <typeparam name="T">The type of object to be serialized.</typeparam>
internal abstract class ObjectConverterBase<T> : Converter<T>
{
	/// <summary>
	/// Adds a <c>description</c> property to the schema based on the <see cref="DescriptionAttribute"/> that is applied to the target.
	/// </summary>
	/// <param name="attributeProvider">The attribute provider for the target.</param>
	/// <param name="schema">The schema for the target.</param>
	/// <param name="namePrefix">An optional prefix to include in the description, or to use by itself when no <see cref="DescriptionAttribute"/> is present.</param>
	protected internal static void ApplyDescription(ICustomAttributeProvider? attributeProvider, JsonObject schema, string? namePrefix = null)
	{
		string? description;
		if (attributeProvider?.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute descriptionAttribute)
		{
			description = descriptionAttribute.Description;
			if (namePrefix is not null)
			{
				description = $"{namePrefix}: {description}";
			}
		}
		else
		{
			description = namePrefix;
		}

		if (description is not null)
		{
			schema["description"] = description;
		}
	}

	/// <summary>
	/// Adds a <c>default</c> property to the schema based on the <see cref="DefaultValueAttribute"/> that is applied to the property
	/// or the default parameter value assigned to the property's associated constructor parameter.
	/// </summary>
	/// <param name="attributeProvider">The attribute provider for the target.</param>
	/// <param name="propertySchema">The schema for the target.</param>
	/// <param name="parameterShape">The constructor parameter that matches the property, if applicable.</param>
	protected internal static void ApplyDefaultValue(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema, IConstructorParameterShape? parameterShape)
	{
		JsonValue? defaultValue =
			parameterShape?.HasDefaultValue is true ? CreateJsonValue(parameterShape.DefaultValue) :
			attributeProvider?.GetCustomAttribute<DefaultValueAttribute>() is DefaultValueAttribute att ? CreateJsonValue(att.Value) :
			null;

		if (defaultValue is not null)
		{
			propertySchema["default"] = defaultValue;
		}
	}

	/// <summary>
	/// Tests whether a given property is non-nullable.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="associatedParameter">The associated constructor parameter, if any.</param>
	/// <returns>A boolean value.</returns>
	protected internal static bool IsNonNullable(IPropertyShape property, IConstructorParameterShape? associatedParameter)
		=> (!property.HasGetter || property.IsGetterNonNullable) &&
			(!property.HasSetter || property.IsSetterNonNullable) &&
			(associatedParameter is null || associatedParameter.IsNonNullable);

	/// <summary>
	/// Creates a dictionary that maps property names to constructor parameters.
	/// </summary>
	/// <param name="objectShape">The object shape.</param>
	/// <returns>The dictionary.</returns>
	protected internal static Dictionary<string, IConstructorParameterShape>? CreatePropertyAndParameterDictionary(IObjectTypeShape objectShape)
	{
		var ctorParams = objectShape.Constructor?.Parameters
			.Where(p => p.Kind is ConstructorParameterKind.ConstructorParameter || p.IsRequired)
			.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
		return ctorParams;
	}
}
