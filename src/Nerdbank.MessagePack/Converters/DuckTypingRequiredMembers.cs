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
	/// <exception cref="NotSupportedException">Thrown when a required member cannot round-trip: a constructor parameter with no matching property, a required property with no getter, or a required getter-only collection on a type with a parameterized constructor.</exception>
	/// <remarks>
	/// A member counts when the object converter would write it and require it:
	/// a required constructor parameter that matches a property with a getter, a <see langword="required"/> property, or <see cref="PropertyShapeAttribute.IsRequired"/>.
	/// Getter-only computed members are omitted. A required property with no getter is rejected. <see cref="UnusedDataPacket"/> is omitted.
	/// A property with a setter, a constructor parameter, or a mutable collection or dictionary is readable.
	/// A getter-only collection is readable only when the converter populates it: the type has no parameterized constructor, or the collection is itself a constructor parameter.
	/// </remarks>
	internal static string[] GetRequiredNames(ITypeShape shape, Func<IPropertyShape, string> getSerializedName)
	{
		shape = ObjectView(shape);
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

			if (!property.HasGetter)
			{
				throw new NotSupportedException($"Duck typing case '{shape.Type.FullName}' requires '{property.Name}', but the object converter does not write it because the property has no getter. The case cannot round-trip.");
			}

			if (parameter is null && !property.HasSetter && IsInPlaceCollection(property) && HasParameterizedConstructor(objectShape))
			{
				// The map converter writes this collection, but a parameterized constructor only assigns parameters.
				// The values are skipped on read and the property keeps its initializer, so the case would not round-trip.
				throw new NotSupportedException($"Duck typing case '{shape.Type.FullName}' requires '{property.Name}', but the object converter does not populate that getter-only collection when the type has a parameterized constructor. The case cannot round-trip.");
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

				// The map converter can read this parameter, but it has no property to write. A duck-typed case would not round-trip.
				throw new NotSupportedException($"Duck typing case '{shape.Type.FullName}' has required constructor parameter '{parameter.Name}' that does not match a property. The object converter does not write that parameter, so the case cannot round-trip.");
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
		shape = ObjectView(shape);
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
	/// Returns the object shape behind a derived-type hierarchy.
	/// </summary>
	/// <param name="shape">A case shape. A <see cref="DerivedTypeShapeAttribute"/> hierarchy is a union shape whose object view is <see cref="IUnionTypeShape.BaseType"/>.</param>
	/// <returns>The object view when <paramref name="shape"/> is a type hierarchy; otherwise <paramref name="shape"/>.</returns>
	/// <remarks>
	/// C# and F# union shapes are left unchanged. Their base view is an empty object and is not the case's data.
	/// </remarks>
	internal static ITypeShape ObjectView(ITypeShape shape)
	{
		if (shape is IUnionTypeShape { UnionKind: UnionTypeShapeKind.TypeHierarchy or UnionTypeShapeKind.Unknown } union
			&& union.BaseType is IObjectTypeShape)
		{
			return union.BaseType;
		}

		return shape;
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

		return IsInPlaceCollection(property);
	}

	/// <summary>
	/// Determines whether the property is a mutable collection or dictionary that can be filled in place.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns><see langword="true"/> when the property type is a mutable collection or dictionary.</returns>
	private static bool IsInPlaceCollection(IPropertyShape property)
		=> property.PropertyType is IEnumerableTypeShape { ConstructionStrategy: CollectionConstructionStrategy.Mutable }
			or IDictionaryTypeShape { ConstructionStrategy: CollectionConstructionStrategy.Mutable };

	/// <summary>
	/// Determines whether the object converter assigns members only through constructor parameters.
	/// </summary>
	/// <param name="objectShape">The object shape.</param>
	/// <returns><see langword="true"/> when the type has a constructor with parameters.</returns>
	private static bool HasParameterizedConstructor(IObjectTypeShape objectShape)
		=> objectShape.Constructor is { Parameters.Count: > 0 };

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
