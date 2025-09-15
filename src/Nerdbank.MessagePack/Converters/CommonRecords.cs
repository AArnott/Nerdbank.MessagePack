// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable NBMsgPackAsync

using System.Collections.Frozen;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A delegate that can read a property from a data type and serialize it to a <see cref="MessagePackWriter"/>.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be read.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="writer">The means by which msgpack should be written.</param>
/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Write" path="/param[@name='context']"/></param>
internal delegate void SerializeProperty<TDeclaringType>(in TDeclaringType container, ref MessagePackWriter writer, SerializationContext context);

/// <summary>
/// A delegate that can asynchronously serialize a property to a <see cref="MessagePackAsyncWriter"/>.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be serialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="writer">The means by which msgpack should be written.</param>
/// <param name="context">The serialization context.</param>
/// <returns>A task that represents the asynchronous operation.</returns>
internal delegate ValueTask SerializePropertyAsync<TDeclaringType>(TDeclaringType container, MessagePackAsyncWriter writer, SerializationContext context);

/// <summary>
/// A delegate that can deserialize a value from a <see cref="MessagePackReader"/> and assign it to a property.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be initialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="reader">The means by which msgpack should be read.</param>
/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
internal delegate void DeserializeProperty<TDeclaringType>(ref TDeclaringType container, ref MessagePackReader reader, SerializationContext context);

/// <summary>
/// A delegate that can asynchronously deserialize the value from a <see cref="MessagePackAsyncReader"/> and assign it to a property.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose property is to be initialized.</typeparam>
/// <param name="container">The instance of the data type to be serialized.</param>
/// <param name="reader">The means by which msgpack should be read.</param>
/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
/// <returns>The <paramref name="container"/>, with the property initialized. This is useful when <typeparamref name="TDeclaringType"/> is a struct.</returns>
internal delegate ValueTask<TDeclaringType> DeserializePropertyAsync<TDeclaringType>(TDeclaringType container, MessagePackAsyncReader reader, SerializationContext context);

/// <summary>
/// A map of serializable properties.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that contains the properties to be serialized.</typeparam>
/// <param name="properties">The list of serializable properties, including the msgpack encoding of the property name and the delegate to serialize that property.</param>
internal class MapSerializableProperties<TDeclaringType>(ReadOnlyMemory<SerializableProperty<TDeclaringType>> properties)
{
	/// <summary>Gets or sets the list of serializable properties.</summary>
	public ReadOnlyMemory<SerializableProperty<TDeclaringType>> Properties { get; set; } = properties;
}

/// <summary>
/// Contains the data necessary for a converter to serialize the value of a particular property.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property to be serialized.</typeparam>
/// <param name="name">The property name.</param>
/// <param name="rawPropertyNameString">The entire msgpack encoding of the property name, including the string header.</param>
/// <param name="write">A delegate that synchronously serializes the value of the property.</param>
/// <param name="writeAsync">A delegate that asynchonously serializes the value of the property.</param>
/// <param name="converter">The converter backing this property. Only intended for retrieving the <see cref="PreferAsyncSerialization"/> value.</param>
/// <param name="shouldSerialize"><inheritdoc cref="PropertyAccessors{TDeclaringType}.ShouldSerialize"/></param>
/// <param name="shape">The property shape, for use when generating schema.</param>
internal class SerializableProperty<TDeclaringType>(
	string name,
	ReadOnlyMemory<byte> rawPropertyNameString,
	SerializeProperty<TDeclaringType> write,
	SerializePropertyAsync<TDeclaringType> writeAsync,
	MessagePackConverter converter,
	Func<TDeclaringType, bool>? shouldSerialize,
	IPropertyShape shape)
{
	/// <summary>Gets the property name.</summary>
	public string Name => name;

	/// <summary>Gets the entire msgpack encoding of the property name, including the string header.</summary>
	public ReadOnlyMemory<byte> RawPropertyNameString => rawPropertyNameString;

	/// <summary>Gets the delegate that synchronously serializes the value of the property.</summary>
	public SerializeProperty<TDeclaringType> Write => write;

	/// <summary>Gets the delegate that asynchronously serializes the value of the property.</summary>
	public SerializePropertyAsync<TDeclaringType> WriteAsync => writeAsync;

	/// <summary>Gets the converter backing this property.</summary>
	public MessagePackConverter Converter => converter;

	/// <summary>Gets the optional func that determines whether a property should be serialized. When <see langword="null"/> the property should always be serialized.</summary>
	public Func<TDeclaringType, bool>? ShouldSerialize => shouldSerialize;

	/// <summary>Gets the property shape, for use when generating schema.</summary>
	public IPropertyShape Shape => shape;

	/// <inheritdoc cref="MessagePackConverter.PreferAsyncSerialization"/>
	public bool PreferAsyncSerialization => this.Converter.PreferAsyncSerialization;
}

