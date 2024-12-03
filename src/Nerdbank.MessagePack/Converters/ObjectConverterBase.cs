// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.Converters;

internal abstract class ObjectConverterBase<T> : MessagePackConverter<T>
{
	protected static void ApplyDescription(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema)
	{
		if (attributeProvider?.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute description)
		{
			propertySchema["description"] = description.Description;
		}
	}

	protected static void ApplyDefaultValue(ICustomAttributeProvider? attributeProvider, JsonObject propertySchema, IConstructorParameterShape? parameterShape)
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

	protected static bool IsNonNullable(IPropertyShape property, IConstructorParameterShape? associatedParameter)
		=> (!property.HasGetter || property.IsGetterNonNullable) &&
			(!property.HasSetter || property.IsSetterNonNullable) &&
			(associatedParameter is null || associatedParameter.IsNonNullable);

	protected static Dictionary<string, IConstructorParameterShape>? CreatePropertyAndParameterDictionary(IObjectTypeShape objectShape)
	{
		IConstructorShape? ctor = objectShape.GetConstructor();
		Dictionary<string, IConstructorParameterShape>? ctorParams = ctor?.GetParameters()
			.Where(p => p.Kind is ConstructorParameterKind.ConstructorParameter || p.IsRequired)
			.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
		return ctorParams;
	}
}
