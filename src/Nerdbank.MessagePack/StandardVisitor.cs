// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable DuckTyping // Experimental API

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft;
using Nerdbank.MessagePack.SecureHash;
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
	object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state)
	{
		object? result = typeShape.Accept(this.OutwardVisitor, state);
		Debug.Assert(result is null or ConverterResult, $"We should not be returning raw converters, but we got one from {typeShape}.");
		return result;
	}

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(objectShape, objectShape.AttributeProvider, out ConverterResult? customConverter))
		{
			return customConverter;
		}

		// Break up significant switch/if statements into local functions or methods to reduce the amount of time spent JITting whole code blocks that won't run.
		// Local functions do not escape the declaring method's generic context, so use private methods when generic context is not required.
		return NonPrimitiveObjectHelper();
		object? NonPrimitiveObjectHelper()
		{
			IConstructorShape? ctorShape = objectShape.Constructor;

			Dictionary<string, IParameterShape>? ctorParametersByName = ctorShape is not null ? PrepareCtorParametersByName(ctorShape) : null;

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
				string propertyName = this.owner.GetSerializedPropertyName(property.Name, property.AttributeProvider);

				IParameterShape? matchingConstructorParameter = null;
				ctorParametersByName?.TryGetValue(property.Name, out matchingConstructorParameter);

				switch (property.Accept(this, matchingConstructorParameter))
				{
					case PropertyAccessors<T> accessors:
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

						break;
					case ConverterResult failure:
						if (failure.TryPrepareFailPath(property, out ConverterResult? failureResult))
						{
							return failureResult;
						}

						break;
				}
			}

			ConverterResult converter;
			if (propertyAccessors is not null)
			{
				HasPropertyAccessors();
				void HasPropertyAccessors()
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
						? (ConverterResult)ctorShape.Accept(this, inputs)!
						: ConverterResult.Ok(new ObjectArrayConverter<T>(inputs.GetJustAccessors(), unusedDataPropertyAccess, null, objectShape.Properties, this.owner.SerializeDefaultValues));
				}
			}
			else
			{
				HasNoPropertyAccessors();
				void HasNoPropertyAccessors()
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
						converter = (ConverterResult)ctorShape.Accept(this, inputs)!;
					}
					else
					{
						Func<T>? ctor = typeof(T) == typeof(object) ? (Func<T>)(object)new Func<object>(() => new object()) : null;
						converter = ConverterResult.Ok(new ObjectMapConverter<T>(
							serializableMap,
							deserializableMap,
							unusedDataPropertyAccess,
							ctor,
							objectShape.Properties,
							this.owner.SerializeDefaultValues));
					}
				}
			}

			// Test IsValueType before considering unions so that the native compiler
			// does not have to generate a SubTypes<T> for value types which will never be used.
			if (converter.Success && !typeof(T).IsValueType)
			{
				if (this.owner.TryGetDynamicUnion(objectShape.Type, out DerivedTypeUnion? union) && !union.Disabled)
				{
					converter = union switch
					{
						IDerivedTypeMapping mapping => this.CreateSubTypes(objectShape.Type, (MessagePackConverter<T>)converter.Value, mapping).MapResult(st => new UnionConverter<T>((MessagePackConverter<T>)converter.Value, st)),
						DerivedTypeDuckTyping duckTyping => this.CreateDuckTypingUnionConverter<T>(duckTyping, (MessagePackConverter<T>)converter.Value),
						_ => ConverterResult.Err(new NotSupportedException($"Unrecognized union type: {union.GetType().Name}")),
					};
				}
			}

			return converter;
		}
	}

	/// <inheritdoc/>
	public override object? VisitUnion<TUnion>(IUnionTypeShape<TUnion> unionShape, object? state = null)
	{
		ConverterResult baseTypeConverter = (ConverterResult)unionShape.BaseType.Accept(this)!;
		if (baseTypeConverter.TryPrepareFailPath("union base type", out ConverterResult? failureResult))
		{
			return failureResult;
		}

		if (baseTypeConverter.Value is UnionConverter<TUnion>)
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
				IDerivedTypeMapping mapping => this.CreateSubTypes(baseType, (MessagePackConverter<TUnion>)baseTypeConverter.Value, mapping).MapResult(st => new UnionConverter<TUnion>((MessagePackConverter<TUnion>)baseTypeConverter.Value, st)),
				DerivedTypeDuckTyping duckTyping => this.CreateDuckTypingUnionConverter(duckTyping, (MessagePackConverter<TUnion>)baseTypeConverter.Value),
				_ => ConverterResult.Err(new NotSupportedException($"Unrecognized union type: {union.GetType().Name}")),
			};
		}

		Getter<TUnion, int> getUnionCaseIndex = unionShape.GetGetUnionCaseIndex();
		Dictionary<int, MessagePackConverter> deserializerByIntAlias = new(unionShape.UnionCases.Count);
		List<(DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)> serializers = new(unionShape.UnionCases.Count);
		foreach (IUnionCaseShape unionCase in unionShape.UnionCases)
		{
			bool useTag = unionCase.IsTagSpecified || this.owner.PerfOverSchemaStability;
			DerivedTypeIdentifier alias = useTag ? new(unionCase.Tag) : new(unionCase.Name);
			ConverterResult caseConverter = (ConverterResult)unionCase.Accept(this, null)!;
			if (caseConverter.TryPrepareFailPath(unionCase, out failureResult))
			{
				return failureResult;
			}

			deserializerByIntAlias.Add(unionCase.Tag, caseConverter.Value);
			serializers.Add((alias, caseConverter.Value, unionCase.UnionCaseType));
		}

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

		return ConverterResult.Ok(new UnionConverter<TUnion>((MessagePackConverter<TUnion>)baseTypeConverter.Value, subTypes));
	}

	/// <inheritdoc/>
	public override object? VisitUnionCase<TUnionCase, TUnion>(IUnionCaseShape<TUnionCase, TUnion> unionCaseShape, object? state = null)
	{
		// NB: don't use the cached converter for TUnionCase, as it might equal TUnion.
		var caseConverter = (ConverterResult)unionCaseShape.UnionCaseType.Accept(this)!;
		if (caseConverter.TryPrepareFailPath(unionCaseShape, out ConverterResult? failureResult))
		{
			return failureResult;
		}

		return ConverterResult.Ok(new UnionCaseConverter<TUnionCase, TUnion>((MessagePackConverter<TUnionCase>)caseConverter.Value, unionCaseShape.Marshaler));
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		IParameterShape? constructorParameterShape = (IParameterShape?)state;

		ConverterResult converter = this.GetConverterForMemberOrParameter(propertyShape.PropertyType, propertyShape.AttributeProvider);

		(SerializeProperty<TDeclaringType>, SerializePropertyAsync<TDeclaringType>)? msgpackWriters = null;
		Func<TDeclaringType, bool>? shouldSerialize = null;

		// We'll break up the significant conditioned blocks into local functions to reduce the amount of time spent JITting whole code blocks that won't run.
		if (propertyShape.HasGetter)
		{
			GetterHelper();
			void GetterHelper()
			{
				Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
				EqualityComparer<TPropertyType> eq = EqualityComparer<TPropertyType>.Default;

				if (this.owner.SerializeDefaultValues != SerializeDefaultValuesPolicy.Always)
				{
					NotAlwaysHelper();
					void NotAlwaysHelper()
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
				}

				SerializeProperty<TDeclaringType> serialize = (in TDeclaringType container, ref MessagePackWriter writer, SerializationContext context) =>
				{
					// Workaround https://github.com/eiriktsarpalis/PolyType/issues/46.
					// We get significantly improved usability in the API if we use the `in` modifier on the Serialize method
					// instead of `ref`. And since serialization should fundamentally be a read-only operation, this *should* be safe.
					TPropertyType? value = getter(ref Unsafe.AsRef(in container));
					try
					{
						((MessagePackConverter<TPropertyType>)converter.ValueOrThrow).Write(ref writer, value, context);
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
						await ((MessagePackConverter<TPropertyType>)converter.ValueOrThrow).WriteAsync(writer, getter(ref container), context).ConfigureAwait(false);
					}
					catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
					{
						throw new MessagePackSerializationException(CreateWriteFailMessage(propertyShape), ex);
					}
				};
				msgpackWriters = (serialize, serializeAsync);
			}
		}

		bool suppressIfNoConstructorParameter = true;
		(DeserializeProperty<TDeclaringType>, DeserializePropertyAsync<TDeclaringType>)? msgpackReaders = null;
		if (propertyShape.HasSetter)
		{
			SetterHelper();
			void SetterHelper()
			{
				Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
				DeserializeProperty<TDeclaringType> deserialize = (ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context) =>
				{
					try
					{
						setter(ref container, ((MessagePackConverter<TPropertyType>)converter.ValueOrThrow).Read(ref reader, context)!);
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
						setter(ref container, (await ((MessagePackConverter<TPropertyType>)converter.ValueOrThrow).ReadAsync(reader, context).ConfigureAwait(false))!);
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
		}
		else if (propertyShape.HasGetter && converter.Value is IDeserializeInto<TPropertyType> inflater)
		{
			CollectionHelper();
			void CollectionHelper()
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
		}

		if (suppressIfNoConstructorParameter && constructorParameterShape is null)
		{
			return null;
		}

		if (!converter.Success)
		{
			return converter;
		}

		return new PropertyAccessors<TDeclaringType>(msgpackWriters, msgpackReaders, converter.Value, shouldSerialize, propertyShape);
	}

	/// <inheritdoc/>
	public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null)
	{
		// Break up significant switch/if statements into local functions to reduce the amount of time spent JITting whole code blocks that won't run.
		return state switch
		{
			MapConstructorVisitorInputs<TDeclaringType> inputs => MapHelper(inputs),
			ArrayConstructorVisitorInputs<TDeclaringType> inputs => ArrayHelper(inputs),
			_ => throw new NotSupportedException("Unsupported state."),
		};

		// Use proper methods instead of local functions when we don't need TArgumentState
		// so that the JIT can reuse native code for reference types.
		object? MapHelper(MapConstructorVisitorInputs<TDeclaringType> inputs) => constructorShape.Parameters.Count == 0
			? this.VisitConstructor_MapHelperEmptyCtor(constructorShape, inputs, constructorShape.GetDefaultConstructor())
			: MapHelperNonEmptyCtor(inputs);

		object? ArrayHelper(ArrayConstructorVisitorInputs<TDeclaringType> inputs) => constructorShape.Parameters.Count == 0
			? this.VisitConstructor_ArrayHelperEmptyCtor(constructorShape, inputs, constructorShape.GetDefaultConstructor())
			: ArrayHelperNonEmptyCtor(inputs);

		object? MapHelperNonEmptyCtor(MapConstructorVisitorInputs<TDeclaringType> inputs)
		{
			List<SerializableProperty<TDeclaringType>> propertySerializers = inputs.Serializers.Properties.Span.ToList();

			var spanDictContent = new KeyValuePair<ReadOnlyMemory<byte>, DeserializableProperty<TArgumentState>>[inputs.ParametersByName.Count];
			int i = 0;
			foreach (KeyValuePair<string, IParameterShape> p in inputs.ParametersByName)
			{
				ICustomAttributeProvider? propertyAttributeProvider = constructorShape.DeclaringType.Properties.FirstOrDefault(prop => prop.Name == p.Value.Name)?.AttributeProvider;
				object parameterResult = p.Value.Accept(this, constructorShape)!;
				if (parameterResult is ConverterResult converterResult && converterResult.TryPrepareFailPath(p.Value, out ConverterResult? failureResult))
				{
					return failureResult;
				}

				var prop = (DeserializableProperty<TArgumentState>)parameterResult;
				string name = this.owner.GetSerializedPropertyName(p.Value.Name, propertyAttributeProvider);
				spanDictContent[i++] = new(Encoding.UTF8.GetBytes(name), prop);
			}

			SpanDictionary<byte, DeserializableProperty<TArgumentState>> parameters = new(spanDictContent, ByteSpanEqualityComparer.Ordinal);

			MapSerializableProperties<TDeclaringType> serializeable = inputs.Serializers;
			serializeable.Properties = propertySerializers.ToArray();
			return ConverterResult.Ok(new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
				serializeable,
				constructorShape.GetArgumentStateConstructor(),
				inputs.UnusedDataProperty,
				constructorShape.GetParameterizedConstructor(),
				new MapDeserializableProperties<TArgumentState>(parameters),
				constructorShape.Parameters,
				this.owner.SerializeDefaultValues,
				this.owner.DeserializeDefaultValues));
		}

		object? ArrayHelperNonEmptyCtor(ArrayConstructorVisitorInputs<TDeclaringType> inputs)
		{
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

				object parameterResult = parameter.Accept(this, constructorShape)!;
				if (parameterResult is ConverterResult converterResult && converterResult.TryPrepareFailPath(parameter, out ConverterResult? failureResult))
				{
					return failureResult;
				}

				parameters[index] = (DeserializableProperty<TArgumentState>)parameterResult;
			}

			return ConverterResult.Ok(new ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
				inputs.GetJustAccessors(),
				inputs.UnusedDataProperty,
				constructorShape.GetArgumentStateConstructor(),
				constructorShape.GetParameterizedConstructor(),
				parameters,
				constructorShape.Parameters,
				this.owner.SerializeDefaultValues,
				this.owner.DeserializeDefaultValues));
		}
	}

	/// <inheritdoc/>
	public override object? VisitParameter<TArgumentState, TParameterType>(IParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		IConstructorShape constructorShape = (IConstructorShape)(state ?? throw new ArgumentNullException(nameof(state)));

		ConverterResult converter = this.GetConverterForMemberOrParameter(parameterShape.ParameterType, parameterShape.AttributeProvider);
		if (!converter.Success)
		{
			return converter;
		}

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();

		DeserializeProperty<TArgumentState> read;
		DeserializePropertyAsync<TArgumentState> readAsync;
		bool throwOnNull =
			(this.owner.DeserializeDefaultValues & DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties) != DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties
			&& parameterShape.IsNonNullable
			&& !typeof(TParameterType).IsValueType;

		// We use local functions to avoid JITting both paths of the if/else for a given parameter.
		if (throwOnNull)
		{
			ThrowingHelper();
			void ThrowingHelper()
			{
				read = (ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) =>
				{
					try
					{
						ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
						setter(ref state, ((MessagePackConverter<TParameterType>)converter.Value).Read(ref reader, context) ?? throw NewDisallowedDeserializedNullValueException(parameterShape));
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
						setter(ref state, (await ((MessagePackConverter<TParameterType>)converter.Value).ReadAsync(reader, context).ConfigureAwait(false)) ?? throw NewDisallowedDeserializedNullValueException(parameterShape));
						return state;
					}
					catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
					{
						throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
					}
				};
			}
		}
		else
		{
			NonThrowingHelper();
			void NonThrowingHelper()
			{
				read = (ref TArgumentState state, ref MessagePackReader reader, SerializationContext context) =>
				{
					try
					{
						ThrowIfAlreadyAssigned(state, parameterShape.Position, parameterShape.Name);
						setter(ref state, ((MessagePackConverter<TParameterType>)converter.Value).Read(ref reader, context)!);
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
						setter(ref state, (await ((MessagePackConverter<TParameterType>)converter.Value).ReadAsync(reader, context).ConfigureAwait(false))!);
						return state;
					}
					catch (Exception ex) when (MessagePackConverter.ShouldWrapSerializationException(ex, context.CancellationToken))
					{
						throw new MessagePackSerializationException(CreateReadFailMessage(parameterShape, constructorShape), ex);
					}
				};
			}
		}

		return new DeserializableProperty<TArgumentState>(
			parameterShape.Name,
			StringEncoding.UTF8.GetBytes(parameterShape.Name),
			read,
			readAsync,
			(MessagePackConverter<TParameterType>)converter.Value,
			parameterShape.Position);
	}

	/// <inheritdoc/>
	public override object? VisitOptional<TOptional, TElement>(IOptionalTypeShape<TOptional, TElement> optionalShape, object? state = null)
	{
		ConverterResult converter = this.GetConverter(optionalShape.ElementType);
		if (converter.TryPrepareFailPath(optionalShape, out ConverterResult? failure))
		{
			return failure;
		}

		return ConverterResult.Ok(new OptionalConverter<TOptional, TElement>((MessagePackConverter<TElement>)converter.Value, optionalShape.GetDeconstructor(), optionalShape.GetNoneConstructor(), optionalShape.GetSomeConstructor()));
	}

	/// <inheritdoc/>
	public override object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(dictionaryShape, dictionaryShape.AttributeProvider, out ConverterResult? customConverter))
		{
			return customConverter;
		}

		MemberConverterInfluence? memberInfluence = state as MemberConverterInfluence;

		// Serialization functions.
		ConverterResult keyConverterResult = this.GetConverter(dictionaryShape.KeyType);
		ConverterResult valueConverterResult = this.GetConverter(dictionaryShape.ValueType);
		Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getReadable = dictionaryShape.GetGetDictionary();

		if (keyConverterResult.TryPrepareFailPath("key", out ConverterResult? keyFailure))
		{
			return keyFailure;
		}

		if (valueConverterResult.TryPrepareFailPath("value", out ConverterResult? valueFailure))
		{
			return valueFailure;
		}

		var keyConverter = (MessagePackConverter<TKey>)keyConverterResult.Value;
		var valueConverter = (MessagePackConverter<TValue>)valueConverterResult.Value;

		// Deserialization functions.
		return dictionaryShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => ConverterResult.Ok(new DictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter)),
			CollectionConstructionStrategy.Mutable => ConverterResult.Ok(new MutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetInserter(DictionaryInsertionMode.Throw), dictionaryShape.GetDefaultConstructor(), this.GetCollectionOptions(dictionaryShape, memberInfluence))),
			CollectionConstructionStrategy.Parameterized => ConverterResult.Ok(new ImmutableDictionaryConverter<TDictionary, TKey, TValue>(getReadable, keyConverter, valueConverter, dictionaryShape.GetParameterizedConstructor(), this.GetCollectionOptions(dictionaryShape, memberInfluence))),
			_ => ConverterResult.Err(new NotSupportedException($"Unrecognized dictionary pattern: {typeof(TDictionary).Name}")),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(enumerableShape, enumerableShape.AttributeProvider, out ConverterResult? customConverter))
		{
			return customConverter;
		}

		MemberConverterInfluence? memberInfluence = state as MemberConverterInfluence;

		// Serialization functions.
		ConverterResult elementConverterResult = this.GetConverter(enumerableShape.ElementType);
		if (elementConverterResult.TryPrepareFailPath("element", out ConverterResult? elementFailure))
		{
			return elementFailure;
		}

		var elementConverter = (MessagePackConverter<TElement>)elementConverterResult.Value;

		if (enumerableShape.Type.IsArray)
		{
			MessagePackConverter<TEnumerable>? converter;
			if (enumerableShape.Rank > 1)
			{
#if NET
				return this.owner.MultiDimensionalArrayFormat switch
				{
					MultiDimensionalArrayFormat.Nested => ConverterResult.Ok(new ArrayWithNestedDimensionsConverter<TEnumerable, TElement>(elementConverter, enumerableShape.Rank)),
					MultiDimensionalArrayFormat.Flat => ConverterResult.Ok(new ArrayWithFlattenedDimensionsConverter<TEnumerable, TElement>(elementConverter)),
					_ => ConverterResult.Err(new NotSupportedException()),
				};
#else
				return ConverterResult.Err(new PlatformNotSupportedException("This functionality is only supported on .NET."));
#endif
			}
#if NET
			else if (!this.owner.DisableHardwareAcceleration &&
				enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Parameterized &&
				HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out converter))
			{
				return ConverterResult.Ok(converter);
			}
