// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft;
using PolyType.Utilities;

namespace ShapeShift;

/// <summary>
/// A <see cref="TypeShapeVisitor"/> that produces <see cref="Converter{T}"/> instances for each type shape it visits.
/// </summary>
internal class StandardVisitor : TypeShapeVisitor, ITypeShapeFunc
{
	private static readonly InterningStringConverter InterningStringConverter = new();
	private readonly ConverterCache owner;
	private readonly TypeGenerationContext context;
	private readonly Converter<string>? referencePreservingInterningStringConverter;

	/// <summary>
	/// Initializes a new instance of the <see cref="StandardVisitor"/> class.
	/// </summary>
	/// <param name="owner">The serializer that created this instance. Usable for obtaining settings that may influence the generated converter.</param>
	/// <param name="context">Context for a generation of a particular data model.</param>
	internal StandardVisitor(ConverterCache owner, TypeGenerationContext context)
	{
		this.owner = owner;
		this.context = context;
		this.OutwardVisitor = this;

		if (owner.ReferencePreservingManager is not null)
		{
			this.referencePreservingInterningStringConverter = owner.ReferencePreservingManager.WrapWithReferencePreservingConverter(InterningStringConverter);
		}
	}

	/// <summary>
	/// Gets the formatter used to encode primitive values.
	/// </summary>
	internal Formatter Formatter => this.owner.Formatter;

	/// <summary>
	/// Gets the deformatter used to decode primitive values.
	/// </summary>
	internal Deformatter Deformatter => this.owner.Deformatter;

	/// <summary>
	/// Gets or sets the visitor that will be used to generate converters for new types that are encountered.
	/// </summary>
	/// <value>Defaults to <see langword="this" />.</value>
	/// <remarks>
	/// This may be changed to a wrapping visitor implementation to implement features such as reference preservation.
	/// </remarks>
	internal ITypeShapeVisitor OutwardVisitor { get; set; }

	/// <summary>
	/// Gets the converter cache that owns this visitor.
	/// </summary>
	protected ConverterCache ConverterCache => this.owner;

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
	{
		// Check if the type has a custom converter.
		if (this.owner.TryGetUserDefinedConverter<T>(out Converter<T>? userDefinedConverter))
		{
			return userDefinedConverter;
		}

		if (this.owner.InternStrings && typeof(T) == typeof(string))
		{
			return this.owner.PreserveReferences ? this.referencePreservingInterningStringConverter : InterningStringConverter;
		}

		// Check if the type has a built-in converter.
		if (this.TryGetPrimitiveConverter(out Converter<T>? defaultConverter))
		{
			if (this.owner.PreserveReferences)
			{
				Verify.Operation(this.owner.ReferencePreservingManager is not null, "This serializer does not support reference preservation.");
				defaultConverter = this.owner.ReferencePreservingManager.WrapWithReferencePreservingConverter(defaultConverter);
			}

			return defaultConverter;
		}

		// Otherwise, build a converter using the visitor.
		return typeShape.Accept(this.OutwardVisitor);
	}

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (this.GetCustomConverter(objectShape) is Converter<T> customConverter)
		{
			return customConverter;
		}

		SubTypes? unionTypes = this.DiscoverUnionTypes(objectShape);

		IConstructorShape? ctorShape = objectShape.Constructor;

		Dictionary<string, IConstructorParameterShape>? ctorParametersByName = null;
		if (ctorShape is not null)
		{
			ctorParametersByName = new(StringComparer.Ordinal);
			foreach (IConstructorParameterShape ctorParameter in ctorShape.Parameters)
			{
				// Keep the one with the Kind that we prefer.
				if (ctorParameter.Kind == ConstructorParameterKind.ConstructorParameter)
				{
					ctorParametersByName[ctorParameter.Name] = ctorParameter;
				}
				else if (!ctorParametersByName.ContainsKey(ctorParameter.Name))
				{
					ctorParametersByName.Add(ctorParameter.Name, ctorParameter);
				}
			}
		}

