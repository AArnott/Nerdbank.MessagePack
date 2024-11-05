﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

using System.Collections.Frozen;
using System.Text;

namespace Nerdbank.MessagePack;

/// <summary>
/// A <see cref="TypeShapeVisitor"/> that produces <see cref="MessagePackConverter{T}"/> instances for each type shape it visits.
/// </summary>
/// <param name="owner">The serializer that created this instance. Usable for obtaining settings that may influence the generated converter.</param>
internal class StandardVisitor(MessagePackSerializer owner) : TypeShapeVisitor, ITypeShapeFunc
{
	private readonly TypeDictionary converters = new();

	/// <summary>
	/// Gets a collection of the converters that have been generated by this visitor.
	/// </summary>
	internal IReadOnlyDictionary<Type, object?> GeneratedConverters => this.converters;

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => this.GetConverter(typeShape, state);

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		SubTypes? unionTypes = this.DiscoverUnionTypes(objectShape);

		IConstructorShape? ctorShape = objectShape.GetConstructor();

		bool? keyAttributesPresent = null;
		List<SerializableProperty<T>>? serializable = null;
		List<DeserializableProperty<T>>? deserializable = null;
		List<(string Name, PropertyAccessors<T> Accessors)?>? propertyAccessors = null;
		foreach (IPropertyShape property in objectShape.GetProperties())
		{
			KeyAttribute? keyAttribute = (KeyAttribute?)property.AttributeProvider?.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault();
			if (keyAttributesPresent is null)
			{
				keyAttributesPresent = keyAttribute is not null;
			}
			else if (keyAttributesPresent != keyAttribute is not null)
			{
				throw new InvalidOperationException($"The type {objectShape.Type.FullName} has fields/properties that are candidates for serialization but are inconsistently attributed with {nameof(KeyAttribute)}.");
			}

			PropertyAccessors<T> accessors = (PropertyAccessors<T>)property.Accept(this)!;
			if (keyAttribute is not null)
			{
				propertyAccessors ??= new();
				while (propertyAccessors.Count <= keyAttribute.Index)
				{
					propertyAccessors.Add(null);
				}

				propertyAccessors[keyAttribute.Index] = (property.Name, accessors);
			}
			else
			{
				serializable ??= new();
				deserializable ??= new();

				CodeGenHelpers.GetEncodedStringBytes(property.Name, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
				if (accessors.MsgPackWriters is var (serialize, serializeAsync))
				{
					serializable.Add(new(msgpackEncoded, serialize, serializeAsync));
				}

				if (accessors.MsgPackReaders is var (deserialize, deserializeAsync))
				{
					deserializable.Add(new(utf8Bytes, deserialize, deserializeAsync));
				}
			}
		}

		MessagePackConverter<T> converter;
		if (propertyAccessors is not null)
		{
			ArrayConstructorVisitorInputs<T> inputs = new(propertyAccessors);
			converter = ctorShape is not null
				? (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!
				: new ObjectArrayConverter<T>(inputs.GetJustAccessors(), null);
		}
		else
		{
			SpanDictionary<byte, DeserializableProperty<T>>? propertyReaders = deserializable?
				.ToSpanDictionary(
					p => p.PropertyNameUtf8,
					ByteSpanEqualityComparer.Ordinal);

			MapSerializableProperties<T> serializableMap = new(serializable);
			MapDeserializableProperties<T> deserializableMap = new(propertyReaders);
			MapConstructorVisitorInputs<T> inputs = new(serializableMap, deserializableMap);
			if (ctorShape is not null)
			{
				converter = (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!;
			}
			else
			{
				Func<T>? ctor = typeof(T) == typeof(object) ? (Func<T>)(object)new Func<object>(() => new object()) : null;
				converter = new ObjectMapConverter<T>(serializableMap, deserializableMap, ctor);
			}
		}

		return unionTypes is null ? converter : new SubTypeUnionConverter<T>(unionTypes, converter);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		MessagePackConverter<TPropertyType> converter = this.GetConverter(propertyShape.PropertyType);

		(SerializeProperty<TDeclaringType>, SerializePropertyAsync<TDeclaringType>)? msgpackWriters = null;
		if (propertyShape.HasGetter)
		{
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			SerializeProperty<TDeclaringType> serialize = (ref TDeclaringType container, ref MessagePackWriter writer, SerializationContext context) =>
			{
				TPropertyType? value = getter(ref container);
				converter.Serialize(ref writer, ref value, context);
			};
			SerializePropertyAsync<TDeclaringType> serializeAsync = (TDeclaringType container, MessagePackAsyncWriter writer, SerializationContext context, CancellationToken cancellationToken)
				=> converter.SerializeAsync(writer, getter(ref container), context, cancellationToken);
			msgpackWriters = (serialize, serializeAsync);
		}

		(DeserializeProperty<TDeclaringType>, DeserializePropertyAsync<TDeclaringType>)? msgpackReaders = null;
		if (propertyShape.HasSetter)
		{
			Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context) => setter(ref container, converter.Deserialize(ref reader, context)!);
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken) =>
			{
				setter(ref container, (await converter.DeserializeAsync(reader, context, cancellationToken).ConfigureAwait(false))!);
				return container;
			};
			msgpackReaders = (deserialize, deserializeAsync);
		}
		else if (propertyShape.HasGetter && converter is IDeserializeInto<TPropertyType> inflater)
		{
			// The property has no setter, but it has a getter and the property type is a collection.
			// So we'll assume the declaring type initializes the collection in its constructor,
			// and we'll just deserialize into it.
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context) =>
			{
				if (reader.TryReadNil())
				{
					// No elements to read. A null collection in msgpack doesn't let us set the collection to null, so just return.
					return;
				}

				TPropertyType collection = getter(ref container);
				inflater.DeserializeInto(ref reader, ref collection, context);
			};
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken) =>
			{
				if (!await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
				{
					TPropertyType collection = propertyShape.GetGetter()(ref container);
					await inflater.DeserializeIntoAsync(reader, collection, context, cancellationToken).ConfigureAwait(false);
				}

				return container;
			};
			msgpackReaders = (deserialize, deserializeAsync);
		}

