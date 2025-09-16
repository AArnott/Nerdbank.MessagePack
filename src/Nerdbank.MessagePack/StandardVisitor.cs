// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable DuckTyping // Experimental API

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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
	private static readonly InterningStringConverter InterningStringConverter = new();
	private static readonly MessagePackConverter<string> ReferencePreservingInterningStringConverter = InterningStringConverter.WrapWithReferencePreservation();

	private readonly ConverterCache owner;
	private readonly TypeGenerationContext context;

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
	}

	/// <summary>
	/// Gets or sets the visitor that will be used to generate converters for new types that are encountered.
	/// </summary>
	/// <value>Defaults to <see langword="this" />.</value>
	/// <remarks>
	/// This may be changed to a wrapping visitor implementation to implement features such as reference preservation.
	/// </remarks>
	internal TypeShapeVisitor OutwardVisitor { get; set; }

	/// <inheritdoc/>
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => typeShape.Accept(this.OutwardVisitor, state);

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(objectShape, objectShape.AttributeProvider, out MessagePackConverter<T>? customConverter))
		{
			return customConverter;
		}

		IConstructorShape? ctorShape = objectShape.Constructor;

		Dictionary<string, IParameterShape>? ctorParametersByName = null;
		if (ctorShape is not null)
		{
			ctorParametersByName = new(StringComparer.Ordinal);
			foreach (IParameterShape ctorParameter in ctorShape.Parameters)
			{
				// Keep the one with the Kind that we prefer.
				if (ctorParameter.Kind == ParameterKind.MethodParameter)
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
		DirectPropertyAccess<T, UnusedDataPacket>? unusedDataPropertyAccess = null;
		int propertyIndex = -1;
		foreach (IPropertyShape property in objectShape.Properties)
		{
			if (property is IPropertyShape<T, UnusedDataPacket> unusedDataProperty)
			{
				if (unusedDataPropertyAccess is null)
				{
					unusedDataPropertyAccess = new DirectPropertyAccess<T, UnusedDataPacket>(unusedDataProperty.HasSetter ? unusedDataProperty.GetSetter() : null, unusedDataProperty.HasGetter ? unusedDataProperty.GetGetter() : null);
				}
				else
				{
					throw new MessagePackSerializationException($"The type {objectShape.Type.FullName} has multiple properties of type {typeof(UnusedDataPacket).FullName}. Only one such property is allowed.");
				}

				continue;
			}

			propertyIndex++;
			string propertyName = this.owner.GetSerializedPropertyName(property);

			IParameterShape? matchingConstructorParameter = null;
			ctorParametersByName?.TryGetValue(property.Name, out matchingConstructorParameter);

			if (property.Accept(this, matchingConstructorParameter) is PropertyAccessors<T> accessors)
			{
				KeyAttribute? keyAttribute = (KeyAttribute?)property.AttributeProvider?.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault();
				if (keyAttribute is not null || this.owner.PerfOverSchemaStability || objectShape.IsTupleType)
				{
					propertyAccessors ??= new();
					int index = keyAttribute?.Index ?? propertyIndex;
					while (propertyAccessors.Count <= index)
					{
						propertyAccessors.Add(null);
					}

					propertyAccessors[index] = (propertyName, accessors);
				}
				else
				{
					serializable ??= new();
					deserializable ??= new();

					StringEncoding.GetEncodedStringBytes(propertyName, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
					if (accessors.MsgPackWriters is var (serialize, serializeAsync))
					{
						serializable.Add(new(propertyName, msgpackEncoded, serialize, serializeAsync, accessors.Converter, accessors.ShouldSerialize, property));
					}

					if (accessors.MsgPackReaders is var (deserialize, deserializeAsync))
					{
						deserializable.Add(new(property.Name, utf8Bytes, deserialize, deserializeAsync, accessors.Converter, property.Position));
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
				throw new MessagePackSerializationException(PrepareExceptionMessage());

				string PrepareExceptionMessage()
				{
					// Avoid use of Linq methods since it will lead to native code gen that closes generics over user types.
					StringBuilder builder = new();
					builder.Append($"The type {objectShape.Type.FullName} has fields/properties that are candidates for serialization but are inconsistently attributed with {nameof(KeyAttribute)}.\nMembers with the attribute: ");
					bool first = true;
					foreach ((string Name, PropertyAccessors<T> Accessors)? a in propertyAccessors)
					{
						if (a is not null)
						{
							if (!first)
							{
								builder.Append(", ");
							}

							first = false;
							builder.Append(a.Value.Name);
						}
					}

					builder.Append("\nMembers without the attribute: ");
					first = true;
					foreach (SerializableProperty<T> p in serializable)
					{
						if (!first)
						{
							builder.Append(", ");
						}

						first = false;
						builder.Append(p.Name);
					}

					return builder.ToString();
				}
			}

			ArrayConstructorVisitorInputs<T> inputs = new(propertyAccessors, unusedDataPropertyAccess);
			converter = ctorShape is not null
				? (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!
				: new ObjectArrayConverter<T>(inputs.GetJustAccessors(), unusedDataPropertyAccess, null, objectShape.Properties, this.owner.SerializeDefaultValues);
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
				MapConstructorVisitorInputs<T> inputs = new(serializableMap, deserializableMap, ctorParametersByName!, unusedDataPropertyAccess);
				converter = (MessagePackConverter<T>)ctorShape.Accept(this, inputs)!;
			}
			else
			{
				Func<T>? ctor = typeof(T) == typeof(object) ? (Func<T>)(object)new Func<object>(() => new object()) : null;
				converter = new ObjectMapConverter<T>(
					serializableMap,
					deserializableMap,
					unusedDataPropertyAccess,
					ctor,
					objectShape.Properties,
					this.owner.SerializeDefaultValues);
			}
		}

		// Test IsValueType before considering unions so that the native compiler
		// does not have to generate a SubTypes<T> for value types which will never be used.
		if (!typeof(T).IsValueType)
		{
			if (this.owner.TryGetDynamicUnion(objectShape.Type, out DerivedTypeUnion? union) && !union.Disabled)
			{
				return union switch
				{
					IDerivedTypeMapping mapping => new UnionConverter<T>(converter, this.CreateSubTypes(objectShape.Type, converter, mapping)),
					DerivedTypeDuckTyping duckTyping => this.CreateDuckTypingUnionConverter<T>(duckTyping, converter),
					_ => throw new NotSupportedException($"Unrecognized union type: {union.GetType().Name}"),
				};
			}
		}

		return converter;
	}

	/// <inheritdoc/>
	public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null)
	{
		MessagePackConverter<TUnion> baseTypeConverter = (MessagePackConverter<TUnion>)unionShape.BaseType.Accept(this)!;

		if (baseTypeConverter is UnionConverter<TUnion>)
		{
			// A runtime mapping *and* attributes are defined for the same base type.
			// The runtime mapping has already been applied and that trumps attributes.
			// Just return the union converter we created for the runtime mapping to avoid
			// double-nesting.
			return baseTypeConverter;
		}

		// Runtime mapping overrides attributes.
		if (unionShape.BaseType is IObjectTypeShape<TUnion> { Type: Type baseType } && this.owner.TryGetDynamicUnion(baseType, out DerivedTypeUnion? union))
		{
			return union switch
			{
				{ Disabled: true } => baseTypeConverter,
				IDerivedTypeMapping mapping => new UnionConverter<TUnion>(baseTypeConverter, this.CreateSubTypes(baseType, baseTypeConverter, mapping)),
				DerivedTypeDuckTyping duckTyping => this.CreateDuckTypingUnionConverter<TUnion>(duckTyping, baseTypeConverter),
				_ => throw new NotSupportedException($"Unrecognized union type: {union.GetType().Name}"),
			};
		}

		Getter<TUnion, int> getUnionCaseIndex = unionShape.GetGetUnionCaseIndex();
		Dictionary<int, MessagePackConverter> deserializerByIntAlias = new(unionShape.UnionCases.Count);
		List<(DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)> serializers = new(unionShape.UnionCases.Count);
		KeyValuePair<int, MessagePackConverter<TUnion>>[] unionCases = unionShape.UnionCases
			.Select(unionCase =>
			{
				bool useTag = unionCase.IsTagSpecified || this.owner.PerfOverSchemaStability;
				DerivedTypeIdentifier alias = useTag ? new(unionCase.Tag) : new(unionCase.Name);
				var caseConverter = (MessagePackConverter<TUnion>)unionCase.Accept(this, null)!;
				deserializerByIntAlias.Add(unionCase.Tag, caseConverter);
				serializers.Add((alias, caseConverter, unionCase.UnionCaseType));

				return new KeyValuePair<int, MessagePackConverter<TUnion>>(unionCase.Tag, caseConverter);
			})
			.ToArray();
		SubTypes<TUnion> subTypes = new()
		{
			DeserializersByIntAlias = deserializerByIntAlias.ToFrozenDictionary(),
			DeserializersByStringAlias = serializers.Where(v => v.Alias.Type == DerivedTypeIdentifier.AliasType.String).ToSpanDictionary(
				p => p.Alias.Utf8Alias,
				p => p.Converter,
				ByteSpanEqualityComparer.Ordinal),
			Serializers = serializers.ToFrozenSet(),
			TryGetSerializer = (ref TUnion value) => getUnionCaseIndex(ref value) is int idx && idx >= 0 ? (serializers[idx].Alias, serializers[idx].Converter) : null,
		};

		return new UnionConverter<TUnion>(baseTypeConverter, subTypes);
	}

	/// <inheritdoc/>
	public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null)
	{
		// NB: don't use the cached converter for TUnionCase, as it might equal TUnion.
		var caseConverter = (MessagePackConverter<TUnionCase>)unionCaseShape.UnionCaseType.Accept(this)!;
		return new UnionCaseConverter<TUnionCase, TUnion>(caseConverter, unionCaseShape.Marshaler);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		IParameterShape? constructorParameterShape = (IParameterShape?)state;

		MessagePackConverter<TPropertyType> converter = this.GetConverterForMemberOrParameter(propertyShape.PropertyType, propertyShape.AttributeProvider);

		static string CreateReadFailMessage(IPropertyShape<TDeclaringType, TPropertyType> propertyShape) => $"Failed to deserialize '{propertyShape.Name}' property on {typeof(TDeclaringType).FullName}.";
		static string CreateWriteFailMessage(IPropertyShape<TDeclaringType, TPropertyType> propertyShape) => $"Failed to serialize '{propertyShape.Name}' property on {typeof(TDeclaringType).FullName}.";

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

			SerializeProperty<TDeclaringType> serialize = (in TDeclaringType container, ref MessagePackWriter writer, SerializationContext context) =>
			{
				// Workaround https://github.com/eiriktsarpalis/PolyType/issues/46.
				// We get significantly improved usability in the API if we use the `in` modifier on the Serialize method
				// instead of `ref`. And since serialization should fundamentally be a read-only operation, this *should* be safe.
				TPropertyType? value = getter(ref Unsafe.AsRef(in container));
				try
				{
					converter.Write(ref writer, value, context);
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateWriteFailMessage(propertyShape), ex);
				}
			};
			SerializePropertyAsync<TDeclaringType> serializeAsync = async (TDeclaringType container, MessagePackAsyncWriter writer, SerializationContext context) =>
			{
				try
				{
					await converter.WriteAsync(writer, getter(ref container), context).ConfigureAwait(false);
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateWriteFailMessage(propertyShape), ex);
				}
			};
			msgpackWriters = (serialize, serializeAsync);
		}

		bool suppressIfNoConstructorParameter = true;
		(DeserializeProperty<TDeclaringType>, DeserializePropertyAsync<TDeclaringType>)? msgpackReaders = null;
		if (propertyShape.HasSetter)
		{
			Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
			DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context) =>
			{
				try
				{
					setter(ref container, converter.Read(ref reader, context)!);
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(propertyShape), ex);
				}
			};
			DeserializePropertyAsync<TDeclaringType> deserializeAsync = async (TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context) =>
			{
				try
				{
					setter(ref container, (await converter.ReadAsync(reader, context).ConfigureAwait(false))!);
					return container;
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(propertyShape), ex);
				}
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

				try
				{
					TPropertyType collection = getter(ref container);
					inflater.DeserializeInto(ref reader, ref collection, context);
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(propertyShape), ex);
				}
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
					try
					{
						TPropertyType collection = propertyShape.GetGetter()(ref container);
						await inflater.DeserializeIntoAsync(reader, collection, context).ConfigureAwait(false);
					}
					catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
					{
						throw new MessagePackSerializationException(CreateReadFailMessage(propertyShape), ex);
					}
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
							inputs.UnusedDataProperty,
							constructorShape.GetDefaultConstructor(),
							constructorShape.DeclaringType.Properties,
							this.owner.SerializeDefaultValues);
					}

					List<SerializableProperty<TDeclaringType>> propertySerializers = inputs.Serializers.Properties.Span.ToList();

					var spanDictContent = new KeyValuePair<ReadOnlyMemory<byte>, DeserializableProperty<TArgumentState>>[inputs.ParametersByName.Count];
					int i = 0;
					foreach (KeyValuePair<string, IParameterShape> p in inputs.ParametersByName)
					{
						IPropertyShape? matchingProperty = constructorShape.DeclaringType.Properties.FirstOrDefault(prop => prop.Name == p.Value.Name);
						var prop = (DeserializableProperty<TArgumentState>)p.Value.Accept(this, constructorShape)!;
						string name = matchingProperty is not null
							? this.owner.GetSerializedPropertyName(matchingProperty)
							: this.owner.GetSerializedPropertyName(p.Value.Name, null);
						spanDictContent[i++] = new(Encoding.UTF8.GetBytes(name), prop);
					}

					SpanDictionary<byte, DeserializableProperty<TArgumentState>> parameters = new(spanDictContent, ByteSpanEqualityComparer.Ordinal);

					MapSerializableProperties<TDeclaringType> serializeable = inputs.Serializers;
					serializeable.Properties = propertySerializers.ToArray();
					return new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						serializeable,
						constructorShape.GetArgumentStateConstructor(),
						inputs.UnusedDataProperty,
						constructorShape.GetParameterizedConstructor(),
						new MapDeserializableProperties<TArgumentState>(parameters),
						constructorShape.Parameters,
						this.owner.SerializeDefaultValues,
						this.owner.DeserializeDefaultValues);
				}

			case ArrayConstructorVisitorInputs<TDeclaringType> inputs:
				{
					if (constructorShape.Parameters.Count == 0)
					{
						return new ObjectArrayConverter<TDeclaringType>(inputs.GetJustAccessors(), inputs.UnusedDataProperty, constructorShape.GetDefaultConstructor(), constructorShape.DeclaringType.Properties, this.owner.SerializeDefaultValues);
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
					foreach (IParameterShape parameter in constructorShape.Parameters)
					{
						if (parameter is IParameterShape<TArgumentState, UnusedDataPacket>)
						{
							continue;
						}

						if (!propertyIndexesByName.TryGetValue(parameter.Name, out int index))
						{
							throw new NotSupportedException($"{constructorShape.DeclaringType.Type.FullName} has a constructor parameter named '{parameter.Name}' that does not match any property on the type, even allowing for camelCase to PascalCase conversion. This is not supported. Adjust the parameters and/or properties or write a custom converter for this type.");
						}

						parameters[index] = (DeserializableProperty<TArgumentState>)parameter.Accept(this, constructorShape)!;
					}

					return new ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
						inputs.GetJustAccessors(),
						inputs.UnusedDataProperty,
						constructorShape.GetArgumentStateConstructor(),
						constructorShape.GetParameterizedConstructor(),
						parameters,
						constructorShape.Parameters,
						this.owner.SerializeDefaultValues,
						this.owner.DeserializeDefaultValues);
				}

			default:
				throw new NotSupportedException("Unsupported state.");
		}
	}

	/// <inheritdoc/>
	public override object? VisitParameter<TArgumentState, TParameterType>(IParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		IConstructorShape constructorShape = (IConstructorShape)(state ?? throw new ArgumentNullException(nameof(state)));

		MessagePackConverter<TParameterType> converter = this.GetConverterForMemberOrParameter(parameterShape.ParameterType, parameterShape.AttributeProvider);

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();

		static string CreateReadFailMessage(IParameterShape<TArgumentState, TParameterType> parameterShape, IConstructorShape constructorShape) => $"Failed to deserialize value for '{parameterShape.Name}' parameter on {constructorShape.DeclaringType.Type.FullName}.";

		DeserializeProperty<TArgumentState> read;
		DeserializePropertyAsync<TArgumentState> readAsync;
		bool throwOnNull =
			(this.owner.DeserializeDefaultValues & DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties) != DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties
			&& parameterShape.IsNonNullable
			&& !typeof(TParameterType).IsValueType;
		if (throwOnNull)
		{
			static Exception NewDisallowedDeserializedNullValueException(IParameterShape parameter) => new MessagePackSerializationException($"The parameter '{parameter.Name}' is non-nullable, but the deserialized value was null.") { Code = MessagePackSerializationException.ErrorCode.DisallowedNullValue };
			read = (ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) =>
			{
				try
				{
					ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
					setter(ref state, converter.Read(ref reader, context) ?? throw NewDisallowedDeserializedNullValueException(parameterShape));
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
				}
			};
			readAsync = async (TArgumentState state, MessagePackAsyncReader reader, SerializationContext context) =>
			{
				try
				{
					ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
					setter(ref state, (await converter.ReadAsync(reader, context).ConfigureAwait(false)) ?? throw NewDisallowedDeserializedNullValueException(parameterShape));
					return state;
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
				}
			};
		}
		else
		{
			read = (ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) =>
			{
				try
				{
					ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
					setter(ref state, converter.Read(ref reader, context)!);
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
				}
			};
			readAsync = async (TArgumentState state, MessagePackAsyncReader reader, SerializationContext context) =>
			{
				try
				{
					ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
					setter(ref state, (await converter.ReadAsync(reader, context).ConfigureAwait(false))!);
					return state;
				}
				catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
				{
					throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
				}
			};
		}

		return new DeserializableProperty<TArgumentState>(
			parameterShape.Name,
			StringEncoding.UTF8.GetBytes(parameterShape.Name),
			read,
			readAsync,
			converter,
			parameterShape.Position);
	}

	/// <inheritdoc/>
	public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null)
		=> new OptionalConverter<TOptional, TElement>(this.GetConverter(optionalShape.ElementType), optionalShape.GetDeconstructor(), optionalShape.GetNoneConstructor(), optionalShape.GetSomeConstructor());

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(dictionaryShape, dictionaryShape.AttributeProvider, out MessagePackConverter<TDictionary>? customConverter))
		{
			return customConverter;
		}

		MemberConverterInfluence? memberInfluence = state as MemberConverterInfluence;

		// Serialization functions.
		MessagePackConverter<TKey> keyConverter = this.GetConverter(dictionaryShape.KeyType);
		MessagePackConverter<TValue> valueConverter = this.GetConverter(dictionaryShape.ValueType);
		Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable = dictionaryShape.GetGetDictionary();

		// Deserialization functions.
		return dictionaryShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => new DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter),
			CollectionConstructionStrategy.Mutable => new MutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetInserter(DictionaryInsertionMode.Throw), dictionaryShape.GetDefaultConstructor(), this.GetCollectionOptions(dictionaryShape, memberInfluence)),
			CollectionConstructionStrategy.Parameterized => new ImmutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetParameterizedConstructor(), this.GetCollectionOptions(dictionaryShape, memberInfluence)),
			_ => throw new NotSupportedException($"Unrecognized dictionary pattern: {typeof(TDictionary).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(enumerableShape, enumerableShape.AttributeProvider, out MessagePackConverter<TEnumerable>? customConverter))
		{
			return customConverter;
		}

		MemberConverterInfluence? memberInfluence = state as MemberConverterInfluence;

		// Serialization functions.
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
				enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Parameterized &&
				HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out converter))
			{
				return converter;
			}
