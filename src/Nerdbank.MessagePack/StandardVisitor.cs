// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

using System.Collections.Frozen;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft;
using PolyType.Utilities;

namespace Nerdbank.MessagePack;

/// <summary>
/// A <see cref="TypeShapeVisitor"/> that produces <see cref="MessagePackConverter{T}"/> instances for each type shape it visits.
/// </summary>
internal class StandardVisitor : TypeShapeVisitor, ITypeShapeFunc
{
	private static readonly FrozenDictionary<Type, object> PrimitiveConverters = new Dictionary<Type, object>()
	{
		{ typeof(char), new CharConverter() },
		{ typeof(byte), new ByteConverter() },
		{ typeof(ushort), new UInt16Converter() },
		{ typeof(uint), new UInt32Converter() },
		{ typeof(ulong), new UInt64Converter() },
		{ typeof(sbyte), new SByteConverter() },
		{ typeof(short), new Int16Converter() },
		{ typeof(int), new Int32Converter() },
		{ typeof(long), new Int64Converter() },
		{ typeof(BigInteger), new BigIntegerConverter() },
		{ typeof(string), new StringConverter() },
		{ typeof(bool), new BooleanConverter() },
		{ typeof(Version), new VersionConverter() },
		{ typeof(Uri), new UriConverter() },
		{ typeof(float), new SingleConverter() },
		{ typeof(double), new DoubleConverter() },
		{ typeof(decimal), new DecimalConverter() },
		{ typeof(DateTime), new DateTimeConverter() },
		{ typeof(DateTimeOffset), new DateTimeOffsetConverter() },
		{ typeof(TimeSpan), new TimeSpanConverter() },
		{ typeof(Guid), new GuidConverter() },
		{ typeof(byte[]), ByteArrayConverter.Instance },
		{ typeof(Memory<byte>), new MemoryOfByteConverter() },
		{ typeof(ReadOnlyMemory<byte>), new ReadOnlyMemoryOfByteConverter() },
		{ typeof(Color), new SystemDrawingColorConverter() },
#if NET
		{ typeof(Rune), new RuneConverter() },
		{ typeof(Int128), new Int128Converter() },
		{ typeof(UInt128), new UInt128Converter() },
		{ typeof(Half), new HalfConverter() },
		{ typeof(TimeOnly), new TimeOnlyConverter() },
		{ typeof(DateOnly), new DateOnlyConverter() },
#endif
	}.ToFrozenDictionary();

	private static readonly FrozenDictionary<Type, object> PrimitiveReferencePreservingConverters = PrimitiveConverters.ToFrozenDictionary(
		pair => pair.Key,
		pair => (object)((IMessagePackConverterInternal)pair.Value).WrapWithReferencePreservation());

	private static readonly InterningStringConverter InterningStringConverter = new();
	private static readonly MessagePackConverter<string> ReferencePreservingInterningStringConverter = InterningStringConverter.WrapWithReferencePreservation();

	private readonly MessagePackSerializer owner;
	private readonly TypeGenerationContext context;

	/// <summary>
	/// Initializes a new instance of the <see cref="StandardVisitor"/> class.
	/// </summary>
	/// <param name="owner">The serializer that created this instance. Usable for obtaining settings that may influence the generated converter.</param>
	/// <param name="context">Context for a generation of a particular data model.</param>
	internal StandardVisitor(MessagePackSerializer owner, TypeGenerationContext context)
	{
		this.owner = owner;
		this.context = context;
		this.OutwardVisitor = this;
	}

	/// <summary>
	/// Gets or sets the visitor that will be used to generate converters for new types that are encountered.
	/// </summary>
	/// <value>Defaults to <see langword="this" />.</value>
	/// <remarks>
	/// This may be changed to a wrapping visitor implementation to implement features such as reference preservation.
	/// </remarks>
	internal ITypeShapeVisitor OutwardVisitor { get; set; }

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
	{
		// Check if the type has a custom converter.
		if (this.owner.TryGetUserDefinedConverter<T>(out MessagePackConverter<T>? userDefinedConverter))
		{
			return userDefinedConverter;
		}

		if (this.owner.InternStrings && typeof(T) == typeof(string))
		{
			return this.owner.PreserveReferences ? ReferencePreservingInterningStringConverter : InterningStringConverter;
		}

		// Check if the type has a built-in converter.
		FrozenDictionary<Type, object> builtins = this.owner.PreserveReferences ? PrimitiveReferencePreservingConverters : PrimitiveConverters;
		if (builtins.TryGetValue(typeof(T), out object? defaultConverter))
		{
			return (MessagePackConverter<T>)defaultConverter;
		}

		// Otherwise, build a converter using the visitor.
		return typeShape.Accept(this.OutwardVisitor);
	}

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (this.GetCustomConverter(objectShape) is MessagePackConverter<T> customConverter)
		{
			return customConverter;
		}