#endif
			else if (enumerableShape.ConstructionStrategy == CollectionConstructionStrategy.Parameterized &&
				ArraysOfPrimitivesConverters.TryGetConverter(enumerableShape.GetGetEnumerable(), enumerableShape.GetParameterizedConstructor(), out converter))
			{
				return ConverterResult.Ok(converter);
			}
			else
			{
				return ConverterResult.Ok(new ArrayConverter<TElement>(elementConverter));
			}
		}

		Func<TEnumerable, IEnumerable<TElement>>? getEnumerable = enumerableShape.IsAsyncEnumerable ? null : enumerableShape.GetGetEnumerable();
		return enumerableShape.ConstructionStrategy switch
		{
			CollectionConstructionStrategy.None => ConverterResult.Ok(new EnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter)),
			CollectionConstructionStrategy.Mutable => ConverterResult.Ok(new MutableEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetAppender(), enumerableShape.GetDefaultConstructor(), this.GetCollectionOptions(enumerableShape, memberInfluence))),
#if NET
			CollectionConstructionStrategy.Parameterized when !this.owner.DisableHardwareAcceleration && HardwareAccelerated.TryGetConverter<TEnumerable, TElement>(out MessagePackConverter<TEnumerable>? converter) => ConverterResult.Ok(converter),