		List<SerializableProperty<T>>? serializable = null;
		List<DeserializableProperty<T>>? deserializable = null;
		List<(string Name, PropertyAccessors<T> Accessors)?>? propertyAccessors = null;
		foreach (IPropertyShape property in objectShape.Properties)
		{
			string propertyName = this.owner.GetSerializedPropertyName(property.Name, property.AttributeProvider);

			IConstructorParameterShape? matchingConstructorParameter = null;
			ctorParametersByName?.TryGetValue(property.Name, out matchingConstructorParameter);

			if (property.Accept(this, matchingConstructorParameter) is PropertyAccessors<T> accessors)
			{
				if (property.AttributeProvider?.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault() is KeyAttribute keyAttribute)
				{
					propertyAccessors ??= new();
					while (propertyAccessors.Count <= keyAttribute.Index)
					{
						propertyAccessors.Add(null);
					}

					propertyAccessors[keyAttribute.Index] = (propertyName, accessors);
				}
				else
				{
					serializable ??= new();
					deserializable ??= new();

					this.Formatter.GetEncodedStringBytes(propertyName, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
					if (accessors.MsgPackWriters is var (serialize, serializeAsync))
					{
						serializable.Add(new(propertyName, msgpackEncoded, serialize, serializeAsync, accessors.Converter, accessors.ShouldSerialize, property));
					}

					if (accessors.MsgPackReaders is var (deserialize, deserializeAsync))
					{
						deserializable.Add(new(property.Name, utf8Bytes, deserialize, deserializeAsync, accessors.Converter));
					}
				}
			}
		}

		Converter<T> converter;
		if (propertyAccessors is not null)
		{
			if (serializable is { Count: > 0 })
			{
				// Members with and without KeyAttribute have been detected as intended for serialization. These two worlds are incompatible.
				throw new SerializationException($"The type {objectShape.Type.FullName} has fields/properties that are candidates for serialization but are inconsistently attributed with {nameof(KeyAttribute)}.\nMembers with the attribute: {string.Join(", ", propertyAccessors.Where(a => a is not null).Select(a => a!.Value.Name))}\nMembers without the attribute: {string.Join(", ", serializable.Select(p => p.Name))}");
			}

			ArrayConstructorVisitorInputs<T> inputs = new(propertyAccessors);
			converter = ctorShape is not null
				? (Converter<T>)ctorShape.Accept(this, inputs)!
				: new ObjectArrayConverter<T>(inputs.GetJustAccessors(), null, this.owner.SerializeDefaultValues);
		}
		else
		{
			SpanDictionary<byte, DeserializableProperty<T>>? propertyReaders = deserializable?
				.ToSpanDictionary(
					p => p.PropertyNameUtf8,
					ByteSpanEqualityComparer.Ordinal);

			MapSerializableProperties<T> serializableMap = new(serializable?.ToArray());
			MapDeserializableProperties<T> deserializableMap = new(propertyReaders);
			if (ctorShape is not null)
			{
				MapConstructorVisitorInputs<T> inputs = new(serializableMap, deserializableMap, ctorParametersByName!);
				converter = (Converter<T>)ctorShape.Accept(this, inputs)!;
			}
			else
			{
				Func<T>? ctor = typeof(T) == typeof(object) ? (Func<T>)(object)new Func<object>(() => new object()) : null;
				converter = this.CreateObjectMapConverter(serializableMap, deserializableMap, ctor, this.owner.SerializeDefaultValues);
			}
		}

		return unionTypes is null ? converter : this.CreateSubTypeUnionConverter<T>(unionTypes, converter);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		IConstructorParameterShape? constructorParameterShape = (IConstructorParameterShape?)state;

		Converter<TPropertyType> converter = this.GetConverter(propertyShape.PropertyType);

		(SerializeProperty<TDeclaringType>, SerializePropertyAsync<TDeclaringType>)? msgpackWriters = null;
		Func<TDeclaringType, bool>? shouldSerialize = null;
		if (propertyShape.HasGetter)
		{
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			EqualityComparer<TPropertyType> eq = EqualityComparer<TPropertyType>.Default;

			if (this.owner.SerializeDefaultValues != SerializeDefaultValuesPolicy.Always)
			{
				// Test for value-independent flags that would indicate this property must always be serialized.
				bool alwaysSerialize =
					((this.owner.SerializeDefaultValues & SerializeDefaultValuesPolicy.ValueTypes) == SerializeDefaultValuesPolicy.ValueTypes && typeof(TPropertyType).IsValueType) ||
					((this.owner.SerializeDefaultValues & SerializeDefaultValuesPolicy.ReferenceTypes) == SerializeDefaultValuesPolicy.ReferenceTypes && !typeof(TPropertyType).IsValueType) ||
					((this.owner.SerializeDefaultValues & SerializeDefaultValuesPolicy.Required) == SerializeDefaultValuesPolicy.Required && constructorParameterShape is { IsRequired: true });

				if (alwaysSerialize)
				{
					shouldSerialize = static obj => true;
				}
				else
				{
					// The only possibility for serializing the property that remains is that it has a non-default value.
					TPropertyType? defaultValue = default;
					if (constructorParameterShape?.HasDefaultValue is true)
					{
						defaultValue = (TPropertyType?)constructorParameterShape.DefaultValue;
					}
					else if (propertyShape.AttributeProvider?.GetCustomAttributes(typeof(System.ComponentModel.DefaultValueAttribute), true).FirstOrDefault() is System.ComponentModel.DefaultValueAttribute { Value: TPropertyType attributeDefaultValue })
					{
						defaultValue = attributeDefaultValue;
					}

					shouldSerialize = obj => !eq.Equals(getter(ref obj), defaultValue!);
				}
			}

			SerializeProperty<TDeclaringType> serialize = (in TDeclaringType container, ref Writer writer, SerializationContext context) =>
			{
				// Workaround https://github.com/eiriktsarpalis/PolyType/issues/46.
				// We get significantly improved usability in the API if we use the `in` modifier on the Serialize method
				// instead of `ref`. And since serialization should fundamentally be a read-only operation, this *should* be safe.
				TPropertyType? value = getter(ref Unsafe.AsRef(in container));
				converter.Write(ref writer, value, context);
			};
			SerializePropertyAsync<TDeclaringType> serializeAsync = (TDeclaringType container, AsyncWriter writer, SerializationContext context)
				=> converter.WriteAsync(writer, getter(ref container), context);
			msgpackWriters = (serialize, serializeAsync);
		}

		bool suppressIfNoConstructorParameter = true;
		(DeserializeProperty<TDeclaringType>, DeserializePropertyAsync<TDeclaringType>)? msgpackReaders = null;
		if (propertyShape.HasSetter)
		{
			Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref Reader reader, SerializationContext context) => setter(ref container, converter.Read(ref reader, context)!);
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, AsyncReader reader, SerializationContext context) =>
			{
				setter(ref container, (await converter.ReadAsync(reader, context).ConfigureAwait(false))!);
				return container;
			};
			msgpackReaders = (deserialize, deserializeAsync);
			suppressIfNoConstructorParameter = false;
		}
		else if (propertyShape.HasGetter && converter is IDeserializeInto<TPropertyType> inflater)
		{
			// The property has no setter, but it has a getter and the property type is a collection.
			// So we'll assume the declaring type initializes the collection in its constructor,
			// and we'll just deserialize into it.
			suppressIfNoConstructorParameter = false;
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref Reader reader, SerializationContext context) =>
			{
				if (reader.TryReadNull())
				{
					// No elements to read. A null collection in msgpack doesn't let us set the collection to null, so just return.
					return;
				}

				TPropertyType collection = getter(ref container);
				inflater.DeserializeInto(ref reader, ref collection, context);
			};
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, AsyncReader reader, SerializationContext context) =>
			{
				StreamingReader streamingReader = reader.CreateStreamingReader();
				bool isNil;
				while (streamingReader.TryReadNull(out isNil).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}

				if (!isNil)
				{
					TPropertyType collection = propertyShape.GetGetter()(ref container);
					await inflater.DeserializeIntoAsync(reader, collection, context).ConfigureAwait(false);
				}

				return container;
			};
			msgpackReaders = (deserialize, deserializeAsync);
		}

		return suppressIfNoConstructorParameter && constructorParameterShape is null
			? null
			: new PropertyAccessors<TDeclaringType>(msgpackWriters, msgpackReaders, converter, shouldSerialize, propertyShape);
	}

	/// <inheritdoc/>
	public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null)
	{
		switch (state)
		{
			case MapConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.Parameters.Count == 0)
					{
						return new ObjectMapConverter<TDeclaringType>(
							inputs.Serializers,
							inputs.Deserializers,
							constructorShape.GetDefaultConstructor(),
							this.owner.SerializeDefaultValues);
					}

					List<SerializableProperty<TDeclaringType>> propertySerializers = [.. inputs.Serializers.Properties.Span];

					SpanDictionary<byte, DeserializableProperty<TArgumentState>> parameters = inputs.ParametersByName.Values
						.SelectMany<IConstructorParameterShape, (string Name, DeserializableProperty<TArgumentState> Deserialize)>(p =>
						{
							var prop = (DeserializableProperty<TArgumentState>)p.Accept(this)!;

							// Apply camelCase and PascalCase transformations and accept a serialized form that matches either one.
							// If the parameter name is camelCased (as would typically happen in an ordinary constructor),
							// we want it to match msgpack property names serialized in PascalCase (since the C# property will default to serializing that way).
							// If the parameter name is PascalCased (as would typically happen in a record primary constructor),
							// we want it to match camelCase property names in case the user has camelCase name policy applied.
							// Ultimately we would probably do well to just match without case sensitivity, but we don't support that yet.
							string camelCase = NamingPolicy.CamelCase.ConvertName(p.Name);
							string pascalCase = NamingPolicy.PascalCase.ConvertName(p.Name);
							return camelCase != pascalCase
								? [(camelCase, prop), (pascalCase, prop)]
								: [(camelCase, prop)];
						}).ToSpanDictionary(
							p => Encoding.UTF8.GetBytes(p.Name),
							p => p.Deserialize,
							ByteSpanEqualityComparer.Ordinal);

					MapSerializableProperties<TDeclaringType> serializeable = inputs.Serializers with { Properties = propertySerializers.ToArray() };
					return this.CreateObjectMapWithNonDefaultCtorConverter(
						serializeable,
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						new MapDeserializableProperties<TArgumentState>(parameters),
						this.owner.SerializeDefaultValues);
				}

			case ArrayConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.Parameters.Count == 0)
					{
						return new ObjectArrayConverter<TDeclaringType>(inputs.GetJustAccessors(), constructorShape.GetDefaultConstructor(), this.owner.SerializeDefaultValues);
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
					foreach (IConstructorParameterShape parameter in constructorShape.Parameters)
					{
						int index = propertyIndexesByName[parameter.Name];
						parameters[index] = (DeserializableProperty<TArgumentState>)parameter.Accept(this)!;
					}

					return new ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						inputs.GetJustAccessors(),
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						parameters,
						this.owner.SerializeDefaultValues);
				}

			default:
				throw new NotSupportedException("Unsupported state.");
		}
	}

	/// <inheritdoc/>
	public override object? VisitConstructorParameter<TArgumentState, TParameterType>(IConstructorParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		Converter<TParameterType> converter = this.GetConverter(parameterShape.ParameterType);

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();

		return new DeserializableProperty<TArgumentState>(
			parameterShape.Name,
			this.Formatter.Encoding.GetBytes(parameterShape.Name),
			(ref TArgumentState state, ref Reader reader, SerializationContext context) => setter(ref state, converter.Read(ref reader, context)!),
			async (TArgumentState state, AsyncReader reader, SerializationContext context) =>
			{
				setter(ref state, (await converter.ReadAsync(reader, context).ConfigureAwait(false))!);
				return state;
			},
			converter);
	}

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null) => new NullableConverter<T>(this.GetConverter(nullableShape.ElementType));

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
	{
		// Serialization functions.
		Converter<TKey> keyConverter = this.GetConverter(dictionaryShape.KeyType);
		Converter<TValue> valueConverter = this.GetConverter(dictionaryShape.ValueType);
		Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable = dictionaryShape.GetGetDictionary();

		// Deserialization functions.
		return dictionaryShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => this.CreateDictionaryConverter(getReadable, keyConverter, valueConverter),
			CollectionConstructionStrategy.Mutable => this.CreateMutableDictionaryConverter(getReadable, keyConverter, valueConverter, dictionaryShape.GetAddKeyValuePair(), dictionaryShape.GetDefaultConstructor()),
			CollectionConstructionStrategy.Span => this.CreateDictionaryFromSpanConverter(getReadable, keyConverter, valueConverter, dictionaryShape.GetSpanConstructor()),
			CollectionConstructionStrategy.Enumerable => this.CreateDictionaryFromEnumerableConverter(getReadable, keyConverter, valueConverter, dictionaryShape.GetEnumerableConstructor()),
			_ => throw new NotSupportedException($"Unrecognized dictionary pattern: {typeof(TDictionary).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
	{
		Converter<TElement> elementConverter = this.GetConverter(enumerableShape.ElementType);

		if (enumerableShape.Type.IsArray)
		{
			Converter<TEnumerable>? converter;
			if (enumerableShape.Rank > 1)
			{
#if NET
				return this.owner.MultiDimensionalArrayFormat switch
				{
					MultiDimensionalArrayFormat.Nested => this.CreateArrayWithNestedDimensionsConverter<TEnumerable, TElement>(elementConverter, enumerableShape.Rank),
					MultiDimensionalArrayFormat.Flat => this.CreateArrayWithFlattenedDimensionsConverter<TEnumerable, TElement>(elementConverter),
					_ => throw new NotSupportedException(),
				};
#else
				throw PolyfillExtensions.ThrowNotSupportedOnNETFramework();
#endif
			}
			else if (!this.owner.DisableHardwareAcceleration &&
				enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Span &&
				this.TryGetHardwareAcceleratedConverter<TEnumerable, TElement>(out converter))
			{
				return converter;
			}
			else if (enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Span &&
				this.TryGetArrayOfPrimitivesConverter(enumerableShape.GetGetEnumerable(), enumerableShape.GetSpanConstructor(), out converter))
			{
				return converter;
			}
			else
			{
				return this.CreateArrayConverter(elementConverter);
			}
		}

		Func<TEnumerable, IEnumerable<TElement>> getEnumerable = enumerableShape.GetGetEnumerable();
		return enumerableShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => this.CreateEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter),
			CollectionConstructionStrategy.Mutable => this.CreateMutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetAddElement(), enumerableShape.GetDefaultConstructor()),
			CollectionConstructionStrategy.Span when !this.owner.DisableHardwareAcceleration && this.TryGetHardwareAcceleratedConverter<TEnumerable, TElement>(out Converter<TEnumerable>? converter) => converter,
			CollectionConstructionStrategy.Span when this.TryGetArrayOfPrimitivesConverter(getEnumerable, enumerableShape.GetSpanConstructor(), out Converter<TEnumerable>? converter) => converter,
			CollectionConstructionStrategy.Span => this.CreateSpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetSpanConstructor()),
			CollectionConstructionStrategy.Enumerable => this.CreateEnumerableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetEnumerableConstructor()),
			_ => throw new NotSupportedException($"Unrecognized enumerable pattern: {typeof(TEnumerable).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> this.owner.SerializeEnumValuesByName
			? new EnumAsStringConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType), this.Formatter)
			: new EnumAsOrdinalConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType));

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
		=> new SurrogateConverter<T, TSurrogate>(surrogateShape, this.GetConverter(surrogateShape.SurrogateType, state));

	protected virtual Converter<T> CreateObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? ctor, SerializeDefaultValuesPolicy defaultValuesPolicy)
		=> new ObjectMapConverter<T>(serializable, deserializable, ctor, defaultValuesPolicy);

	protected virtual Converter<TDeclaringType> CreateObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters, SerializeDefaultValuesPolicy defaultValuesPolicy)
		=> new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(serializable, argStateCtor, ctor, parameters, defaultValuesPolicy);

	/// <summary>
	/// Creates a converter for a class and a set of derived types that should be serialized with full fidelity.
	/// </summary>
	/// <typeparam name="T">The type of the base type.</typeparam>
	/// <param name="unionTypes">A mapping of aliases and derived types.</param>
	/// <param name="baseConverter">The converter to use when the runtime type is the base type itself.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateSubTypeUnionConverter<T>(SubTypes unionTypes, Converter<T> baseConverter)
		=> new SubTypeUnionConverter<T>(unionTypes, baseConverter);

	/// <summary>
	/// Looks up a built-in converter for a given type, if available.
	/// </summary>
	/// <typeparam name="T">The data type.</typeparam>
	/// <param name="converter">Receives the converter for the data type, if available.</param>
	/// <returns>A value indicating whether a built-in converter was available.</returns>
	protected virtual bool TryGetPrimitiveConverter<T>([NotNullWhen(true)] out Converter<T>? converter) => PrimitiveConverterLookup.TryGetPrimitiveConverter(out converter);

	/// <summary>
	/// Creates a converter for a dictionary that can only be serialized.
	/// </summary>
	/// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
	/// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
	/// <param name="getReadable">A function that retrieves an readable interface from the dictionary type, allowing serialization.</param>
	/// <param name="keyConverter">The converter to use when serializing keys.</param>
	/// <param name="valueConverter">The converter to use when serializing values.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateDictionaryConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, Converter<TKey> keyConverter, Converter<TValue> valueConverter)
		where TKey : notnull => new DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter);

	/// <summary>
	/// Creates a converter for a mutable dictionary.
	/// </summary>
	/// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
	/// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
	/// <param name="getReadable">A function that retrieves an readable interface from the dictionary type, allowing serialization.</param>
	/// <param name="keyConverter">The converter to use when serializing keys.</param>
	/// <param name="valueConverter">The converter to use when serializing values.</param>
	/// <param name="addKeyValuePair">The function that can add a key=value pair to the dictionary.</param>
	/// <param name="defaultConstructor">A factory for an empty dictionary, for use when deserializing.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateMutableDictionaryConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, Converter<TKey> keyConverter, Converter<TValue> valueConverter, Setter<TDictionary, KeyValuePair<TKey, TValue>> addKeyValuePair, Func<TDictionary> defaultConstructor)
		where TKey : notnull => new MutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, addKeyValuePair, defaultConstructor);

	/// <summary>
	/// Creates a converter for a dictionary that can be constructed from a span of pairs.
	/// </summary>
	/// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
	/// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
	/// <param name="getReadable">A function that retrieves an readable interface from the dictionary type, allowing serialization.</param>
	/// <param name="keyConverter">The converter to use when serializing keys.</param>
	/// <param name="valueConverter">The converter to use when serializing values.</param>
	/// <param name="spanConstructor">A factory for the dictionary that is initially populated with a list of pairs.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateDictionaryFromSpanConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, Converter<TKey> keyConverter, Converter<TValue> valueConverter, SpanConstructor<KeyValuePair<TKey, TValue>, TDictionary> spanConstructor)
		where TKey : notnull => new ImmutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, spanConstructor);

	/// <summary>
	/// Creates a converter for a dictionary that can be constructed from an enumerable of pairs.
	/// </summary>
	/// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
	/// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
	/// <param name="getReadable">A function that retrieves an readable interface from the dictionary type, allowing serialization.</param>
	/// <param name="keyConverter">The converter to use when serializing keys.</param>
	/// <param name="valueConverter">The converter to use when serializing values.</param>
	/// <param name="enumerableConstructor">A factory for the dictionary that is initially populated from an enumeration of pairs.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateDictionaryFromEnumerableConverter<TDictionary, TKey, TValue>(Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable, Converter<TKey> keyConverter, Converter<TValue> valueConverter, Func<IEnumerable<KeyValuePair<TKey, TValue>>, TDictionary> enumerableConstructor)
		where TKey : notnull => new EnumerableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, enumerableConstructor);

	/// <summary>
	/// Creates a converter for an array.
	/// </summary>
	/// <typeparam name="TElement">The type of element stored in the array.</typeparam>
	/// <param name="elementConverter">The converter to use for each array element.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateArrayConverter<TElement>(Converter<TElement> elementConverter)
		=> new ArrayConverter<TElement>(elementConverter);