		SubTypes? unionTypes = this.DiscoverUnionTypes(objectShape);

		IConstructorShape? ctorShape = objectShape.GetConstructor();

		Dictionary<string, IConstructorParameterShape>? ctorParametersByName =
			ctorShape?.GetParameters().ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

		List<SerializableProperty<T>>? serializable = null;
		List<DeserializableProperty<T>>? deserializable = null;
		List<(string Name, PropertyAccessors<T> Accessors)?>? propertyAccessors = null;
		foreach (IPropertyShape property in objectShape.GetProperties())
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

					StringEncoding.GetEncodedStringBytes(propertyName, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
					if (accessors.MsgPackWriters is var (serialize, serializeAsync))
					{
						serializable.Add(new(propertyName, msgpackEncoded, serialize, serializeAsync, accessors.PreferAsyncSerialization, accessors.ShouldSerialize, property));
					}

					if (accessors.MsgPackReaders is var (deserialize, deserializeAsync))
					{
						deserializable.Add(new(property.Name, utf8Bytes, deserialize, deserializeAsync, accessors.PreferAsyncSerialization));
					}
				}
			}
		}

		MessagePackConverter<T> converter;
		if (propertyAccessors is not null)
		{
			if (serializable is { Count: > 0 })
			{
				// Members with and without KeyAttribute have been detected as intended for serialization. These two worlds are incompatible.
				throw new MessagePackSerializationException($"The type {objectShape.Type.FullName} has fields/properties that are candidates for serialization but are inconsistently attributed with {nameof(KeyAttribute)}.\nMembers with the attribute: {string.Join(", ", propertyAccessors.Where(a => a is not null).Select(a => a!.Value.Name))}\nMembers without the attribute: {string.Join(", ", serializable.Select(p => p.Name))}");
			}

			ArrayConstructorVisitorInputs<T> inputs = new(propertyAccessors);
			converter = ctorShape is not null
				? (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!
				: new ObjectArrayConverter<T>(inputs.GetJustAccessors(), null, !this.owner.SerializeDefaultValues);
		}
		else
		{
			SpanDictionary<byte, DeserializableProperty<T>>? propertyReaders = deserializable?
				.ToSpanDictionary(
					p => p.PropertyNameUtf8,
					ByteSpanEqualityComparer.Ordinal);

			MapSerializableProperties<T> serializableMap = new(serializable?.ToArray());
			MapDeserializableProperties<T> deserializableMap = new(propertyReaders);
			MapConstructorVisitorInputs<T> inputs = new(serializableMap, deserializableMap);
			if (ctorShape is not null)
			{
				converter = (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!;
			}
			else
			{
				Func<T>? ctor = typeof(T) == typeof(object) ? (Func<T>)(object)new Func<object>(() => new object()) : null;
				converter = new ObjectMapConverter<T>(serializableMap, deserializableMap, ctor, !this.owner.SerializeDefaultValues);
			}
		}

		return unionTypes is null ? converter : new SubTypeUnionConverter<T>(unionTypes, converter);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		IConstructorParameterShape? constructorParameterShape = (IConstructorParameterShape?)state;

		MessagePackConverter<TPropertyType> converter = this.GetConverter(propertyShape.PropertyType);

		(SerializeProperty<TDeclaringType>, SerializePropertyAsync<TDeclaringType>)? msgpackWriters = null;
		Func<TDeclaringType, bool>? shouldSerialize = null;
		if (propertyShape.HasGetter)
		{
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			EqualityComparer<TPropertyType> eq = EqualityComparer<TPropertyType>.Default;

			if (!this.owner.SerializeDefaultValues)
			{
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

			SerializeProperty<TDeclaringType> serialize = (in TDeclaringType container, ref MessagePackWriter writer, SerializationContext context) =>
			{
				// Workaround https://github.com/eiriktsarpalis/PolyType/issues/46.
				// We get significantly improved usability in the API if we use the `in` modifier on the Serialize method
				// instead of `ref`. And since serialization should fundamentally be a read-only operation, this *should* be safe.
				TPropertyType? value = getter(ref Unsafe.AsRef(in container));
				converter.Write(ref writer, value, context);
			};
			SerializePropertyAsync<TDeclaringType> serializeAsync = (TDeclaringType container, MessagePackAsyncWriter writer, SerializationContext context)
				=> converter.WriteAsync(writer, getter(ref container), context);
			msgpackWriters = (serialize, serializeAsync);
		}

		bool suppressIfNoConstructorParameter = true;
		(DeserializeProperty<TDeclaringType>, DeserializePropertyAsync<TDeclaringType>)? msgpackReaders = null;
		if (propertyShape.HasSetter)
		{
			Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context) => setter(ref container, converter.Read(ref reader, context)!);
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context) =>
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
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context) =>
			{
				MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
				bool isNil;
				while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
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
			: new PropertyAccessors<TDeclaringType>(msgpackWriters, msgpackReaders, converter.PreferAsyncSerialization, shouldSerialize, propertyShape);
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
						return new ObjectMapConverter<TDeclaringType>(
							inputs.Serializers,
							inputs.Deserializers,
							constructorShape.GetDefaultConstructor(),
							!this.owner.SerializeDefaultValues);
					}

					List<SerializableProperty<TDeclaringType>> propertySerializers = inputs.Serializers.Properties.Span.ToList();

					SpanDictionary<byte, DeserializableProperty<TArgumentState>> parameters = constructorShape.GetParameters()
						.SelectMany<IConstructorParameterShape, (string Name, DeserializableProperty<TArgumentState> Deserialize)>(p =>
						{
							var prop = (DeserializableProperty<TArgumentState>)p.Accept(this)!;

							// Apply camelCase and PascalCase transformations and accept a serialized form that matches either one.
							// If the parameter name is camelCased (as would typically happen in an ordinary constructor),
							// we want it to match msgpack property names serialized in PascalCase (since the C# property will default to serializing that way).
							// If the parameter name is PascalCased (as would typically happen in a record primary constructor),
							// we want it to match camelCase property names in case the user has camelCase name policy applied.
							// Ultimately we would probably do well to just match without case sensitivity, but we don't support that yet.
							string camelCase = MessagePackNamingPolicy.CamelCase.ConvertName(p.Name);
							string pascalCase = MessagePackNamingPolicy.PascalCase.ConvertName(p.Name);
							return camelCase != pascalCase
								? [(camelCase, prop), (pascalCase, prop)]
								: [(camelCase, prop)];
						}).ToSpanDictionary(
							p => Encoding.UTF8.GetBytes(p.Name),
							p => p.Deserialize,
							ByteSpanEqualityComparer.Ordinal);

					MapSerializableProperties<TDeclaringType> serializeable = inputs.Serializers with { Properties = propertySerializers.ToArray() };
					return new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						serializeable,
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						new MapDeserializableProperties<TArgumentState>(parameters),
						!this.owner.SerializeDefaultValues);
				}

			case ArrayConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.ParameterCount == 0)
					{
						return new ObjectArrayConverter<TDeclaringType>(inputs.GetJustAccessors(), constructorShape.GetDefaultConstructor(), !this.owner.SerializeDefaultValues);
					}

					Dictionary<string, int> propertyIndexesByName = new(StringComparer.OrdinalIgnoreCase);
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
						parameters,
						!this.owner.SerializeDefaultValues);
				}

			default:
				throw new NotSupportedException("Unsupported state.");
		}
	}

	/// <inheritdoc/>
	public override object? VisitConstructorParameter<TArgumentState, TParameterType>(IConstructorParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		MessagePackConverter<TParameterType> converter = this.GetConverter(parameterShape.ParameterType);

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();

		return new DeserializableProperty<TArgumentState>(
			parameterShape.Name,
			StringEncoding.UTF8.GetBytes(parameterShape.Name),
			(ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) => setter(ref state, converter.Read(ref reader, context)!),
			async (TArgumentState state, MessagePackAsyncReader reader, SerializationContext context) =>
			{
				setter(ref state, (await converter.ReadAsync(reader, context).ConfigureAwait(false))!);
				return state;
			},
			converter.PreferAsyncSerialization);
	}

	/// <inheritdoc/>
	public override object? VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null) => new NullableConverter<T>(this.GetConverter(nullableShape.ElementType));

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
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
			MessagePackConverter<TEnumerable>? converter;
			if (enumerableShape.Rank > 1)
			{
#if NET
				return this.owner.MultiDimensionalArrayFormat switch
				{
					MultiDimensionalArrayFormat.Nested => new ArrayWithNestedDimensionsConverter<TEnumerable, TElement>(elementConverter, enumerableShape.Rank),
					MultiDimensionalArrayFormat.Flat => new ArrayWithFlattenedDimensionsConverter<TEnumerable, TElement>(elementConverter),
					_ => throw new NotSupportedException(),
				};
#else
				throw PolyfillExtensions.ThrowNotSupportedOnNETFramework();
#endif
			}