/// <summary>
/// A map of deserializable properties.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that contains properties to be deserialized.</typeparam>
/// <param name="readers">The map of deserializable properties, keyed by the UTF-8 encoding of the property name.</param>
internal class MapDeserializableProperties<TDeclaringType>(SpanDictionary<byte, DeserializableProperty<TDeclaringType>>? readers)
{
	/// <summary>Gets the map of deserializable properties, keyed by the UTF-8 encoding of the property name.</summary>
	public SpanDictionary<byte, DeserializableProperty<TDeclaringType>>? Readers => readers;
}

/// <summary>
/// Contains the data necessary for a converter to initialize some property with a value deserialized from msgpack.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property to be deserialized.</typeparam>
/// <param name="name">The property name.</param>
/// <param name="propertyNameUtf8">The UTF-8 encoding of the property name.</param>
/// <param name="read">A delegate that synchronously initializes the value of the property with a value deserialized from msgpack.</param>
/// <param name="readAsync">A delegate that asynchronously initializes the value of the property with a value deserialized from msgpack.</param>
/// <param name="converter">The converter backing this property. Only intended for retrieving the <see cref="PreferAsyncSerialization"/> value.</param>
/// <param name="shapePosition">The value of <see cref="IPropertyShape.Position"/> or <see cref="IParameterShape.Position"/>.</param>
internal class DeserializableProperty<TDeclaringType>(
	string name,
	ReadOnlyMemory<byte> propertyNameUtf8,
	DeserializeProperty<TDeclaringType> read,
	DeserializePropertyAsync<TDeclaringType> readAsync,
	MessagePackConverter converter,
	int shapePosition)
{
	/// <summary>Gets the property name.</summary>
	public string Name => name;

	/// <summary>Gets the UTF-8 encoding of the property name.</summary>
	public ReadOnlyMemory<byte> PropertyNameUtf8 => propertyNameUtf8;

	/// <summary>Gets the delegate that synchronously deserializes the value of the property.</summary>
	public DeserializeProperty<TDeclaringType> Read => read;

	/// <summary>Gets the delegate that asynchronously deserializes the value of the property.</summary>
	public DeserializePropertyAsync<TDeclaringType> ReadAsync => readAsync;

	/// <summary>Gets the converter backing this property.</summary>
	public MessagePackConverter Converter => converter;

	/// <summary>
	/// Gets the value of <see cref="IPropertyShape.Position"/> or <see cref="IParameterShape.Position"/>.
	/// </summary>
	public int ShapePosition => shapePosition;

	/// <inheritdoc cref="MessagePackConverter.PreferAsyncSerialization"/>
	public bool PreferAsyncSerialization => this.Converter.PreferAsyncSerialization;
}

