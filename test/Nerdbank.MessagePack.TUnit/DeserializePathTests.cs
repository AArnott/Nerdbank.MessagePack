// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DeserializePathTests : MessagePackSerializerTestBase
{
	public enum TestEnum
	{
		Value1,
		Value2,
		Value3,
	}

	// Test deserializing the root object.
	[Test]
	public void RootObject()
	{
		OuterContainer container = new(new(5));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, OuterContainer> options = new(c => c);
		OuterContainer? result = this.Serializer.DeserializePath<OuterContainer, OuterContainer, Witness>(msgpack, options, this.TimeoutToken);

		Assert.NotNull(result);
		Assert.Equal(5, result.Inner?.Value);
	}

	[Test]
	public void OneStepObject()
	{
		OuterContainer container = new(new(5));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, InnerValue> options = new(c => c.Inner!);
		InnerValue? result = this.Serializer.DeserializePath<OuterContainer, InnerValue, Witness>(msgpack, options, this.TimeoutToken);

		Assert.NotNull(result);
		Assert.Equal(5, result.Value);
	}

	// Test deserializing a primitive at the end of a non-empty path.
	[Test]
	public void NestedPrimitive()
	{
		OuterContainer container = new(new(42));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value);
		int result = this.Serializer.DeserializePath<OuterContainer, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(42, result);
	}

	// Test deserializing an object at the end of a non-empty path.
	[Test]
	public void NestedObject()
	{
		ThreeLevelContainer container = new(new(new(99)));
		byte[] msgpack = this.Serializer.Serialize<ThreeLevelContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ThreeLevelContainer, InnerValue> options = new(c => c.Middle!.Inner!);
		InnerValue? result = this.Serializer.DeserializePath<ThreeLevelContainer, InnerValue, Witness>(msgpack, options, this.TimeoutToken);

		Assert.NotNull(result);
		Assert.Equal(99, result.Value);
	}

	// Test deserializing a collection at the end of a non-empty path.
	[Test]
	public void Collection()
	{
		ContainerWithCollection container = new([1, 2, 3]);
		byte[] msgpack = this.Serializer.Serialize<ContainerWithCollection, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithCollection, int[]> options = new(c => c.Values!);
		int[]? result = this.Serializer.DeserializePath<ContainerWithCollection, int[], Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal([1, 2, 3], result!);
	}

	// Test deserializing an enum at the end of a non-empty path.
	[Test]
	public void Enum()
	{
		ContainerWithEnum container = new(TestEnum.Value2);
		byte[] msgpack = this.Serializer.Serialize<ContainerWithEnum, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithEnum, TestEnum> options = new(c => c.Status);
		TestEnum result = this.Serializer.DeserializePath<ContainerWithEnum, TestEnum, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(TestEnum.Value2, result);
	}

	// Test deserializing a null value.
	[Test]
	public void NullValue()
	{
		OuterContainer container = new(null);
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		InnerValue? result = this.Serializer.DeserializePath<OuterContainer, InnerValue?, Witness>(msgpack, new(c => c.Inner), this.TimeoutToken);

		Assert.Null(result);
	}

	// Test throwing when the path is not found.
	[Test, MatrixDataSource]
	public void MemberPathNotFound_Throws(bool nullRoot)
	{
		OuterContainer? container = nullRoot ? null : new(null);
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value)
		{
			DefaultForUndiscoverablePath = false,
		};

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() =>
			this.Serializer.DeserializePath<OuterContainer, int, Witness>(msgpack, options, this.TimeoutToken));

		Console.WriteLine(ex.Message);
		if (nullRoot)
		{
			Assert.Matches(@"\Wc(?!\.Inner)", ex.Message);
		}
		else
		{
			Assert.Matches(@"\Wc\.Inner(?!\.Value)", ex.Message);
		}
	}

	// Test getting default value when the path is not found.
	[Test, MatrixDataSource]
	public void MemberPathNotFound_ReturnsDefault(bool nullRoot)
	{
		OuterContainer? container = nullRoot ? null : new OuterContainer(null);
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container as OuterContainer, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value)
		{
			DefaultForUndiscoverablePath = true,
		};

		int result = this.Serializer.DeserializePath<OuterContainer, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(0, result);
	}

	// Test throwing when the path is not found.
	[Test]
	public void IndexPathNotFound_Throws()
	{
		byte[] msgpack = this.Serializer.Serialize<ContainerWithCollection, Witness>(new ContainerWithCollection(null), this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithCollection, int> options = new(c => c.Values![2])
		{
			DefaultForUndiscoverablePath = false,
		};

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() =>
			this.Serializer.DeserializePath<ContainerWithCollection, int, Witness>(msgpack, options, this.TimeoutToken));

		Console.WriteLine(ex.Message);
		Assert.Matches(@"\Wc\.Values(?!\[)", ex.Message);
	}

	// Test getting default value when the path is not found.
	[Test]
	public void IndexPathNotFound_ReturnsDefault()
	{
		byte[] msgpack = this.Serializer.Serialize<ContainerWithCollection, Witness>(new ContainerWithCollection(null), this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithCollection, int> options = new(c => c.Values![2])
		{
			DefaultForUndiscoverablePath = true,
		};

		int result = this.Serializer.DeserializePath<ContainerWithCollection, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(0, result);
	}

	// Test step through array indexer
	[Test]
	public void ThroughArray()
	{
		ContainerByArray container = new([null, new(10), new(20)]);
		byte[] msgpack = this.Serializer.Serialize<ContainerByArray, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerByArray, int> options = new(c => c.Values[1]!.Value);
		int result = this.Serializer.DeserializePath<ContainerByArray, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(10, result);
	}

	// Test step through dictionary
	[Test]
	public void ThroughDictionary()
	{
		ContainerByDictionary container = new(new() { ["a"] = null, ["b"] = new(15) });
		byte[] msgpack = this.Serializer.Serialize<ContainerByDictionary, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerByDictionary, int> options = new(c => c.Values["b"]!.Value);
		int result = this.Serializer.DeserializePath<ContainerByDictionary, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(15, result);
	}

	// Test step through dictionary with custom key
	[Test]
	public void ThroughDictionaryCustomKey()
	{
		ContainerByDictionaryCustomKey container = new(new() { [new CustomKey(5)] = null, [new CustomKey(3)] = new(25) });
		byte[] msgpack = this.Serializer.Serialize<ContainerByDictionaryCustomKey, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		CustomKey key = new(3);
		MessagePackSerializer.DeserializePathOptions<ContainerByDictionaryCustomKey, int> options = new(c => c.Values[key]!.Value);
		int result = this.Serializer.DeserializePath<ContainerByDictionaryCustomKey, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(25, result);
	}

	// Test step through ImmutableArray indexer
	[Test]
	public void ThroughImmutableArray()
	{
		ContainerByImmutableArray container = new([null, new(30), new(40)]);
		byte[] msgpack = this.Serializer.Serialize<ContainerByImmutableArray, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerByImmutableArray, int> options = new(c => c.Values[2]!.Value);
		int result = this.Serializer.DeserializePath<ContainerByImmutableArray, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(40, result);
	}

	// Test with keyed properties (array format)
	[Test]
	public void KeyedProperties()
	{
		KeyedContainer container = new() { Before = "a", Value = new(50), After = "b" };
		byte[] msgpack = this.Serializer.Serialize<KeyedContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<KeyedContainer, int> options = new(c => c.Value!.Value);
		int result = this.Serializer.DeserializePath<KeyedContainer, int, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(50, result);
	}

	// Test overloads with ReadOnlyMemory
	[Test]
	public void ReadOnlyMemory_Overload()
	{
		OuterContainer container = new(new(7));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value);
		int result = this.Serializer.DeserializePath<OuterContainer, int, Witness>(msgpack.AsMemory(), options, this.TimeoutToken);

		Assert.Equal(7, result);
	}

	// Test overloads with ReadOnlySequence
	[Test]
	public void ReadOnlySequence_Overload()
	{
		OuterContainer container = new(new(8));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);

		ReadOnlySequence<byte> sequence = new(msgpack);
		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value);
		int result = this.Serializer.DeserializePath<OuterContainer, int, Witness>(sequence, options, this.TimeoutToken);

		Assert.Equal(8, result);
	}

	// Test overloads with Stream
	[Test]
	public void Stream_Overload()
	{
		OuterContainer container = new(new(9));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);

		using MemoryStream stream = new(msgpack);
		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value);
		int result = this.Serializer.DeserializePath<OuterContainer, int, Witness>(stream, options, this.TimeoutToken);

		Assert.Equal(9, result);
	}

	// Test with reference preservation disabled
	[Property("ReferencePreservation", "true")]
	[Test]
	public void ReferencesPreserved_NotSupported()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		OuterContainer container = new(new(12));
		byte[] msgpack = this.Serializer.Serialize<OuterContainer, Witness>(container, this.TimeoutToken);

		MessagePackSerializer.DeserializePathOptions<OuterContainer, int> options = new(c => c.Inner!.Value);
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.DeserializePath<OuterContainer, int, Witness>(msgpack, options, this.TimeoutToken));
		Console.WriteLine(ex.Message);
	}

	// Test with complex path through multiple levels
	[Test]
	public void ComplexPath()
	{
		ComplexContainer container = new(new(new() { ["key1"] = [1, 2, 3], ["key2"] = [4, 5, 6] }));
		byte[] msgpack = this.Serializer.Serialize<ComplexContainer, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ComplexContainer, short> options = new(c => c.Outer!.Values["key2"]![1]);
		short result = this.Serializer.DeserializePath<ComplexContainer, short, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(5, result);
	}

	// Test deserializing string value
	[Test]
	public void StringValue()
	{
		ContainerWithString container = new("hello world");
		byte[] msgpack = this.Serializer.Serialize<ContainerWithString, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithString, string> options = new(c => c.Text!);
		string? result = this.Serializer.DeserializePath<ContainerWithString, string, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal("hello world", result);
	}

	// Test deserializing struct value type
	[Test]
	public void StructValue()
	{
		ContainerWithStruct container = new(new StructRecord(100, "test"));
		byte[] msgpack = this.Serializer.Serialize<ContainerWithStruct, Witness>(container, this.TimeoutToken);
		this.LogMsgPack(msgpack);

		MessagePackSerializer.DeserializePathOptions<ContainerWithStruct, StructRecord> options = new(c => c.Data);
		StructRecord result = this.Serializer.DeserializePath<ContainerWithStruct, StructRecord, Witness>(msgpack, options, this.TimeoutToken);

		Assert.Equal(100, result.Id);
		Assert.Equal("test", result.Name);
	}

	public record struct CustomKey(int Key);

	[GenerateShape]
	public partial record struct StructRecord(int Id, string Name);

	[GenerateShape]
	public partial record InnerValue(int Value);

	[GenerateShape]
	public partial record OuterContainer(InnerValue? Inner);

	[GenerateShape]
	public partial record ThreeLevelContainer(OuterContainer? Middle);

	[GenerateShape]
	public partial record ContainerWithCollection(int[]? Values);

	[GenerateShape]
	public partial record ContainerWithEnum(TestEnum Status);

	[GenerateShape]
	public partial record ContainerByArray(InnerValue?[] Values);

	[GenerateShape]
	public partial record ContainerByDictionary(Dictionary<string, InnerValue?> Values);

	[GenerateShape]
	public partial record ContainerByDictionaryCustomKey(Dictionary<CustomKey, InnerValue?> Values);

	[GenerateShape]
	public partial record ContainerByImmutableArray(ImmutableArray<InnerValue?> Values);

	[GenerateShape]
	public partial class KeyedContainer
	{
		[Key(0)]
		public string? Before { get; set; }

		[Key(1)]
		public InnerValue? Value { get; set; }

		[Key(2)]
		public string? After { get; set; }
	}

	[GenerateShape]
	public partial record DictionaryContainer(Dictionary<string, short[]?> Values);

	[GenerateShape]
	public partial record ComplexContainer(DictionaryContainer? Outer);

	[GenerateShape]
	public partial record ContainerWithString(string? Text);

	[GenerateShape]
	public partial record ContainerWithStruct(StructRecord Data);

	[GenerateShapeFor<OuterContainer>]
	[GenerateShapeFor<InnerValue>]
	[GenerateShapeFor<ThreeLevelContainer>]
	[GenerateShapeFor<ContainerWithCollection>]
	[GenerateShapeFor<ContainerWithEnum>]
	[GenerateShapeFor<ContainerByArray>]
	[GenerateShapeFor<ContainerByDictionary>]
	[GenerateShapeFor<ContainerByDictionaryCustomKey>]
	[GenerateShapeFor<ContainerByImmutableArray>]
	[GenerateShapeFor<KeyedContainer>]
	[GenerateShapeFor<DictionaryContainer>]
	[GenerateShapeFor<ComplexContainer>]
	[GenerateShapeFor<ContainerWithString>]
	[GenerateShapeFor<StructRecord>]
	[GenerateShapeFor<ContainerWithStruct>]
	private partial class Witness;
}
