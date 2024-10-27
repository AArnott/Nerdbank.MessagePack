// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Nerdbank.MessagePack;

/// <summary>
/// A <see cref="TypeShapeVisitor"/> that produces <see cref="IMessagePackConverter"/> instances for each type shape it visits.
/// </summary>
/// <param name="owner">The serializer that created this instance. Usable for obtaining settings that may influence the generated converter.</param>
internal class StandardVisitor(MessagePackSerializer owner) : TypeShapeVisitor
{
	private readonly TypeDictionary converters = new();

	/// <inheritdoc/>
	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
		IConstructorShape? ctorShape = objectShape.GetConstructor();

		List<(ReadOnlyMemory<byte> RawPropertyNameString, SerializeProperty<T> Write)> serializable = new();
		List<(ReadOnlyMemory<byte> PropertyNameUtf8, DeserializeProperty<T> Read)> deserializable = new();
		foreach (IPropertyShape property in objectShape.GetProperties())
		{
			if (property.HasGetter)
			{
				// PERF: encode the property name once and reuse it for both serialization and deserialization.
				CodeGenHelpers.GetEncodedStringBytes(property.Name, out ReadOnlyMemory<byte> utf8Bytes, out ReadOnlyMemory<byte> msgpackEncoded);
				PropertyAccessors<T> accessors = (PropertyAccessors<T>)property.Accept(this)!;
				if (accessors.Serialize is not null)
				{
					serializable.Add((msgpackEncoded, accessors.Serialize));
				}

				if (accessors.Deserialize is not null)
				{
					deserializable.Add((utf8Bytes, accessors.Deserialize));
				}
			}
		}

		SpanDictionary<byte, DeserializeProperty<T>> propertyReaders = deserializable
			.ToSpanDictionary(
				p => p.PropertyNameUtf8,
				p => p.Read,
				ByteSpanEqualityComparer.Ordinal);

		MapSerializableProperties<T> serializableMap = new(serializable);
		MapDeserializableProperties<T> deserializableMap = new(propertyReaders);
		return ctorShape is not null
			? ctorShape.Accept(this, new ConstructorVisitorInputs<T>(serializableMap, deserializableMap))
			: new ObjectMapConverter<T>(serializableMap, null, null);
	}

	/// <inheritdoc/>
	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		IMessagePackConverter<TPropertyType> converter = this.GetConverter(propertyShape.PropertyType);

		SerializeProperty<TDeclaringType>? serialize = null;
		if (propertyShape.HasGetter)
		{
			Getter<TDeclaringType, TPropertyType> getter = propertyShape.GetGetter();
			serialize = (ref TDeclaringType container, ref MessagePackWriter writer) =>
			{
				TPropertyType? value = getter(ref container);
				converter.Serialize(ref writer, ref value);
			};
		}

		DeserializeProperty<TDeclaringType>? deserialize = null;
		if (propertyShape.HasSetter)
		{
			Setter<TDeclaringType, TPropertyType> setter = propertyShape.GetSetter();
			deserialize = (ref TDeclaringType container, ref MessagePackReader reader) => setter(ref container, converter.Deserialize(ref reader)!);
		}

		return new PropertyAccessors<TDeclaringType>(serialize, deserialize);
	}

	/// <inheritdoc/>
	public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null)
	{
		ConstructorVisitorInputs<TDeclaringType> inputs = (ConstructorVisitorInputs<TDeclaringType>)state!;
		if (constructorShape.ParameterCount == 0)
		{
			return new ObjectMapConverter<TDeclaringType>(inputs.Serializers, inputs.Deserializers, constructorShape.GetDefaultConstructor());
		}

		SpanDictionary<byte, DeserializeProperty<TArgumentState>> parameters = constructorShape.GetParameters()
			.Select(p => (p.Name, Deserialize: (DeserializeProperty<TArgumentState>)p.Accept(this)!))
			.ToSpanDictionary(
				p => Encoding.UTF8.GetBytes(p.Name),
				p => p.Deserialize,
				ByteSpanEqualityComparer.Ordinal);

		Func<TArgumentState> argStateCtor = constructorShape.GetArgumentStateConstructor();
		Constructor<TArgumentState, TDeclaringType> ctor = constructorShape.GetParameterizedConstructor();
		return new ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
			inputs.Serializers,
			constructorShape.GetArgumentStateConstructor(),
			constructorShape.GetParameterizedConstructor(),
			new MapDeserializableProperties<TArgumentState>(parameters));
	}

	/// <inheritdoc/>
	public override object? VisitConstructorParameter<TArgumentState, TParameterType>(IConstructorParameterShape<TArgumentState, TParameterType> parameterShape, object? state = null)
	{
		IMessagePackConverter<TParameterType> converter = owner.GetOrAddConverter(parameterShape.ParameterType);

		Setter<TArgumentState, TParameterType> setter = parameterShape.GetSetter();
		return new DeserializeProperty<TArgumentState>((ref TArgumentState state, ref MessagePackReader reader) => setter(ref state, converter.Deserialize(ref reader)!));
	}

	/// <summary>
	/// Gets or creates a converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to make convertible.</typeparam>
	/// <param name="shape">The type shape.</param>
	/// <returns>The converter.</returns>
	protected IMessagePackConverter<T> GetConverter<T>(ITypeShape<T> shape)
	{
		if (owner.TryGetConverter(out IMessagePackConverter<T>? converter))
		{
			return converter;
		}

		return this.converters.GetOrAdd<IMessagePackConverter<T>>(shape, this, box => new DelayedConverter<T>(box));
	}
}