/// <summary>
/// Encapsulates serializing accessors for a particular property of some data type.
/// </summary>
/// <typeparam name="TDeclaringType">The data type that declares the property that these accessors can serialize and deserialize values for.</typeparam>
/// <param name="msgPackWriters">Delegates that can serialize the value of a property.</param>
/// <param name="msgPackReaders">Delegates that can initialize the property with a value deserialized from msgpack.</param>
/// <param name="converter">The converter backing this property. Only intended for retrieving the <see cref="PreferAsyncSerialization"/> value.</param>
/// <param name="shouldSerialize">An optional func that determines whether a property should be serialized. When <see langword="null"/> the property should always be serialized.</param>
/// <param name="shape">The property shape, for use with generating schema.</param>
internal class PropertyAccessors<TDeclaringType>(
	(SerializeProperty<TDeclaringType> Serialize, SerializePropertyAsync<TDeclaringType> SerializeAsync)? msgPackWriters,
	(DeserializeProperty<TDeclaringType> Deserialize, DeserializePropertyAsync<TDeclaringType> DeserializeAsync)? msgPackReaders,
	MessagePackConverter converter,
	Func<TDeclaringType, bool>? shouldSerialize,
	IPropertyShape shape)
{
	/// <summary>Gets the delegates that can serialize the value of a property.</summary>
	public (SerializeProperty<TDeclaringType> Serialize, SerializePropertyAsync<TDeclaringType> SerializeAsync)? MsgPackWriters => msgPackWriters;

	/// <summary>Gets the delegates that can initialize the property with a value deserialized from msgpack.</summary>
	public (DeserializeProperty<TDeclaringType> Deserialize, DeserializePropertyAsync<TDeclaringType> DeserializeAsync)? MsgPackReaders => msgPackReaders;

	/// <summary>Gets the converter backing this property.</summary>
	public MessagePackConverter Converter => converter;

	/// <summary>Gets the optional func that determines whether a property should be serialized. When <see langword="null"/> the property should always be serialized.</summary>
	public Func<TDeclaringType, bool>? ShouldSerialize => shouldSerialize;

	/// <summary>Gets the property shape, for use with generating schema.</summary>
	public IPropertyShape Shape => shape;

	/// <inheritdoc cref="MessagePackConverter.PreferAsyncSerialization"/>
	public bool PreferAsyncSerialization => this.Converter.PreferAsyncSerialization;
}

/// <summary>
/// Provides direct access to the setter and getter for a property.
/// </summary>
/// <typeparam name="TDeclaringType">The type that declares the property.</typeparam>
/// <typeparam name="TValue">The value to be set or retrieved.</typeparam>
/// <param name="setter">The setter.</param>
/// <param name="getter">The getter.</param>
internal class DirectPropertyAccess<TDeclaringType, TValue>(
	Setter<TDeclaringType, TValue>? setter,
	Getter<TDeclaringType, TValue>? getter)
{
	/// <summary>Gets the setter.</summary>
	public Setter<TDeclaringType, TValue>? Setter => setter;

	/// <summary>Gets the getter.</summary>
	public Getter<TDeclaringType, TValue>? Getter => getter;
}

/// <summary>
/// Encapsulates the data passed through <see cref="TypeShapeVisitor.VisitConstructor{TDeclaringType, TArgumentState}(IConstructorShape{TDeclaringType, TArgumentState}, object?)"/> state arguments
/// when serializing an object as a map.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose constructor is to be visited.</typeparam>
/// <param name="serializers">Serializable properties on the data type.</param>
/// <param name="deserializers">Deserializable properties on the data type.</param>
/// <param name="parametersByName">A collection of constructor parameters, with any conflicting names removed.</param>
/// <param name="unusedDataProperty">The special unused data property, if present.</param>
internal class MapConstructorVisitorInputs<TDeclaringType>(
	MapSerializableProperties<TDeclaringType> serializers,
	MapDeserializableProperties<TDeclaringType> deserializers,
	Dictionary<string, IParameterShape> parametersByName,
	DirectPropertyAccess<TDeclaringType, UnusedDataPacket>? unusedDataProperty)
{
	/// <summary>Gets the serializable properties on the data type.</summary>
	public MapSerializableProperties<TDeclaringType> Serializers => serializers;

	/// <summary>Gets the deserializable properties on the data type.</summary>
	public MapDeserializableProperties<TDeclaringType> Deserializers => deserializers;

	/// <summary>Gets the collection of constructor parameters, with any conflicting names removed.</summary>
	public Dictionary<string, IParameterShape> ParametersByName => parametersByName;

	/// <summary>Gets the special unused data property, if present.</summary>
	public DirectPropertyAccess<TDeclaringType, UnusedDataPacket>? UnusedDataProperty => unusedDataProperty;
}

