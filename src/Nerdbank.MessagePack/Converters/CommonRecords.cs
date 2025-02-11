// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable NBMsgPackAsync

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// A delegate that can read a property from a data type and serialize it to a <see cref="Writer"/>.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be read.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="writer">The means by which msgpack should be written.</param>
/// <param name="context"><inheritdoc cref="Converter{T}.Write" path="/param[@name='context']"/></param>
internal delegate void SerializeProperty<TDeclaringType>(in TDeclaringType container, ref Writer writer, SerializationContext context);

/// <summary>
/// A delegate that can asynchronously serialize a property to a <see cref="AsyncWriter"/>.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be serialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="writer">The means by which msgpack should be written.</param>
/// <param name="context">The serialization context.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
[Experimental("NBMsgPackAsync")]
internal delegate ValueTask SerializePropertyAsync<TDeclaringType>(TDeclaringType container, AsyncWriter writer, SerializationContext context);

/// <summary>
/// A delegate that can deserialize a value from a <see cref="Reader"/> and assign it to a property.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be initialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="reader">The means by which msgpack should be read.</param>
/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
internal delegate void DeserializeProperty<TDeclaringType>(ref TDeclaringType container, ref Reader reader, SerializationContext context);

/// <summary>
/// A delegate that can asynchronously deserialize the value from a <see cref="AsyncReader"/> and assign it to a property.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be initialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="reader">The means by which msgpack should be read.</param>
/// <param name="context"><inheritdoc cref="Converter{T}.Read" path="/param[@name='context']"/></param>
/// <returns>The <paramref name="container"/>, with the property initialized. This is useful when <typeparamref name="TDeclaringType"/> is a struct.</returns>
[Experimental("NBMsgPackAsync")]
internal delegate ValueTask<TDeclaringType> DeserializePropertyAsync<TDeclaringType>(TDeclaringType container, AsyncReader reader, SerializationContext context);

/// <summary>
/// A map of serializable properties.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that contains the properties to be serialized.</typeparam>
/// <param name="Properties">The list of serializable properties, including the msgpack encoding of the property name and the delegate to serialize that property.</param>
internal record struct MapSerializableProperties<TDeclaringType>(ReadOnlyMemory<SerializableProperty<TDeclaringType>> Properties);

/// <summary>
/// Contains the data necessary for a converter to serialize the value of a particular property.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property to be serialized.</typeparam>
/// <param name="Name">The property name.</param>
/// <param name="RawPropertyNameString">The entire msgpack encoding of the property name, including the string header.</param>
/// <param name="Write">A delegate that synchronously serializes the value of the property.</param>
/// <param name="WriteAsync">A delegate that asynchonously serializes the value of the property.</param>
/// <param name="PreferAsyncSerialization"><inheritdoc cref="Converter.PreferAsyncSerialization"/></param>
/// <param name="ShouldSerialize"><inheritdoc cref="PropertyAccessors{TDeclaringType}.ShouldSerialize"/></param>
/// <param name="Shape">The property shape, for use when generating schema.</param>
internal record struct SerializableProperty<TDeclaringType>(string Name, ReadOnlyMemory<byte> RawPropertyNameString, SerializeProperty<TDeclaringType> Write, SerializePropertyAsync<TDeclaringType> WriteAsync, bool PreferAsyncSerialization, Func<TDeclaringType, bool>? ShouldSerialize, IPropertyShape Shape);

/// <summary>
/// A map of deserializable properties.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that contains properties to be deserialized.</typeparam>
/// <param name="Readers">The map of deserializable properties, keyed by the UTF-8 encoding of the property name.</param>
internal record struct MapDeserializableProperties<TDeclaringType>(SpanDictionary<byte, DeserializableProperty<TDeclaringType>>? Readers);

/// <summary>
/// Contains the data necessary for a converter to initialize some property with a value deserialized from msgpack.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property to be serialized.</typeparam>
/// <param name="Name">The property name.</param>
/// <param name="PropertyNameUtf8">The UTF-8 encoding of the property name.</param>
/// <param name="Read">A delegate that synchronously initializes the value of the property with a value deserialized from msgpack.</param>
/// <param name="ReadAsync">A delegate that asynchronously initializes the value of the property with a value deserialized from msgpack.</param>
/// <param name="PreferAsyncSerialization"><inheritdoc cref="Converter.PreferAsyncSerialization"/></param>
internal record struct DeserializableProperty<TDeclaringType>(string Name, ReadOnlyMemory<byte> PropertyNameUtf8, DeserializeProperty<TDeclaringType> Read, DeserializePropertyAsync<TDeclaringType> ReadAsync, bool PreferAsyncSerialization);

