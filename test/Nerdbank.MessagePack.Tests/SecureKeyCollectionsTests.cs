// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SecureKeyCollectionsTests : MessagePackSerializerTestBase
{
	[Fact]
	public void CanSerializeDictionariesWithEmptyKeys_ButNotDeserialize()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();
		HasDictionaryWithIndirectObjectKey container = new()
		{
			Collection = new(),
		};

		// Serialization should succeed because although we cannot create an equality comparer,
		// that only limits our ability to deserialize, not serialize.
		byte[] payload = serializer.Serialize(container, TestContext.Current.CancellationToken);

		// Deserialization should fail because the FilterKey type has an 'object' property which is not supported.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => serializer.Deserialize<HasDictionaryWithIndirectObjectKey>(payload, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.ToString());
		Assert.IsType<NotSupportedException>(ex.GetBaseException());

		// Deserialization with primitives should work fine though.
		MessagePackReader reader = new(payload);
		Assert.NotNull(serializer.DeserializePrimitives(ref reader, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void CanSerializeSetsWithEmptyKeys_ButNotDeserialize()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();
		HasHashSetWithIndirectObjectKey container = new()
		{
			Collection = new(),
		};

		// Serialization should succeed because although we cannot create an equality comparer,
		// that only limits our ability to deserialize, not serialize.
		byte[] payload = serializer.Serialize(container, TestContext.Current.CancellationToken);

		// Deserialization should fail because the FilterKey type has an 'object' property which is not supported.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => serializer.Deserialize<HasHashSetWithIndirectObjectKey>(payload, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.ToString());
		Assert.IsType<NotSupportedException>(ex.GetBaseException());

		// Deserialization with primitives should work fine though.
		MessagePackReader reader = new(payload);
		Assert.NotNull(serializer.DeserializePrimitives(ref reader, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void CanSerializeDictionariesWithEmptyKeys_AndDeserializeWithGetterOnlyCollection()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();
		HasDictionaryWithIndirectObjectKeyGetterOnly container = new();

		// Serialization should succeed because although we cannot create an equality comparer,
		// that only limits our ability to deserialize, not serialize.
		byte[] payload = serializer.Serialize(container, TestContext.Current.CancellationToken);

		// Deserialization should also succeed because we never have to or get to initialize the keyed collection ourselves.
		HasDictionaryWithIndirectObjectKeyGetterOnly? deserialized = serializer.Deserialize<HasDictionaryWithIndirectObjectKeyGetterOnly>(payload, TestContext.Current.CancellationToken);
		Assert.NotNull(deserialized);

		// Deserialization with primitives should work fine too.
		MessagePackReader reader = new(payload);
		Assert.NotNull(serializer.DeserializePrimitives(ref reader, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void CanSerializeSetsWithEmptyKeys_AndDeserializeWithGetterOnlyCollection()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();
		HasHashSetWithIndirectObjectKeyGetterOnly container = new();

		// Serialization should succeed because although we cannot create an equality comparer,
		// that only limits our ability to deserialize, not serialize.
		byte[] payload = serializer.Serialize(container, TestContext.Current.CancellationToken);

		// Deserialization should also succeed because we never have to or get to initialize the keyed collection ourselves.
		HasHashSetWithIndirectObjectKeyGetterOnly? deserialized = serializer.Deserialize<HasHashSetWithIndirectObjectKeyGetterOnly>(payload, TestContext.Current.CancellationToken);
		Assert.NotNull(deserialized);

		// Deserialization with primitives should work fine too.
		MessagePackReader reader = new(payload);
		Assert.NotNull(serializer.DeserializePrimitives(ref reader, TestContext.Current.CancellationToken));
	}

	[GenerateShape]
	internal partial class HasDictionaryWithIndirectObjectKey
	{
		public Dictionary<HasObjectProperty, bool>? Collection { get; set; }
	}

	[GenerateShape]
	internal partial class HasHashSetWithIndirectObjectKey
	{
		public HashSet<HasObjectProperty>? Collection { get; set; }
	}

	[GenerateShape]
	internal partial class HasDictionaryWithIndirectObjectKeyGetterOnly
	{
		public Dictionary<HasObjectProperty, bool>? Collection { get; } = new();
	}

	[GenerateShape]
	internal partial class HasHashSetWithIndirectObjectKeyGetterOnly
	{
		public HashSet<HasObjectProperty>? Collection { get; } = new();
	}

	internal partial class HasObjectProperty
	{
		public object? Value { get; set; }
	}
}
