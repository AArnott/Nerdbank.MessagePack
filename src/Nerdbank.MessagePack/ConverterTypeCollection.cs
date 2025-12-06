// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using static Nerdbank.MessagePack.ConverterTypeCollection;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable collection of <see cref="MessagePackConverter{T}"/> types.
/// </summary>
[CollectionBuilder(typeof(ConverterTypeCollection), nameof(Create))]
public class ConverterTypeCollection : IReadOnlyCollection<TypeWithDefaultConstructor>
{
	private ConverterTypeCollection(FrozenDictionary<Type, TypeWithDefaultConstructor> map)
	{
		this.Map = map;
	}

	/// <inheritdoc />
	public int Count => this.Map.Count;

	/// <summary>
	/// Gets a mapping of data types to their custom converter types.
	/// </summary>
	private FrozenDictionary<Type, TypeWithDefaultConstructor> Map { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterTypeCollection"/> class
	/// populated with a collection of <see cref="Type"/>s.
	/// </summary>
	/// <param name="converterTypes">The <see cref="MessagePackConverter{T}"/> types that should be elements in the collection.</param>
	/// <returns>The newly initialized collection type.</returns>
	public static ConverterTypeCollection Create(ReadOnlySpan<TypeWithDefaultConstructor> converterTypes)
	{
		Dictionary<Type, TypeWithDefaultConstructor> map = [];

		foreach (Type converterType in converterTypes)
		{
			Requires.Argument(converterType is not null, nameof(converterTypes), "Null elements are not allowed.");
			Requires.Argument(converterType.IsClass && !converterType.IsAbstract, nameof(converterTypes), "All types must be concrete classes.");

			// Discover what the data type being converted is.
			Type? baseType = converterType.BaseType;
			while (baseType is not null && !(baseType.IsGenericType && typeof(MessagePackConverter<>).IsAssignableFrom(baseType.GetGenericTypeDefinition())))
			{
				baseType = baseType.BaseType;
			}

			Requires.Argument(baseType is not null, nameof(converterType), $"Type does not derive from MessagePackConverter<T>.");
			Type dataType = baseType.GetGenericArguments()[0];

			// If the data type has no generic type arguments, turn it into a proper generic type definition so we can find it later.
			if (dataType.GenericTypeArguments is [{ IsGenericParameter: true }, ..])
			{
				dataType = dataType.GetGenericTypeDefinition();
			}

			map[dataType] = converterType;
		}

		return new(map.ToFrozenDictionary());
	}

	/// <summary>Gets an enumerator over the <see cref="Type"/> elements of the collection.</summary>
	/// <returns>The enumerator.</returns>
	public ImmutableArray<TypeWithDefaultConstructor>.Enumerator GetEnumerator() => this.Map.Values.GetEnumerator();

	/// <inheritdoc />
	IEnumerator<TypeWithDefaultConstructor> IEnumerable<TypeWithDefaultConstructor>.GetEnumerator() => ((IReadOnlyList<TypeWithDefaultConstructor>)this.Map.Values).GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TypeWithDefaultConstructor>)this).GetEnumerator();

	/// <summary>
	/// Retrieves a converter type for a given data type.
	/// </summary>
	/// <param name="dataType">The data type.</param>
	/// <param name="converterType">Receives the converter type, if available.</param>
	/// <returns>A value indicating whether the converter type was available.</returns>
	internal bool TryGetConverterType(Type dataType, [NotNullWhen(true), DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type? converterType)
	{
		if (this.Map.TryGetValue(dataType, out TypeWithDefaultConstructor converterTypeWithDefaultCtor))
		{
			converterType = converterTypeWithDefaultCtor;
			return true;
		}

		converterType = default;
		return false;
	}

	/// <summary>
	/// A wrapper around <see cref="Type"/> that ensures a trimmed application will preserve the type's public constructors.
	/// </summary>
	public readonly struct TypeWithDefaultConstructor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypeWithDefaultConstructor"/> struct.
		/// </summary>
		/// <param name="type">The wrapped type.</param>
		public TypeWithDefaultConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
		{
			this.Type = type;
		}

		/// <summary>
		/// Gets the wrapped type.
		/// </summary>
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		public Type Type { get; }

		/// <summary>
		/// Implicitly converts a <see cref="Type"/> to a <see cref="TypeWithDefaultConstructor"/>.
		/// </summary>
		/// <remarks>This operator enables seamless conversion from a <see cref="Type"/> to a <see cref="TypeWithDefaultConstructor"/>.</remarks>
		/// <param name="type">The type to convert.</param>
		public static implicit operator TypeWithDefaultConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) => new(type);

		/// <summary>
		/// Implicitly converts a <see cref="TypeWithDefaultConstructor"/> instance to its underlying <see cref="Type"/>.
		/// </summary>
		/// <param name="type">The <see cref="TypeWithDefaultConstructor"/> instance to convert.</param>
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		public static implicit operator Type(TypeWithDefaultConstructor type) => type.Type;
	}
}