/// <summary>
/// Encapsulates the data passed through <see cref="TypeShapeVisitor.VisitConstructor{TDeclaringType, TArgumentState}(IConstructorShape{TDeclaringType, TArgumentState}, object?)"/> state arguments
/// when serializing an object as an array.
/// </summary>
/// <typeparam name="TDeclaringType">The data type whose constructor is to be visited.</typeparam>
/// <param name="properties">The accessors to use for accessing each array element.</param>
/// <param name="unusedDataProperty">The special unused data property, if present.</param>
internal class ArrayConstructorVisitorInputs<TDeclaringType>(
	List<(string Name, PropertyAccessors<TDeclaringType> Accessors)?> properties,
	DirectPropertyAccess<TDeclaringType, UnusedDataPacket>? unusedDataProperty)
{
	/// <summary>Gets the accessors to use for accessing each array element.</summary>
	public List<(string Name, PropertyAccessors<TDeclaringType> Accessors)?> Properties => properties;

	/// <summary>Gets the special unused data property, if present.</summary>
	public DirectPropertyAccess<TDeclaringType, UnusedDataPacket>? UnusedDataProperty => unusedDataProperty;

	/// <summary>
	/// Constructs an array of just the property accessors (without property names).
	/// </summary>
	/// <returns>An array of accessors.</returns>
	internal PropertyAccessors<TDeclaringType>?[] GetJustAccessors()
	{
		var result = new PropertyAccessors<TDeclaringType>?[this.Properties.Count];
		for (int i = 0; i < this.Properties.Count; i++)
		{
			result[i] = this.Properties[i]?.Accessors;
		}

		return result;
	}
}

/// <summary>
/// Describes the derived types of some class that are allowed to appear as the runtime type in an object graph
/// for serialization, or may be referenced by an alias in the serialized data for deserialization.
/// </summary>
/// <typeparam name="TUnion">The common base type.</typeparam>
internal class SubTypes<TUnion>
{
	/// <summary>
	/// A singleton instance that may be used to represent an expressly disabled union.
	/// </summary>
	internal static readonly SubTypes<TUnion> DisabledInstance = new()
	{
		Disabled = true,
		DeserializersByIntAlias = FrozenDictionary<int, MessagePackConverter>.Empty,
		DeserializersByStringAlias = new SpanDictionary<byte, MessagePackConverter>([], ByteSpanEqualityComparer.Ordinal),
		Serializers = FrozenSet<(DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)>.Empty,
		TryGetSerializer = (ref TUnion _) => default,
	};

	/// <inheritdoc cref="DerivedTypeUnion.Disabled"/>
	public bool Disabled { get; private init; }

	/// <summary>Gets the converters to use to deserialize a subtype, keyed by its integer alias.</summary>
	public required FrozenDictionary<int, MessagePackConverter> DeserializersByIntAlias { get; init; }

	/// <summary>Gets the converter to use to deserialize a subtype, keyed by its UTF-8 encoded string alias.</summary>
	public required SpanDictionary<byte, MessagePackConverter> DeserializersByStringAlias { get; init; }

	/// <summary>Gets the set of converters and aliases.</summary>
	public required FrozenSet<(DerivedTypeIdentifier Alias, MessagePackConverter Converter, ITypeShape Shape)> Serializers { get; init; }

	/// <summary>Gets the converter and alias to use for a subtype, keyed by their <see cref="Type"/>.</summary>
	public required Getter<TUnion, (DerivedTypeIdentifier Alias, MessagePackConverter Converter)?> TryGetSerializer { get; init; }
}