#endif
			CollectionConstructionStrategy.Parameterized when getEnumerable is not null && ArraysOfPrimitivesConverters.TryGetConverter(getEnumerable, enumerableShape.GetParameterizedConstructor(), out MessagePackConverter<TEnumerable>? converter) => ConverterResult.Ok(converter),
			CollectionConstructionStrategy.Parameterized => ConverterResult.Ok(new SpanEnumerableConverter<TEnumerable, TElement>(getEnumerable, elementConverter, enumerableShape.GetParameterizedConstructor(), this.GetCollectionOptions(enumerableShape, memberInfluence))),
			_ => ConverterResult.Err(new NotSupportedException($"Unrecognized enumerable pattern: {typeof(TEnumerable).Name}")),
		};
	}

	/// <inheritdoc/>
	public override object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumShape, object? state = null)
	{
		if (this.TryGetCustomOrPrimitiveConverter(enumShape, enumShape.AttributeProvider, out ConverterResult? customConverter))
		{
			return customConverter;
		}

		ConverterResult underlyingConverter = this.GetConverter(enumShape.UnderlyingType);
		if (underlyingConverter.TryPrepareFailPath(enumShape, out ConverterResult? failure))
		{
			return failure;
		}

		return ConverterResult.Ok(this.owner.SerializeEnumValuesByName
			? new EnumAsStringConverter<TEnum, TUnderlying>((MessagePackConverter<TUnderlying>)underlyingConverter.Value, enumShape.Members)
			: new EnumAsOrdinalConverter<TEnum, TUnderlying>((MessagePackConverter<TUnderlying>)underlyingConverter.Value));
	}

	/// <inheritdoc/>
	public override object? VisitSurrogate<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> surrogateShape, object? state = null)
	{
		ConverterResult surrogateConverter = this.GetConverter(surrogateShape.SurrogateType, state: state);
		if (surrogateConverter.TryPrepareFailPath(surrogateShape, out ConverterResult? failure))
		{
			return failure;
		}

		return ConverterResult.Ok(new SurrogateConverter<T, TSurrogate>(surrogateShape, (MessagePackConverter<TSurrogate>)surrogateConverter.Value));
	}

	/// <inheritdoc/>
	public override object? VisitFunction<TFunction, TArgumentState, TResult>(IFunctionTypeShape<TFunction, TArgumentState, TResult> functionShape, object? state = null)
		=> ConverterResult.Err("Delegate types cannot be serialized.");

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
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
	protected ConverterResult GetConverter(ITypeShape shape, ICustomAttributeProvider? memberAttributes = null, object? state = null)
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
				return (ConverterResult)shape.Invoke(this, memberInfluence)!;
			}
		}

		return (ConverterResult)this.context.GetOrAdd(shape, state)!;
	}

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <param name="shape">The type shape.</param>
	/// <param name="state">An optional state object to pass to the converter.</param>
	/// <returns>The converter.</returns>
	protected ConverterResult GetConverterByAccept(ITypeShape shape, object? state = null) => (ConverterResult)shape.Accept(this.OutwardVisitor, state)!;

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

	private static Dictionary<string, IParameterShape> PrepareCtorParametersByName(IConstructorShape ctorShape)
	{
		Dictionary<string, IParameterShape> ctorParametersByName = new(StringComparer.Ordinal);
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

		return ctorParametersByName;
	}

	private static Exception NewDisallowedDeserializedNullValueException(IParameterShape parameter) => new MessagePackSerializationException($"The parameter '{parameter.Name}' is non-nullable, but the deserialized value was null.") { Code = MessagePackSerializationException.ErrorCode.DisallowedNullValue };

	private static string CreateReadFailMessage(IParameterShape parameterShape, IConstructorShape constructorShape) => $"Failed to deserialize value for '{parameterShape.Name}' parameter on {constructorShape.DeclaringType.Type.FullName}.";

	private static string CreateReadFailMessage(IPropertyShape propertyShape) => $"Failed to deserialize '{propertyShape.Name}' property on {propertyShape.DeclaringType.Type.FullName}.";

	private static string CreateWriteFailMessage(IPropertyShape propertyShape) => $"Failed to serialize '{propertyShape.Name}' property on {propertyShape.DeclaringType.Type.FullName}.";

	private object? VisitConstructor_ArrayHelperEmptyCtor<TDeclaringType>(IConstructorShape constructorShape, ArrayConstructorVisitorInputs<TDeclaringType> inputs, Func<TDeclaringType> defaultConstructor)
	{
		return ConverterResult.Ok(new ObjectArrayConverter<TDeclaringType>(inputs.GetJustAccessors(), inputs.UnusedDataProperty, defaultConstructor, constructorShape.DeclaringType.Properties, this.owner.SerializeDefaultValues));
	}

	private object? VisitConstructor_MapHelperEmptyCtor<TDeclaringType>(IConstructorShape constructorShape, MapConstructorVisitorInputs<TDeclaringType> inputs, Func<TDeclaringType> defaultConstructor)
	{
		return ConverterResult.Ok(new ObjectMapConverter<TDeclaringType>(
			inputs.Serializers,
			inputs.Deserializers,
			inputs.UnusedDataProperty,
			defaultConstructor,
			constructorShape.DeclaringType.Properties,
			this.owner.SerializeDefaultValues));
	}

	private Result<SubTypes<TBaseType>, VisitorError> CreateSubTypes<TBaseType>(Type baseType, MessagePackConverter<TBaseType> baseTypeConverter, IDerivedTypeMapping mapping)
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
			MessagePackConverter converter;
			if (shape.Type == baseType)
			{
				converter = baseTypeConverter;
			}
			else
			{
				ConverterResult subtypeConverter = this.GetConverterByAccept(shape);
				if (subtypeConverter.TryPrepareFailPath(pair.Value, out ConverterResult? failureResult))
				{
					return failureResult.Error!;
				}

				converter = ((IMessagePackConverterInternal)subtypeConverter.Value).UnwrapReferencePreservation();
			}

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
	private ConverterResult CreateDuckTypingUnionConverter<TBase>(DerivedTypeDuckTyping duckTyping, MessagePackConverter<TBase> baseTypeConverter)
	{
		// Create converters for each member type
		Dictionary<Type, MessagePackConverter> convertersByType = new(duckTyping.DerivedShapes.Length);
		foreach (ITypeShape shape in duckTyping.DerivedShapes.Span)
		{
			if (!typeof(TBase).IsAssignableFrom(shape.Type))
			{
				throw new ArgumentException($"Type '{shape.Type}' is not assignable to base type '{typeof(TBase)}'.", nameof(duckTyping));
			}

			ConverterResult converter = this.GetConverterByAccept(shape);
			if (converter.TryPrepareFailPath(shape, out ConverterResult? failureResult))
			{
				return failureResult;
			}

			convertersByType[shape.Type] = converter.Value;
		}

		return ConverterResult.Ok(new ShapeBasedUnionConverter<TBase>(baseTypeConverter, duckTyping, convertersByType));
	}

	/// <summary>
	/// Retrieves a converter for the given type shape from runtime-supplied user sources, primitive converters, or attribute-specified converters.
	/// </summary>
	/// <typeparam name="T">The type for which a converter is required.</typeparam>
	/// <param name="typeShape">The shape for the type to be converted.</param>
	/// <param name="attributeProvider"><inheritdoc cref="TryGetConverterFromAttribute" path="/param[@name='attributeProvider']"/></param>
	/// <param name="converter">Receives the converter if one is found.</param>
	/// <returns>A value indicating whether a match was found.</returns>
	private bool TryGetCustomOrPrimitiveConverter<T>(ITypeShape<T> typeShape, ICustomAttributeProvider? attributeProvider, [NotNullWhen(true)] out ConverterResult? converter)
	{
		// Check if the type has a custom converter.
		if (this.owner.TryGetRuntimeProfferedConverter(typeShape, out MessagePackConverter? proferredConverter))
		{
			converter = ConverterResult.Ok(proferredConverter);
			return true;
		}

		if (this.owner.InternStrings && typeof(T) == typeof(string))
		{
			converter = ConverterResult.Ok((MessagePackConverter<T>)(object)(this.owner.PreserveReferences != ReferencePreservationMode.Off ? ReferencePreservingInterningStringConverter : InterningStringConverter));
			return true;
		}

		// Check if the type has a built-in converter.
		if (PrimitiveConverterLookup.TryGetPrimitiveConverter(this.owner.PreserveReferences, out MessagePackConverter<T>? primitiveConverter))
		{
			converter = ConverterResult.Ok(primitiveConverter);
			return true;
		}

		return this.TryGetConverterFromAttribute(typeShape, attributeProvider, out converter);
	}

	private ConverterResult GetConverterForMemberOrParameter(ITypeShape typeShape, ICustomAttributeProvider? attributeProvider)
	{
		try
		{
			return this.TryGetConverterFromAttribute(typeShape, attributeProvider, out ConverterResult? converter)
				? converter
				: this.GetConverter(typeShape, attributeProvider);
		}
		catch (Exception ex)
		{
			return ConverterResult.Err(ex);
		}
	}

	/// <summary>
	/// Activates a converter for the given shape if a <see cref="MessagePackConverterAttribute"/> is present on the type or member.
	/// </summary>
	/// <param name="typeShape">The shape of the type to be serialized.</param>
	/// <param name="attributeProvider">
	/// The source of the attributes.
	/// This will typically be the attributes on the type itself, but may be the attributes on the requesting property or parameter.
	/// </param>
	/// <param name="converter">Receives the converter, if applicable.</param>
	/// <returns>A value indicating whether a converter was found.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if the prescribed converter has no default constructor.</exception>
	private bool TryGetConverterFromAttribute(ITypeShape typeShape, ICustomAttributeProvider? attributeProvider, [NotNullWhen(true)] out ConverterResult? converter)
	{
		if (attributeProvider?.GetCustomAttribute<MessagePackConverterAttribute>() is not { } customConverterAttribute)
		{
			converter = null;
			return false;
		}

		Type converterType = customConverterAttribute.ConverterType;
		if ((typeShape.GetAssociatedTypeShape(converterType) as IObjectTypeShape)?.GetDefaultConstructor() is Func<object> converterFactory)
		{
			MessagePackConverter intermediateConverter = (MessagePackConverter)converterFactory();
			if (this.owner.PreserveReferences != ReferencePreservationMode.Off)
			{
				intermediateConverter = ((IMessagePackConverterInternal)intermediateConverter).WrapWithReferencePreservation();
			}

			converter = ConverterResult.Ok(intermediateConverter);
			return true;
		}

		if (converterType.GetConstructor(Type.EmptyTypes) is not ConstructorInfo ctor)
		{
			throw new MessagePackSerializationException($"{typeShape.Type.FullName} has {typeof(MessagePackConverterAttribute)} that refers to {customConverterAttribute.ConverterType.FullName} but that converter has no default constructor.");
		}

		converter = ConverterResult.Ok((MessagePackConverter)ctor.Invoke(Array.Empty<object?>()));
		return true;
	}

	private Result<CollectionConstructionOptions<TKey>, VisitorError> GetCollectionOptions<TDictionary, TKey, TValue>(IDictionaryTypeShape<TDictionary, TKey, TValue> dictionaryShape, MemberConverterInfluence? memberInfluence)
		where TKey : notnull
		=> this.GetCollectionOptions(dictionaryShape.KeyType, dictionaryShape.SupportedComparer, memberInfluence);

	private Result<CollectionConstructionOptions<TElement>, VisitorError> GetCollectionOptions<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, MemberConverterInfluence? memberInfluence)
		=> this.GetCollectionOptions(enumerableShape.ElementType, enumerableShape.SupportedComparer, memberInfluence);

	private Result<CollectionConstructionOptions<TKey>, VisitorError> GetCollectionOptions<TKey>(ITypeShape<TKey> keyShape, CollectionComparerOptions requiredComparer, MemberConverterInfluence? memberInfluence)
	{
		if (this.owner.ComparerProvider is null)
		{
			return default(CollectionConstructionOptions<TKey>);
		}

		try
		{
			return requiredComparer switch
			{
				CollectionComparerOptions.None => default(CollectionConstructionOptions<TKey>),
				CollectionComparerOptions.Comparer => new CollectionConstructionOptions<TKey> { Comparer = memberInfluence?.GetComparer<TKey>() ?? this.owner.ComparerProvider.GetComparer(keyShape) },
				CollectionComparerOptions.EqualityComparer => new CollectionConstructionOptions<TKey> { EqualityComparer = memberInfluence?.GetEqualityComparer<TKey>() ?? this.owner.ComparerProvider.GetEqualityComparer(keyShape) },
				_ => new VisitorError(new NotSupportedException()),
			};
		}
		catch (Exception ex) when (SecureVisitor.TryGetEmptyTypeFailure(ex.GetBaseException(), out Type? emptyType))
		{
			return new VisitorError(new NotSupportedException($"Serializing dictionaries or hash sets with keys that are or contain empty types is not supported. {emptyType.FullName} is an empty type. Consider using a strong-typed key with properties, or using a custom (or null) MessagePackSerializer.ComparerProvider.", ex));
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