/// <summary>
/// Encapsulates serializing accessors for a particular property of some data type.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that declares the property that these accessors can serialize and deserialize values for.</typeparam>
/// <param name="MsgPackWriters">Delegates that can serialize the value of a property.</param>
/// <param name="MsgPackReaders">Delegates that can initialize the property with a value deserialized from msgpack.</param>
/// <param name="PreferAsyncSerialization"><inheritdoc cref="Converter.PreferAsyncSerialization"/></param>
/// <param name="ShouldSerialize">An optional func that determines whether a property should be serialized. When <see langword="null"/> the property should always be serialized.</param>
/// <param name="Shape">The property shape, for use with generating schema.</param>
internal record struct PropertyAccessors<TDeclaringType>(
	(SerializeProperty<TDeclaringType> Serialize, SerializePropertyAsync<TDeclaringType> SerializeAsync)? MsgPackWriters,
	(DeserializeProperty<TDeclaringType> Deserialize, DeserializePropertyAsync<TDeclaringType> DeserializeAsync)? MsgPackReaders,
	bool PreferAsyncSerialization,
	Func<TDeclaringType, bool>? ShouldSerialize,
	IPropertyShape Shape);

/// <summary>
/// Encapsulates the data passed through <see cref="ITypeShapeVisitor.VisitConstructor{TDeclaringType, TArgumentState}(IConstructorShape{TDeclaringType, TArgumentState}, object?)"/> state arguments
/// when serializing an object as a map.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose constructor is to be visited.</typeparam>
/// <param name="Serializers">Serializable properties on the data type.</param>
/// <param name="Deserializers">Deserializable properties on the data type.</param>
/// <param name="ParametersByName">A collection of constructor parameters, with any conflicting names removed.</param>
internal record MapConstructorVisitorInputs<TDeclaringType>(MapSerializableProperties<TDeclaringType> Serializers, MapDeserializableProperties<TDeclaringType> Deserializers, Dictionary<string, IConstructorParameterShape> ParametersByName);

/// <summary>
/// Encapsulates the data passed through <see cref="ITypeShapeVisitor.VisitConstructor{TDeclaringType, TArgumentState}(IConstructorShape{TDeclaringType, TArgumentState}, object?)"/> state arguments
/// when serializing an object as an array.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose constructor is to be visited.</typeparam>
/// <param name="Properties">The accessors to use for accessing each array element.</param>
internal record ArrayConstructorVisitorInputs<TDeclaringType>(List<(string Name, PropertyAccessors<TDeclaringType> Accessors)?> Properties)
{
	/// <summary>
	/// Constructs an array of just the property accessors (without property names).
	/// </summary>
	/// <returns>An array of accessors.</returns>
	internal PropertyAccessors<TDeclaringType>?[] GetJustAccessors() => this.Properties.Select(p => p?.Accessors).ToArray();
}

/// <summary>
/// Describes the derived types of some class that are allowed to appear as the runtime type in an object graph
/// for serialization, or may be referenced by an alias in the serialized data for deserialization.
/// </summary>
internal record SubTypes
{
	/// <summary>
	/// Gets the converters to use to deserialize a subtype, keyed by its integer alias.
	/// </summary>
	internal required FrozenDictionary<int, Converter> DeserializersByIntAlias { get; init; }

	/// <summary>
	/// Gets the converter to use to deserialize a subtype, keyed by its UTF-8 encoded string alias.
	/// </summary>
	internal required SpanDictionary<byte, Converter> DeserializersByStringAlias { get; init; }

	/// <summary>
	/// Gets the converter and alias to use for a subtype, keyed by their <see cref="Type"/>.
	/// </summary>
	internal required FrozenDictionary<Type, (FormattedSubTypeAlias Alias, Converter Converter, ITypeShape Shape)> Serializers { get; init; }
}