#if NET
	/// <summary>
	/// Creates a converter for an array with nested dimensions (i.e. when <see cref="SerializerBase.MultiDimensionalArrayFormat"/>
	/// is set to <see cref="MultiDimensionalArrayFormat.Nested"/>.
	/// </summary>
	/// <typeparam name="TArray">The type of the array.</typeparam>
	/// <typeparam name="TElement">The type of array elements.</typeparam>
	/// <param name="elementConverter">The converter to use for each array element.</param>
	/// <param name="rank">The array rank.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateArrayWithNestedDimensionsConverter<TArray, TElement>(Converter<TElement> elementConverter, int rank)
		=> new ArrayWithNestedDimensionsConverter<TArray, TElement>(elementConverter, rank);

	/// <summary>
	/// Creates a converter for an array with flattened dimensions (i.e. when <see cref="SerializerBase.MultiDimensionalArrayFormat"/>
	/// is set to <see cref="MultiDimensionalArrayFormat.Flat"/>.
	/// </summary>
	/// <typeparam name="TArray">The type of the array.</typeparam>
	/// <typeparam name="TElement">The type of array elements.</typeparam>
	/// <param name="elementConverter">The converter to use for each array element.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateArrayWithFlattenedDimensionsConverter<TArray, TElement>(Converter<TElement> elementConverter)
		=> new ArrayWithFlattenedDimensionsConverter<TArray, TElement>(elementConverter);