#if NET
			else if (!this.owner.DisableHardwareAcceleration &&
				enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Span &&
				HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out converter))
			{
				return converter;
			}
#endif
			else if (enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Span &&
				ArraysOfPrimitivesConverters.TryGetConverter(enumerableShape.GetGetEnumerable(), enumerableShape.GetSpanConstructor(), out converter))
			{
				return converter;
			}
			else
			{
				return new ArrayConverter<TElement>(elementConverter);
			}
		}

		Func<TEnumerable, IEnumerable<TElement>> getEnumerable = enumerableShape.GetGetEnumerable();
		return enumerableShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => new EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter),
			CollectionConstructionStrategy.Mutable => new MutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetAddElement(), enumerableShape.GetDefaultConstructor()),
#if NET
			CollectionConstructionStrategy.Span when !this.owner.DisableHardwareAcceleration && HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out MessagePackConverter<TEnumerable>? converter) => converter,
#endif
			CollectionConstructionStrategy.Span when ArraysOfPrimitivesConverters.TryGetConverter(getEnumerable, enumerableShape.GetSpanConstructor(), out MessagePackConverter<TEnumerable>? converter) => converter,
			CollectionConstructionStrategy.Span => new SpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetSpanConstructor()),
			CollectionConstructionStrategy.Enumerable => new EnumerableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetEnumerableConstructor()),
			_ => throw new NotSupportedException($"Unrecognized enumerable pattern: {typeof(TEnumerable).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
		=> this.owner.SerializeEnumValuesByName
			? new EnumAsStringConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType))
			: new EnumAsOrdinalConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType));

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected MessagePackConverter<T> GetConverter<T>(ITypeShape<T> shape, object? state = null)
	{
		return (MessagePackConverter<T>)this.context.GetOrAdd(shape, state)!;
	}

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected IMessagePackConverterInternal GetConverter(ITypeShape shape, object? state = null)
	{
		ITypeShapeFunc self = this;
		return (IMessagePackConverterInternal)shape.Invoke(this, state)!;
	}

	/// <summary>
	/// Returns a dictionary of <see cref="MessagePackConverter{T}"/> objects for each subtype, keyed by their alias.
	/// </summary>
	/// <param name="objectShape">The shape of the data type that may define derived types that are also allowed for serialization.</param>
	/// <returns>A dictionary of <see cref="MessagePackConverter{T}"/> objects, keyed by the alias by which they will be identified in the data stream.</returns>
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

		Dictionary<int, IMessagePackConverter> deserializeByIntData = new();
		Dictionary<ReadOnlyMemory<byte>, IMessagePackConverter> deserializeByUtf8Data = new();
		Dictionary<Type, (SubTypeAlias Alias, IMessagePackConverter Converter, ITypeShape Shape)> serializerData = new();
		foreach (KeyValuePair<SubTypeAlias, ITypeShape> pair in mapping)
		{
			SubTypeAlias alias = pair.Key;
			ITypeShape shape = pair.Value;

			// We don't want a reference-preserving converter here because that layer has already run
			// by the time our subtype converter is invoked.
			// And doubling up on it means values get serialized incorrectly.
			IMessagePackConverter converter = this.GetConverter(shape).UnwrapReferencePreservation();
			switch (alias.Type)
			{
				case SubTypeAlias.AliasType.Integer:
					deserializeByIntData.Add(alias.IntAlias, converter);
					break;
				case SubTypeAlias.AliasType.String:
					deserializeByUtf8Data.Add(alias.Utf8Alias, converter);
					break;
				default:
					throw new NotImplementedException("Unknown alias type.");
			}

			Verify.Operation(serializerData.TryAdd(shape.Type, (alias, converter, shape)), $"The type {objectShape.Type.FullName} has more than one subtype with a duplicate alias: {alias}.");
		}

		return new SubTypes
		{
			DeserializersByIntAlias = deserializeByIntData.ToFrozenDictionary(),
			DeserializersByStringAlias = new SpanDictionary<byte, IMessagePackConverter>(deserializeByUtf8Data, ByteSpanEqualityComparer.Ordinal),
			Serializers = serializerData.ToFrozenDictionary(),
		};
	}

	private MessagePackConverter<T>? GetCustomConverter<T>(ITypeShape<T> typeShape)
	{
		if (typeShape.AttributeProvider?.GetCustomAttributes(typeof(MessagePackConverterAttribute), false).FirstOrDefault() is not MessagePackConverterAttribute customConverterAttribute)
		{
			return null;
		}

		if (customConverterAttribute.ConverterType.GetConstructor(Type.EmptyTypes) is not ConstructorInfo ctor)
		{
			throw new MessagePackSerializationException($"{typeof(T).FullName} has {typeof(MessagePackConverterAttribute)} that refers to {customConverterAttribute.ConverterType.FullName} but that converter has no default constructor.");
		}

		return (MessagePackConverter<T>)ctor.Invoke(Array.Empty<object?>());
	}
}