		return new PropertyAccessors<TDeclaringType>(msgpackWriters, msgpackReaders);
	}

	/// <inheritdoc/>
	public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null)
	{
		switch (state)
		{
			case MapConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.ParameterCount == 0)
					{
						return new ObjectMapConverter<TDeclaringType>(inputs.Serializers, inputs.Deserializers, constructorShape.GetDefaultConstructor());
					}

					SpanDictionary<byte, DeserializableProperty<TArgumentState>> parameters = constructorShape.GetParameters()
						.SelectMany<IConstructorParameterShape, (string Name, DeserializableProperty<TArgumentState> Deserialize)>(p =>
						{
							var prop = (DeserializableProperty<TArgumentState>)p.Accept(this)!;
							if (char.IsLower(p.Name[0]))
							{
								// Also allow a PascalCase match, since the property will probably have serialized that way.
								Span<char> pascalCased = stackalloc char[p.Name.Length];
								p.Name.AsSpan().CopyTo(pascalCased);
								pascalCased[0] = char.ToUpperInvariant(pascalCased[0]);
								return [(p.Name, prop), (new string(pascalCased), prop)];
							}
							else
							{
								// The parameter name isn't camelCased, so expect it to match the property name exactly.
								return [(p.Name, prop)];
							}
						}).ToSpanDictionary(
							p => Encoding.UTF8.GetBytes(p.Name),
							p => p.Deserialize,
							ByteSpanEqualityComparer.Ordinal);

					return new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						inputs.Serializers,
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						new MapDeserializableProperties<TArgumentState>(parameters));
				}

			case ArrayConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.ParameterCount == 0)
					{
						return new ObjectArrayConverter<TDeclaringType>(inputs.GetJustAccessors(), constructorShape.GetDefaultConstructor());
					}

					Dictionary<string, int> propertyIndexesByName = new(StringComparer.Ordinal);
					for (int i = 0; i < inputs.Properties.Count; i++)
					{
						if (inputs.Properties[i] is { } property)
						{
							propertyIndexesByName[property.Name] = i;
						}
					}

					DeserializableProperty<TArgumentState>?[] parameters = new DeserializableProperty<TArgumentState>?[inputs.Properties.Count];
					foreach (IConstructorParameterShape parameter in constructorShape.GetParameters())
					{
						int index = propertyIndexesByName[parameter.Name];
						parameters[index] = (DeserializableProperty<TArgumentState>)parameter.Accept(this)!;
					}

					return new ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						inputs.GetJustAccessors(),
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						parameters);
				}

			default:
				throw new NotSupportedException("Unsupported state.");
		}
	}

	/// <inheritdoc/>
	public override object? VisitConstructorParameter<TArgumentState, TParameterType>(IConstructorParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		MessagePackConverter<TParameterType> converter = owner.GetOrAddConverter(parameterShape.ParameterType);

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();
		return new DeserializableProperty<TArgumentState>(
			StringEncoding.UTF8.GetBytes(parameterShape.Name),
			(ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) => setter(ref state, converter.Deserialize(ref reader, context)!),
			async (TArgumentState state, MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken) =>
			{
				setter(ref state, (await converter.DeserializeAsync(reader, context, cancellationToken).ConfigureAwait(false))!);
				return state;
			});
	}

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null) => new NullableConverter<T>(this.GetConverter(nullableShape.ElementType));

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
	{
		// Serialization functions.
		MessagePackConverter<TKey> keyConverter = this.GetConverter(dictionaryShape.KeyType);
		MessagePackConverter<TValue> valueConverter = this.GetConverter(dictionaryShape.ValueType);
		Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable = dictionaryShape.GetGetDictionary();

		// Deserialization functions.
		return dictionaryShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => new DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter),
			CollectionConstructionStrategy.Mutable => new MutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetAddKeyValuePair(), dictionaryShape.GetDefaultConstructor()),
			CollectionConstructionStrategy.Span => new ImmutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetSpanConstructor()),
			CollectionConstructionStrategy.Enumerable => new EnumerableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetEnumerableConstructor()),
			_ => throw new NotSupportedException($"Unrecognized dictionary pattern: {typeof(TDictionary).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
	{
		MessagePackConverter<TElement> elementConverter = this.GetConverter(enumerableShape.ElementType);

		if (enumerableShape.Type.IsArray)
		{
			return enumerableShape.Rank > 1
				? owner.MultiDimensionalArrayFormat switch
				{
					MultiDimensionalArrayFormat.Nested => new ArrayWithNestedDimensionsConverter<TEnumerable, TElement>(elementConverter, enumerableShape.Rank),
					MultiDimensionalArrayFormat.Flat => new ArrayWithFlattenedDimensionsConverter<TEnumerable, TElement>(elementConverter),
					_ => throw new NotSupportedException(),
				}
				: new ArrayConverter<TElement>(elementConverter);
		}

		return enumerableShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => new EnumerableConverter<TEnumerable, TElement>(enumerableShape.GetGetEnumerable(), elementConverter),
			CollectionConstructionStrategy.Mutable => new MutableEnumerableConverter<TEnumerable, TElement>(enumerableShape.GetGetEnumerable(), elementConverter, enumerableShape.GetAddElement(), enumerableShape.GetDefaultConstructor()),
			CollectionConstructionStrategy.Span => new SpanEnumerableConverter<TEnumerable, TElement>(enumerableShape.GetGetEnumerable(), elementConverter, enumerableShape.GetSpanConstructor()),
			CollectionConstructionStrategy.Enumerable => new EnumerableEnumerableConverter<TEnumerable, TElement>(enumerableShape.GetGetEnumerable(), elementConverter, enumerableShape.GetEnumerableConstructor()),
			_ => throw new NotSupportedException($"Unrecognized enumerable pattern: {typeof(TEnumerable).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> new EnumAsOrdinalConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType));

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected MessagePackConverter<T> GetConverter<T>(ITypeShape<T> shape, object? state = null)
	{
		if (owner.TryGetConverter(out MessagePackConverter<T>? converter))
		{
			return converter;
		}

		return this.converters.GetOrAdd<MessagePackConverter<T>>(shape, this, box => new DelayedConverter<T>(box));
	}

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected IMessagePackConverter GetConverter(ITypeShape shape, object? state = null)
	{
		ITypeShapeFunc self = this;
		return (IMessagePackConverter)shape.Invoke(this, state)!;
	}

	/// <summary>
	/// Returns a dictionary of <see cref="MessagePackConverter{T}"/> objects for each subtype, keyed by their alias.
	/// </summary>
	/// <param name="objectShape">The shape of the data type that may define derived types that are also allowed for serialization.</param>
	/// <returns>A dictionary of <see cref="MessagePackConverter{T}"/> objets, keyed by the alias by which they will be identified in the data stream.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <paramref name="objectShape"/> has any <see cref="KnownSubTypeAttribute"/> that violates rules.</exception>
	private SubTypes? DiscoverUnionTypes(IObjectTypeShape objectShape)
	{
		KnownSubTypeAttribute[]? unionAttributes = objectShape.AttributeProvider?.GetCustomAttributes(typeof(KnownSubTypeAttribute), false).Cast<KnownSubTypeAttribute>().ToArray();
		if (unionAttributes is null or { Length: 0 })
		{
			return null;
		}

		Dictionary<int, IMessagePackConverter> deserializerData = new();
		Dictionary<Type, (int Alias, IMessagePackConverter Converter)> serializerData = new();
		foreach (KnownSubTypeAttribute unionAttribute in unionAttributes)
		{
			ITypeShape? subtypeShape = objectShape.Provider.GetShape(unionAttribute.SubType);
			if (subtypeShape is null)
			{
				throw new InvalidOperationException($"The type {objectShape.Type.FullName} has a union attribute that references a type that is not known to the serializer: {unionAttribute.SubType.FullName}.");
			}

			if (!objectShape.Type.IsAssignableFrom(subtypeShape.Type))
			{
				throw new InvalidOperationException($"The type {objectShape.Type.FullName} has a {nameof(KnownSubTypeAttribute)} that references non-derived {unionAttribute.SubType.FullName}.");
			}

			IMessagePackConverter converter = this.GetConverter(subtypeShape);
			if (!deserializerData.TryAdd(unionAttribute.Alias, converter))
			{
				throw new InvalidOperationException($"The type {objectShape.Type.FullName} has more than one {nameof(KnownSubTypeAttribute)} with a duplicate alias: {unionAttribute.Alias}.");
			}

			if (!serializerData.TryAdd(subtypeShape.Type, (unionAttribute.Alias, converter)))
			{
				throw new InvalidOperationException($"The type {objectShape.Type.FullName} has more than one subtype with a duplicate alias: {unionAttribute.Alias}.");
			}
		}

		return new SubTypes
		{
			Deserializers = deserializerData.ToFrozenDictionary(),
			Serializers = serializerData.ToFrozenDictionary(),
		};
	}
}