#endif

	/// <summary>
	/// Gets a converter that is optimized for arrays of primitive values, if available.
	/// </summary>
	/// <typeparam name="TArray">The type of the array.</typeparam>
	/// <typeparam name="TElement">The type of array elements.</typeparam>
	/// <param name="getEnumerable">A function that retrieves a readable object for serializing.</param>
	/// <param name="constructor">The factory for the array for deserializing.</param>
	/// <param name="converter">Receives the converter, if available.</param>
	/// <returns><see langword="true" /> if an optimized converter is available; <see langword="false" /> otherwise.</returns>
	protected virtual bool TryGetArrayOfPrimitivesConverter<TArray, TElement>(Func<TArray, IEnumerable<TElement>> getEnumerable, SpanConstructor<TElement, TArray> constructor, [NotNullWhen(true)] out Converter<TArray>? converter)
	{
		converter = null;
		return false;
	}

	/// <summary>
	/// Gets a converter that is hardware-accelerated for arrays of primitive values, if available.
	/// </summary>
	/// <typeparam name="TArray">The type of the array.</typeparam>
	/// <typeparam name="TElement">The type of array elements.</typeparam>
	/// <param name="converter">Receives the converter, if available.</param>
	/// <returns><see langword="true" /> if an optimized converter is available; <see langword="false" /> otherwise.</returns>
	protected virtual bool TryGetHardwareAcceleratedConverter<TArray, TElement>(out Converter<TArray>? converter)
	{
		converter = null;
		return false;
	}

	/// <summary>
	/// Creates a converter for an enumerable (serialization only).
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of enumerated element.</typeparam>
	/// <param name="getEnumerable">A function that retrieves the enumerable from a runtime object.</param>
	/// <param name="elementConverter">The converter to use for each element.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter)
		=> new EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter);

	/// <summary>
	/// Creates a converter for a mutable enumerable.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of enumerated element.</typeparam>
	/// <param name="getEnumerable">A function that retrieves the enumerable from a runtime object.</param>
	/// <param name="elementConverter">The converter to use for each element.</param>
	/// <param name="addElement">The function to add an element to the collection during deserialization.</param>
	/// <param name="defaultConstructor">The factory for constructing the empty collection.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateMutableEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, Setter<TEnumerable, TElement> addElement, Func<TEnumerable> defaultConstructor)
		=> new MutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, addElement, defaultConstructor);

	/// <summary>
	/// Creates a converter for an enumerable that can be constructed from a span of elements.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of enumerated element.</typeparam>
	/// <param name="getEnumerable">A function that retrieves the enumerable from a runtime object.</param>
	/// <param name="elementConverter">The converter to use for each element.</param>
	/// <param name="spanConstructor">The factory that can create the collection from a span of elements.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateSpanEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, SpanConstructor<TElement, TEnumerable> spanConstructor)
		=> new SpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, spanConstructor);

	/// <summary>
	/// Creates a converter for an enumerable that can be constructed from an enumerable of elements.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of enumerated element.</typeparam>
	/// <param name="getEnumerable">A function that retrieves the enumerable from a runtime object.</param>
	/// <param name="elementConverter">The converter to use for each element.</param>
	/// <param name="enumerableConstructor">The factory that can create the collection from an enumeration of elements.</param>
	/// <returns>The converter.</returns>
	protected virtual Converter CreateEnumerableEnumerableConverter<TEnumerable, TElement>(Func<TEnumerable, IEnumerable<TElement>> getEnumerable, Converter<TElement> elementConverter, Func<IEnumerable<TElement>, TEnumerable> enumerableConstructor)
		=> new EnumerableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableConstructor);

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected Converter<T> GetConverter<T>(ITypeShape<T> shape, object? state = null)
	{
		return (Converter<T>)this.context.GetOrAdd(shape, state)!;
	}

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected Converter GetConverter(ITypeShape shape, object? state = null) => (Converter)shape.Invoke(this, state)!;

	/// <summary>
	/// Returns a dictionary of <see cref="Converter{T}"/> objects for each subtype, keyed by their alias.
	/// </summary>
	/// <param name="objectShape">The shape of the data type that may define derived types that are also allowed for serialization.</param>
	/// <returns>A dictionary of <see cref="Converter{T}"/> objects, keyed by the alias by which they will be identified in the data stream.</returns>
	/// <exception cref="InvalidOperationException">Thrown if <paramref name="objectShape"/> has any <see cref="KnownSubTypeAttribute"/> that violates rules.</exception>
	private SubTypes? DiscoverUnionTypes(IObjectTypeShape objectShape)
	{
		IReadOnlyDictionary<SubTypeAlias, ITypeShape>? mapping;
		if (!this.owner.TryGetDynamicSubTypes(objectShape.Type, out mapping))
		{
			KnownSubTypeAttribute[]? unionAttributes = objectShape.AttributeProvider?.GetCustomAttributes(typeof(KnownSubTypeAttribute), false).Cast<KnownSubTypeAttribute>().ToArray();
			if (unionAttributes is null or { Length: 0 })
			{
				return null;
			}

			Dictionary<SubTypeAlias, ITypeShape> mutableMapping = new();
			foreach (KnownSubTypeAttribute unionAttribute in unionAttributes)
			{
				ITypeShape subtypeShape = unionAttribute.Shape ?? objectShape.Provider.GetShapeOrThrow(unionAttribute.SubType);
				Verify.Operation(objectShape.Type.IsAssignableFrom(subtypeShape.Type), $"The type {objectShape.Type.FullName} has a {KnownSubTypeAttribute.TypeName} that references non-derived {subtypeShape.Type.FullName}.");
				Verify.Operation(mutableMapping.TryAdd(unionAttribute.Alias, subtypeShape), $"The type {objectShape.Type.FullName} has more than one {KnownSubTypeAttribute.TypeName} with a duplicate alias: {unionAttribute.Alias}.");
			}

			mapping = mutableMapping;
		}

		Dictionary<int, Converter> deserializeByIntData = new();
		Dictionary<ReadOnlyMemory<byte>, Converter> deserializeByUtf8Data = new();
		Dictionary<Type, (FormattedSubTypeAlias Alias, Converter Converter, ITypeShape Shape)> serializerData = new();
		foreach (KeyValuePair<SubTypeAlias, ITypeShape> pair in mapping)
		{
			FormattedSubTypeAlias alias = pair.Key.Format(this.Formatter);
			ITypeShape shape = pair.Value;

			Converter converter = this.GetConverter(shape);

			// We don't want a reference-preserving converter here because that layer has already run
			// by the time our subtype converter is invoked.
			// And doubling up on it means values get serialized incorrectly.
			if (this.owner.ReferencePreservingManager is not null)
			{
				converter = this.owner.ReferencePreservingManager.UnwrapFromReferencePreservingConverter(converter);
			}

			switch (alias.Type)
			{
				case SubTypeAlias.AliasType.Integer:
					deserializeByIntData.Add(alias.IntAlias, converter);
					break;
				case SubTypeAlias.AliasType.String:
					deserializeByUtf8Data.Add(alias.EncodedAlias, converter);
					break;
				default:
					throw new NotImplementedException("Unknown alias type.");
			}

			Verify.Operation(serializerData.TryAdd(shape.Type, (alias, converter, shape)), $"The type {objectShape.Type.FullName} has more than one subtype with a duplicate alias: {alias}.");
		}

		return new SubTypes
		{
			DeserializersByIntAlias = deserializeByIntData.ToFrozenDictionary(),
			DeserializersByStringAlias = new SpanDictionary<byte, Converter>(deserializeByUtf8Data, ByteSpanEqualityComparer.Ordinal),
			Serializers = serializerData.ToFrozenDictionary(),
		};
	}

	private Converter<T>? GetCustomConverter<T>(ITypeShape<T> typeShape)
	{
		if (typeShape.AttributeProvider?.GetCustomAttributes(typeof(ConverterAttribute), false).FirstOrDefault() is not ConverterAttribute customConverterAttribute)
		{
			return null;
		}

		if (customConverterAttribute.ConverterType.GetConstructor(Type.EmptyTypes) is not ConstructorInfo ctor)
		{
			throw new SerializationException($"{typeof(T).FullName} has {typeof(ConverterAttribute)} that refers to {customConverterAttribute.ConverterType.FullName} but that converter has no default constructor.");
		}

		return (Converter<T>)ctor.Invoke(Array.Empty<object?>());
	}
}
