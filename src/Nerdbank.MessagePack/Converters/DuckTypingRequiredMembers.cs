// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Collects the property names that identify a duck-typed object case.
/// </summary>
internal static class DuckTypingRequiredMembers
{
	/// <summary>
	/// Gets the serialized names of the required, readable members of an object case.
	/// </summary>
	/// <param name="shape">The case shape. Must be an object.</param>
	/// <param name="getSerializedName">Maps a property to the name written on the wire. Shape names are used when no naming policy applies yet.</param>
	/// <returns>The names, sorted ordinally, with no duplicates. Empty when the case is a catch-all.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="shape"/> is not an object, or when two required members share a name.</exception>
	/// <remarks>
	/// A member counts when the object converter would require it and can read it:
	/// a required constructor parameter, a <see cref="PropertyShapeAttribute"/> with <see cref="PropertyShapeAttribute.IsRequired"/> set,
	/// or a required parameter that has no matching property (its parameter name is used as written, without a naming policy).
	/// Getter-only computed members are omitted. <see cref="UnusedDataPacket"/> is omitted.
	/// A property with a setter, a constructor parameter, or a mutable collection or dictionary is readable.
	/// </remarks>
	internal static string[] GetRequiredNames(ITypeShape shape, Func<IPropertyShape, string> getSerializedName)
	{
		if (shape is not IObjectTypeShape objectShape)
		{
			throw new ArgumentException($"Duck typing cases must be object types, but '{shape.Type.FullName}' is not.", nameof(shape));
		}

		ConstructorParameterMapping? mapping = objectShape.Constructor is { } constructor
			? new ConstructorParameterMapping(constructor, getSerializedName)
			: null;

		List<string> names = [];
		foreach (IPropertyShape property in objectShape.Properties)
		{
			if (property.PropertyType.Type == typeof(UnusedDataPacket))
			{
				continue;
			}

			IParameterShape? parameter = mapping?.GetParameter(property);
			if (!IsReadable(property, parameter))
			{
				continue;
			}

			if (!IsRequired(property, parameter))
			{
				continue;
			}

			AddName(shape, names, getSerializedName(property));
		}

		if (mapping is not null)
		{
			foreach ((IParameterShape parameter, IPropertyShape? property) in mapping.Parameters)
			{
				if (property is not null || parameter.ParameterType.Type == typeof(UnusedDataPacket) || !parameter.IsRequired)
				{
					continue;
				}

				// The object converter writes an unmatched parameter under its parameter name and does not apply the naming policy.
				AddName(shape, names, parameter.Name);
			}
		}

		if (names.Count == 0)
		{
			return [];
		}

		names.Sort(StringComparer.Ordinal);
		return names.ToArray();
	}

	/// <summary>
	/// Throws when a case is not serialized as a name-keyed map.
	/// </summary>
	/// <param name="shape">The case shape.</param>
	/// <param name="ignoreKeyAttributes"><see langword="true"/> when <see cref="SerializerConfiguration.IgnoreKeyAttributes"/> is set.</param>
	/// <exception cref="NotSupportedException">Thrown when the case is a tuple or uses <see cref="KeyAttribute"/>.</exception>
	internal static void EnsureNameKeyedMap(ITypeShape shape, bool ignoreKeyAttributes)
	{
		if (shape is not IObjectTypeShape objectShape)
		{
			return;
		}

		if (objectShape.IsTupleType)
		{
			throw new NotSupportedException($"Duck typing case '{shape.Type.FullName}' is a tuple. Duck typing matches name-keyed maps.");
		}

		if (ignoreKeyAttributes)
		{
			return;
		}

		foreach (IPropertyShape property in objectShape.Properties)
		{
			if (property.AttributeProvider is null)
			{
				continue;
			}

			foreach (KeyAttribute _ in property.AttributeProvider.GetCustomAttributes<KeyAttribute>(inherit: false))
			{
				throw new NotSupportedException($"Duck typing case '{shape.Type.FullName}' uses {nameof(KeyAttribute)}. Duck typing matches name-keyed maps.");
			}
		}
	}

	/// <summary>
	/// Determines whether the object converter can assign the property from the payload.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="parameter">The constructor parameter mapped to the property, if any.</param>
	/// <returns><see langword="true"/> when the property can be populated.</returns>
	private static bool IsReadable(IPropertyShape property, IParameterShape? parameter)
	{
		if (property.HasSetter || parameter is not null)
		{
			return true;
		}

		return property.PropertyType is IEnumerableTypeShape { ConstructionStrategy: CollectionConstructionStrategy.Mutable }
			or IDictionaryTypeShape { ConstructionStrategy: CollectionConstructionStrategy.Mutable };
	}

	/// <summary>
	/// Determines whether the object converter treats the member as required.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <param name="parameter">The constructor parameter mapped to the property, if any.</param>
	/// <returns><see langword="true"/> when the member is required evidence.</returns>
	private static bool IsRequired(IPropertyShape property, IParameterShape? parameter)
	{
		if (parameter is not null)
		{
			return parameter.IsRequired;
		}

		if (property.AttributeProvider is null)
		{
			return false;
		}

		foreach (PropertyShapeAttribute attribute in property.AttributeProvider.GetCustomAttributes<PropertyShapeAttribute>(inherit: true))
		{
			return attribute.IsRequired;
		}

		return false;
	}

	/// <summary>
	/// Adds a required name, rejecting duplicates within the case.
	/// </summary>
	/// <param name="shape">The case shape.</param>
	/// <param name="names">The names collected so far.</param>
	/// <param name="name">The name to add.</param>
	private static void AddName(ITypeShape shape, List<string> names, string name)
	{
		for (int i = 0; i < names.Count; i++)
		{
			if (string.Equals(names[i], name, StringComparison.Ordinal))
			{
				throw new ArgumentException($"Duck typing case '{shape.Type.FullName}' has more than one required member serialized as '{name}'.", nameof(shape));
			}
		}

		names.Add(name);
	}
}
