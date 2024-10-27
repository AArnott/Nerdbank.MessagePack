// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

internal class StandardVisitor(MessagePackSerializer owner) : TypeShapeVisitor
{
	private readonly TypeDictionary converter = new();

	public override object? VisitObject<T>(IObjectTypeShape<T> objectShape, object? state = null)
	{
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

		Func<T> ctor = (Func<T>)objectShape.GetConstructor().Accept(this);
		return ctor is null ? throw new NotSupportedException($"{objectShape.Type.Name} is serialize-only")
			: new ObjectMapConverter<T>(new MapSerializableProperties<T>(serializable), new MapDeserializableProperties<T>(propertyReaders), ctor);
	}

	public override object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
	{
		MessagePackConverter<TPropertyType> converter = owner.GetOrAddConverter<TPropertyType>(propertyShape.PropertyType);

		SerializeProperty<TDeclaringType>? serialize = null;
		if (propertyShape.HasGetter)
		{
			serialize = (ref TDeclaringType container, ref MessagePackWriter writer) =>
			{
				var value = propertyShape.GetGetter()(ref container);
				converter.Serialize(ref writer, ref value);
			};
		}

		DeserializeProperty<TDeclaringType>? deserialize = propertyShape.HasSetter ? (ref TDeclaringType container, ref MessagePackReader reader) => propertyShape.GetSetter()(ref container, converter.Deserialize(ref reader)!) : null;

		return new PropertyAccessors<TDeclaringType>(serialize, deserialize);
	}

	public override object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructorShape, object? state = null)
	{
		if (constructorShape.GetDefaultConstructor() is Func<TDeclaringType> defaultCtor)
		{
			return defaultCtor;
		}

		throw new NotSupportedException();
	}
}