#endif
			else if (enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Parameterized &&
				ArraysOfPrimitivesConverters.TryGetConverter(enumerableShape.GetGetEnumerable(), enumerableShape.GetParameterizedConstructor(), out converter))
			{
				return converter;
			}
			else
			{
				return new ArrayConverter<TElement>(elementConverter);
			}
		}

		Func<TEnumerable, IEnumerable<TElement>>? getEnumerable = enumerableShape.IsAsyncEnumerable ? null : enumerableShape.GetGetEnumerable();
		return enumerableShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => new EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter),
			CollectionConstructionStrategy.Mutable => new MutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetAppender(), enumerableShape.GetDefaultConstructor(), this.GetCollectionOptions(enumerableShape, memberInfluence)),
#if NET
			CollectionConstructionStrategy.Parameterized when !this.owner.DisableHardwareAcceleration && HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out MessagePackConverter<TEnumerable>? converter) => converter,
#endif
			CollectionConstructionStrategy.Parameterized when getEnumerable is not null && ArraysOfPrimitivesConverters.TryGetConverter(getEnumerable, enumerableShape.GetParameterizedConstructor(), out MessagePackConverter<TEnumerable>? converter) => converter,
			CollectionConstructionStrategy.Parameterized => new SpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetParameterizedConstructor(), this.GetCollectionOptions(enumerableShape, memberInfluence)),
			_ => throw new NotSupportedException($"Unrecognized enumerable pattern: {typeof(TEnumerable).Name}"),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(enumShape, enumShape.AttributeProvider, out MessagePackConverter<TEnum>? customConverter))
		{
			return customConverter;
		}

		return this.owner.SerializeEnumValuesByName
			? new EnumAsStringConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType), enumShape.Members)
			: new EnumAsOrdinalConverter<TEnum, TUnderlying>(this.GetConverter(enumShape.UnderlyingType));
	}

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
		=> new SurrogateConverter<T, TSurrogate>(surrogateShape, this.GetConverter(surrogateShape.SurrogateType, state: state));

	/// <inheritdoc/>
	public override object? VisitFunction<TFunction, TArgumentState, TResult>(IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape, object? state = null)
		=> throw new NotSupportedException("Delegate types cannot be serialized.");

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <param name="memberAttributes">
	/// The attribute provider on the member that requires this converter.
	/// This is used to look for <see cref="UseComparerAttribute"/> which may customize the converter we return.
	/// </param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	/// <remarks>
	/// This is the main entry point for getting converters on behalf of other functions,
	/// e.g. converting the key or value in a dictionary.
	/// It does <em>not</em> take <see cref="MessagePackConverterAttribute"/> into account
	/// if it were to appear in <paramref name="memberAttributes"/>.
	/// Callers that want to respect that attribute must call <see cref="TryGetConverterFromAttribute"/> first.
	/// </remarks>
	protected MessagePackConverter<T> GetConverter<T>(ITypeShape<T> shape, ICustomAttributeProvider? memberAttributes = null, object? state = null)
	{
		if (memberAttributes is not null)
		{
			if (state is not null)
			{
				throw new ArgumentException("Providing both attributes and state are not supported because we reuse the state parameter for attribute influence.");
			}

			if (memberAttributes.GetCustomAttribute<UseComparerAttribute>() is { } attribute)
			{
				MemberConverterInfluence memberInfluence = new()
				{
					ComparerSource = attribute.ComparerType,
					ComparerSourceMemberName = attribute.MemberName,
				};

				// PERF: Ideally, we can store and retrieve member influenced converters
				// just like we do for non-member influenced ones.
				// We'd probably use a separate dictionary dedicated to member-influenced converters.
				return (MessagePackConverter<T>)shape.Invoke(this, memberInfluence)!;
			}
		}

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

	private static void ThrowIfAlreadyAssigned<TArgumentState>(in TArgumentState argumentState, int position, string name)
		where TArgumentState : IArgumentState
	{
		if (argumentState.IsArgumentSet(position))
		{
			Throw(name);

			[DoesNotReturn]
			static void Throw(string name)
				=> throw new MessagePackSerializationException($"The parameter '{name}' has already been assigned a value.")
				{
					Code = MessagePackSerializationException.ErrorCode.DoublePropertyAssignment,
				};
		}
	}

	private SubTypes<TBaseType> CreateSubTypes<TBaseType>(Type baseType, MessagePackConverter<TBaseType> baseTypeConverter, IDerivedTypeMapping mapping)
	{
		if (mapping is DerivedTypeUnion { Disabled: true })
		{
			return SubTypes<TBaseType>.DisabledInstance;
		}

		Dictionary<int, MessagePackConverter> deserializeByIntData = new();
		Dictionary<ReadOnlyMemory<byte>, MessagePackConverter> deserializeByUtf8Data = new();
		Dictionary<Type, (DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)> serializerData = new();
		foreach (KeyValuePair<DerivedTypeIdentifier, ITypeShape> pair in mapping.GetDerivedTypesMapping())
		{
			DerivedTypeIdentifier alias = pair.Key;
			ITypeShape shape = pair.Value;

			// We don't want a reference-preserving converter here because that layer has already run
			// by the time our subtype converter is invoked.
			// And doubling up on it means values get serialized incorrectly.
			MessagePackConverter converter = shape.Type == baseType ? baseTypeConverter : this.GetConverter(shape).UnwrapReferencePreservation();
			switch (alias.Type)
			{
				case DerivedTypeIdentifier.AliasType.Integer:
					deserializeByIntData.Add(alias.IntAlias, converter);
					break;
				case DerivedTypeIdentifier.AliasType.String:
					deserializeByUtf8Data.Add(alias.Utf8Alias, converter);
					break;
				default:
					throw new NotImplementedException("Unspecified alias type.");
			}

			Verify.Operation(serializerData.TryAdd(shape.Type, (alias, converter, shape)), $"The type {baseType.FullName} has more than one subtype with a duplicate alias: {alias}.");
		}

		// Our runtime type checks must be done in an order that will select the most derived matching type.
		(DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)[] sortedTypes = serializerData.Values.ToArray();
		Array.Sort(sortedTypes, (a, b) => DerivedTypeComparer.Default.Compare(a.Shape.Type, b.Shape.Type));

		return new SubTypes<TBaseType>
		{
			DeserializersByIntAlias = deserializeByIntData.ToFrozenDictionary(),
			DeserializersByStringAlias = new SpanDictionary<byte, MessagePackConverter>(deserializeByUtf8Data, ByteSpanEqualityComparer.Ordinal),
			Serializers = serializerData.Select(t => t.Value).ToFrozenSet(),
			TryGetSerializer = (ref TBaseType v) =>
			{
				if (v is null)
				{
					return null;
				}

				foreach ((DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape) pair in sortedTypes)
				{
					if (pair.Shape.Type.IsAssignableFrom(v.GetType()))
					{
						return (pair.Alias, pair.Converter);
					}
				}

				return null;
			},
		};
	}

	/// <summary>
	/// Returns a dictionary of <see cref="MessagePackConverter{T}"/> objects for each subtype, keyed by their alias.
	/// </summary>
	/// <param name="duckTyping">Information about the base type and derived types that distinguish objects between each type.</param>
	/// <param name="baseTypeConverter">The converter to use when serializing the base type itself.</param>
	/// <returns>A dictionary of <see cref="MessagePackConverter{T}"/> objects, keyed by the alias by which they will be identified in the data stream.</returns>
	private ShapeBasedUnionConverter<TBase>? CreateDuckTypingUnionConverter<TBase>(DerivedTypeDuckTyping duckTyping, MessagePackConverter<TBase> baseTypeConverter)
	{
		// Create converters for each member type
		Dictionary<Type, MessagePackConverter> convertersByType = new(duckTyping.DerivedShapes.Length);
		foreach (ITypeShape shape in duckTyping.DerivedShapes.Span)
		{
			if (!typeof(TBase).IsAssignableFrom(shape.Type))
			{
				throw new ArgumentException($"Type '{shape.Type}' is not assignable to base type '{typeof(TBase)}'.", nameof(duckTyping));
			}

			MessagePackConverter converter = (MessagePackConverter)this.GetConverter(shape);
			convertersByType[shape.Type] = converter;
		}

		return new ShapeBasedUnionConverter<TBase>(baseTypeConverter, duckTyping, convertersByType);
	}

	/// <summary>
	/// Retrieves a converter for the given type shape from runtime-supplied user sources, primitive converters, or attribute-specified converters.
	/// </summary>
	/// <typeparam name="T">The type for which a converter is required.</typeparam>
	/// <param name="typeShape">The shape for the type to be converted.</param>
	/// <param name="attributeProvider"><inheritdoc cref="TryGetConverterFromAttribute{T}" path="/param[@name='attributeProvider']"/></param>
	/// <param name="converter">Receives the converter if one is found.</param>
	/// <returns>A value indicating whether a match was found.</returns>
	private bool TryGetCustomOrPrimitiveConverter<T>(ITypeShape<T> typeShape, ICustomAttributeProvider? attributeProvider, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		// Check if the type has a custom converter.
		if (this.owner.TryGetRuntimeProfferedConverter(typeShape, out converter))
		{
			return true;
		}

		if (this.owner.InternStrings && typeof(T) == typeof(string))
		{
			converter = (MessagePackConverter<T>)(object)(this.owner.PreserveReferences != ReferencePreservationMode.Off ? ReferencePreservingInterningStringConverter : InterningStringConverter);
			return true;
		}

		// Check if the type has a built-in converter.
		if (PrimitiveConverterLookup.TryGetPrimitiveConverter(this.owner.PreserveReferences, out converter))
		{
			return true;
		}

		return this.TryGetConverterFromAttribute(typeShape, attributeProvider, out converter);
	}

	private MessagePackConverter<T> GetConverterForMemberOrParameter<T>(ITypeShape<T> typeShape, ICustomAttributeProvider? attributeProvider)
	{
		return this.TryGetConverterFromAttribute(typeShape, attributeProvider, out MessagePackConverter<T>? converter)
			? converter
			: this.GetConverter(typeShape, attributeProvider);
	}

	/// <summary>
	/// Activates a converter for the given shape if a <see cref="MessagePackConverterAttribute"/> is present on the type or member.
	/// </summary>
	/// <typeparam name="T">The type of value to be serialized.</typeparam>
	/// <param name="typeShape">The shape of the type to be serialized.</param>
	/// <param name="attributeProvider">
	/// The source of the attributes.
	/// This will typically be the attributes on the type itself, but may be the attributes on the requesting property or parameter.
	/// </param>
	/// <param name="converter">Receives the converter, if applicable.</param>
	/// <returns>A value indicating whether a converter was found.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if the prescribed converter has no default constructor.</exception>
	private bool TryGetConverterFromAttribute<T>(ITypeShape<T> typeShape, ICustomAttributeProvider? attributeProvider, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (attributeProvider?.GetCustomAttribute<MessagePackConverterAttribute>() is not { } customConverterAttribute)
		{
			converter = null;
			return false;
		}

		Type converterType = customConverterAttribute.ConverterType;
		if ((typeShape.GetAssociatedTypeShape(converterType) as IObjectTypeShape)?.GetDefaultConstructor() is Func<object> converterFactory)
		{
			converter = (MessagePackConverter<T>)converterFactory();
			if (this.owner.PreserveReferences != ReferencePreservationMode.Off)
			{
				converter = converter.WrapWithReferencePreservation();
			}

			return true;
		}

		if (converterType.GetConstructor(Type.EmptyTypes) is not ConstructorInfo ctor)
		{
			throw new MessagePackSerializationException($"{typeof(T).FullName} has {typeof(MessagePackConverterAttribute)} that refers to {customConverterAttribute.ConverterType.FullName} but that converter has no default constructor.");
		}

		converter = (MessagePackConverter<T>)ctor.Invoke(Array.Empty<object?>());
		return true;
	}

	private CollectionConstructionOptions<TKey> GetCollectionOptions<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, MemberConverterInfluence? memberInfluence)
		where TKey : notnull
		=> this.GetCollectionOptions(dictionaryShape.KeyType, dictionaryShape.SupportedComparer, memberInfluence);

	private CollectionConstructionOptions<TElement> GetCollectionOptions<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, MemberConverterInfluence? memberInfluence)
		=> this.GetCollectionOptions(enumerableShape.ElementType, enumerableShape.SupportedComparer, memberInfluence);

	private CollectionConstructionOptions<TKey> GetCollectionOptions<TKey>(ITypeShape<TKey> keyShape, CollectionComparerOptions requiredComparer, MemberConverterInfluence? memberInfluence)
	{
		if (this.owner.ComparerProvider is null)
		{
			return default;
		}

		try
		{
			return requiredComparer switch
			{
				CollectionComparerOptions.None => default,
				CollectionComparerOptions.Comparer => new() { Comparer = memberInfluence?.GetComparer<TKey>() ?? this.owner.ComparerProvider.GetComparer(keyShape) },
				CollectionComparerOptions.EqualityComparer => new() { EqualityComparer = memberInfluence?.GetEqualityComparer<TKey>() ?? this.owner.ComparerProvider.GetEqualityComparer(keyShape) },
				_ => throw new NotSupportedException(),
			};
		}
		catch (NotSupportedException ex) when (typeof(TKey) == typeof(object))
		{
			throw new NotSupportedException("Serializing dictionaries or hash sets with System.Object keys is not supported. Consider using a strong-typed key with properties, or using a custom MessagePackSerializer.ComparerProvider.", ex);
		}
	}

	/// <summary>
	/// A comparer that sorts types by their inheritance hierarchy, with the most derived types first.
	/// </summary>
	private class DerivedTypeComparer : IComparer<Type>
	{
		internal static readonly DerivedTypeComparer Default = new();

		private DerivedTypeComparer()
		{
		}

		public int Compare(Type? x, Type? y)
		{
			// This proprietary implementation does not expect null values.
			Requires.NotNull(x!);
			Requires.NotNull(y!);

			return
				x.IsAssignableFrom(y) ? 1 :
				y.IsAssignableFrom(x) ? -1 :
				0;
		}
	}

	/// <summary>
	/// Captures the influence of a member on a converter.
	/// </summary>
	/// <remarks>
	/// This must be hashable/equatable so that we can cache converters based on this influence.
	/// </remarks>
	private record MemberConverterInfluence
	{
		/// <summary>
		/// Gets the type that provides the comparer, if specified by the member.
		/// </summary>
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		public Type? ComparerSource { get; init; }

		/// <summary>
		/// Gets the name of the property on <see cref="ComparerSource"/> that provides the comparer, if specified by the member.
		/// </summary>
		public string? ComparerSourceMemberName { get; init; }

		/// <summary>
		/// Gets the equality comparer for the specified type, if a comparer source is specified.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <returns>The equality comparer, if available.</returns>
		public IEqualityComparer<T>? GetEqualityComparer<T>() => this.ComparerSource is null ? null : (IEqualityComparer<T>)this.ActivateComparer();

		/// <summary>
		/// Gets the comparer for the specified type, if a comparer source is specified.
		/// </summary>
		/// <typeparam name="T">The type to be compared.</typeparam>
		/// <returns>The comparer, if available.</returns>
		public IComparer<T>? GetComparer<T>() => this.ComparerSource is null ? null : (IComparer<T>)this.ActivateComparer();

		/// <summary>
		/// Gets the comparer from the specified type and member.
		/// </summary>
		/// <returns>The comparer.</returns>
		/// <exception cref="InvalidOperationException">Thrown if something goes wrong in obtaining the comparer from the given type and member.</exception>
		private object ActivateComparer()
		{
			Verify.Operation(this.ComparerSource is not null, "Comparer source is not specified.");

			MethodInfo? propertyGetter = null;
			if (this.ComparerSourceMemberName is not null)
			{
				PropertyInfo? property = this.ComparerSource.GetProperty(this.ComparerSourceMemberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
				if (property is not { GetMethod: { } getter })
				{
					throw new InvalidOperationException($"Unable to find public property '{this.ComparerSourceMemberName}' on type '{this.ComparerSource.FullName}' with getter.");
				}

				if (getter.IsStatic)
				{
					return getter.Invoke(null, null) ?? throw CreateNullPropertyValueError();
				}

				propertyGetter = getter;
			}

			object? instance = Activator.CreateInstance(this.ComparerSource) ?? throw new InvalidOperationException($"Unable to activate {this.ComparerSource}.");

			return propertyGetter is null ? instance : propertyGetter.Invoke(instance, null) ?? CreateNullPropertyValueError();

			InvalidOperationException CreateNullPropertyValueError() => new InvalidOperationException($"{this.ComparerSource.FullName}.{this.ComparerSourceMemberName} produced a null value.");
		}
	}
}
