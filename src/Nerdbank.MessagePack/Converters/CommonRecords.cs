// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A delegate that can read a property from a data type and serialize it to a <see cref="MessagePackWriter"/>.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be read.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="writer">The means by which msgpack should be written.</param>
/// <param name="context"><inheritdoc cref="IMessagePackConverter{T}.Serialize" path="/param[@name='context']"/></param>
internal delegate void SerializeProperty<TDeclaringType>(ref TDeclaringType container, ref MessagePackWriter writer, SerializationContext context);

/// <summary>
/// A delegate that can deserialize the value of a property from a <see cref="MessagePackReader"/> and assign it to a data type.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be initialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="reader">The means by which msgpack should be read.</param>
/// <param name="context"><inheritdoc cref="IMessagePackConverter{T}.Deserialize" path="/param[@name='context']"/></param>
internal delegate void DeserializeProperty<TDeclaringType>(ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context);

/// <summary>
/// A map of serializable properties.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that contains the properties to be serialized.</typeparam>
/// <param name="Properties">The list of serializable properties, including the msgpack encoding of the property name and the delegate to serialize that property.</param>
internal record struct MapSerializableProperties<TDeclaringType>(List<(ReadOnlyMemory<byte> RawPropertyNameString, SerializeProperty<TDeclaringType> Write)> Properties);

/// <summary>
/// A map of deserializable properties.
/// </summary>
/// <typeparam name="T">The data type that contains properties to be deserialized.</typeparam>
/// <param name="Readers">The map of deserializable properties, keyed by the UTF-8 encoding of the property name.</param>
internal record struct MapDeserializableProperties<T>(SpanDictionary<byte, DeserializeProperty<T>> Readers);

/// <summary>
/// Encapsulates serializing accessors for a particular property of some data type.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that declares the property that these accessors can serialize and deserialize values for.</typeparam>
/// <param name="Serialize">A delegate that serializes the property.</param>
/// <param name="Deserialize">A delegate that can initialize the property with a value deserialized from msgpack.</param>
internal record struct PropertyAccessors<TDeclaringType>(SerializeProperty<TDeclaringType>? Serialize, DeserializeProperty<TDeclaringType>? Deserialize);

/// <summary>
/// Encapsulates the data passed through <see cref="ITypeShapeVisitor.VisitConstructor{TDeclaringType, TArgumentState}(IConstructorShape{TDeclaringType, TArgumentState}, object?)"/> state arguments.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose constructor is to be visited.</typeparam>
/// <param name="Serializers">Serializable properties on the data type.</param>
/// <param name="Deserializers">Deserializable properties on the data type.</param>
internal record ConstructorVisitorInputs<TDeclaringType>(MapSerializableProperties<TDeclaringType> Serializers, MapDeserializableProperties<TDeclaringType> Deserializers);
