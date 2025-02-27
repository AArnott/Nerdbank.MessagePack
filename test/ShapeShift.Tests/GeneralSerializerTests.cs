// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Sdk;
using static SharedTestTypes;

public abstract partial class GeneralSerializerTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	/// <summary>
	/// Verifies that properties are independent on each instance of <see cref="SerializerBase"/>
	/// of properties on other instances.
	/// </summary>
	[Fact]
	public void PropertiesAreIndependent()
	{
		this.Serializer = this.Serializer with { SerializeEnumValuesByName = true };
		SerializerBase s1 = this.Serializer with { InternStrings = true };
		SerializerBase s2 = this.Serializer with { InternStrings = false };

		s1 = s1 with { SerializeEnumValuesByName = false };
		Assert.True(s2.SerializeEnumValuesByName);
	}

	[Fact]
	public void SimpleNull() => this.AssertRoundtrip<Fruit>(null);

	[Fact]
	public void SimplePoco() => this.AssertRoundtrip(new Fruit { Seeds = 18 });

	[Fact]
	public void MultiByteString() => this.AssertRoundtrip<string, Witness>(TestConstants.MultibyteCharString);

	[Fact]
	public void NoProperties() => this.AssertRoundtrip(new EmptyClass());

	[Fact]
	public void AllIntTypes() => this.AssertRoundtrip(new IntRichPoco { Int8 = -1, Int16 = -2, Int32 = -3, Int64 = -4, UInt8 = 1, UInt16 = 2, UInt32 = 3, UInt64 = 4 });

	[Fact]
	public void SimpleRecordClass() => this.AssertRoundtrip(new RecordClass(42) { Weight = 5, ChildNumber = 2 });

	[Fact]
	public void ClassWithDefaultCtorWithInitProperty() => this.AssertRoundtrip(new DefaultCtorWithInitProperty { Age = 42 });

	[Fact]
	public void RecordWithOtherPrimitives() => this.AssertRoundtrip(new OtherPrimitiveTypes("hello", true, 0.1f, 0.2));

	[Fact]
	public void NullableStruct_Null() => this.AssertRoundtrip(new RecordWithNullableStruct(null));

	[Fact]
	public void NullableStruct_NotNull() => this.AssertRoundtrip(new RecordWithNullableStruct(3));

	[Fact]
	public void Dictionary() => this.AssertRoundtrip(new ClassWithDictionary { StringInt = new() { { "a", 1 }, { "b", 2 } } });

	[Fact]
	public void Dictionary_Null() => this.AssertRoundtrip(new ClassWithDictionary { StringInt = null });

	[Fact]
	public void ImmutableDictionary() => this.AssertRoundtrip(new ClassWithImmutableDictionary { StringInt = ImmutableDictionary<string, int>.Empty.Add("a", 1) });

	[Fact]
	public void Array() => this.AssertRoundtrip(new ClassWithArray { IntArray = [1, 2, 3] });

	[Fact]
	public void Array_Null() => this.AssertRoundtrip(new ClassWithArray { IntArray = null });

#if NET
#pragma warning disable SA1500 // Braces for multi-line statements should not share line
	[Theory, PairwiseData]
	public void MultidimensionalArray(MultiDimensionalArrayFormat format)
	{
		this.Serializer = this.Serializer with { MultiDimensionalArrayFormat = format };
		this.AssertRoundtrip(new HasMultiDimensionalArray
		{
			Array2D = new[,]
			{
				{ 1, 2, 5 },
				{ 3, 4, 6 },
			},
			Array3D = new int[2, 3, 4]
			{
				{ { 20, 21, 22, 23 }, { 24, 25, 26, 27 }, { 28, 29, 30, 31 } },
				{ { 40, 41, 42, 43 }, { 44, 45, 46, 47 }, { 48, 49, 50, 51 } },
			},
		});
	}
#pragma warning restore SA1500 // Braces for multi-line statements should not share line
#endif

	[Fact]
	public void MultidimensionalArray_Null()
	{
		try
		{
			this.AssertRoundtrip(new HasMultiDimensionalArray());
		}
		catch (PlatformNotSupportedException ex)
		{
			throw SkipException.ForSkip($"Skipped: {ex.Message}");
		}
	}

	[Fact]
	public void Enumerable() => this.AssertRoundtrip(new ClassWithEnumerable { IntEnum = [1, 2, 3] });

	[Fact]
	public void Enumerable_Null() => this.AssertRoundtrip(new ClassWithEnumerable { IntEnum = null });

	[Fact]
	public void Enum() => this.AssertRoundtrip(new HasEnum(SomeEnum.B));

	[Fact]
	public void SerializeUnannotatedViaWitness() => this.AssertRoundtrip<UnannotatedPoco, Witness>(new UnannotatedPoco { Value = 42 });

	[Fact]
	public void SerializeGraphWithAnnotationOnlyAtBase() => this.AssertRoundtrip(new ReferencesUnannotatedPoco { Poco = new UnannotatedPoco { Value = 42 } });

	[Fact]
	public void PrivateFields() => this.AssertRoundtrip(new InternalRecordWithPrivateField { PrivateFieldAccessor = 42, PrivatePropertyAccessor = 43 });

	[Fact]
	public void ReadOnlyPropertiesNotSerialized()
	{
		RecordWithReadOnlyProperties obj = new(1, 2);
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		Reader reader = new(msgpack, this.Serializer.Deformatter);

		// The Sum field should not be serialized.
		Assert.False(ObjectMapHasKey(reader, nameof(obj.Sum)));
	}

	[Fact]
	public void ReadOnlyPropertiesNotSerialized_NoCtor()
	{
		RecordWithReadOnlyProperties_NoConstructor obj = new(1, 2);
		Sequence<byte> seq = new();
		this.Serializer.Serialize(seq, obj, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		this.LogFormattedBytes(seq);
		Reader reader = new(seq, this.Serializer.Deformatter);

		// The Sum field should not be serialized.
		Assert.False(ObjectMapHasKey(reader, nameof(obj.Sum)));
	}

	[Fact]
	public void ReadOnlyPropertiesNotSerialized_Keyed()
	{
		RecordWithReadOnlyPropertiesKeyed obj = new(1, 2);
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		Reader reader = new(msgpack, this.Serializer.Deformatter);

		// The Sum field should not be serialized.
		Assert.Equal(2, CountVectorElements(reader));
	}

	[Fact]
	public void SystemObject()
	{
		Assert.NotNull(this.Roundtrip<object, Witness>(new object()));
	}

	[Theory, PairwiseData]
	public async Task ReadOnlyCollectionProperties(bool isAsync)
	{
		var testData = new ClassWithReadOnlyCollectionProperties
		{
			Dictionary = { { "a", "b" }, { "c", "d" } },
			List = { "c" },
		};
		if (isAsync)
		{
			await this.AssertRoundtripAsync(testData);
		}
		else
		{
			this.AssertRoundtrip(testData);
		}
	}

	/// <summary>
	/// Verifies that an unexpected nil value doesn't disturb deserializing readonly collections.
	/// </summary>
	[Fact]
	public async Task ReadOnlyCollectionProperties_Nil()
	{
		ReadOnlySequence<byte> sequence = PrepareSequence();
		this.Serializer.Deserialize<ClassWithReadOnlyCollectionProperties>(sequence, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		await this.Serializer.DeserializeAsync<ClassWithReadOnlyCollectionProperties>(PipeReader.Create(sequence), Witness.ShapeProvider, TestContext.Current.CancellationToken);

		Sequence<byte> PrepareSequence()
		{
			Sequence<byte> sequence = new();
			Writer writer = new(sequence, this.Serializer.Formatter);
			writer.WriteStartMap(2);
			writer.Write("List");
			writer.WriteMapKeyValueSeparator();
			writer.WriteNull();
			writer.WriteMapPairSeparator();
			writer.Write("Dictionary");
			writer.WriteMapKeyValueSeparator();
			writer.WriteNull();
			writer.WriteEndMap();
			writer.Flush();
			return sequence;
		}
	}

	[Fact]
	public void ByteArraySerializedOptimally()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<byte[], Witness>([1, 2, 3]);
		Reader reader = new(msgpack, this.Serializer.Deformatter);
		Assert.Equal(this.IsTextFormat ? TokenType.String : TokenType.Binary, reader.NextTypeCode);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteMemorySerializedOptimally()
	{
		Memory<byte> original = new byte[] { 1, 2, 3 };
		Memory<byte> deserialized = this.Roundtrip<Memory<byte>, Witness>(original);
		Assert.Equal(original.ToArray(), deserialized.ToArray());
		Reader reader = new(this.lastRoundtrippedFormattedBytes, this.Serializer.Deformatter);
		Assert.Equal(this.IsTextFormat ? TokenType.String : TokenType.Binary, reader.NextTypeCode);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteReadOnlyMemorySerializedOptimally()
	{
		ReadOnlyMemory<byte> original = new byte[] { 1, 2, 3 };
		ReadOnlyMemory<byte> deserialized = this.Roundtrip<ReadOnlyMemory<byte>, Witness>(original);
		Assert.Equal(original.ToArray(), deserialized.ToArray());
		Reader reader = new(this.lastRoundtrippedFormattedBytes, this.Serializer.Deformatter);
		Assert.Equal(this.IsTextFormat ? TokenType.String : TokenType.Binary, reader.NextTypeCode);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteArrayCanDeserializeSuboptimally()
	{
		Sequence<byte> value = this.GetByteArrayAsActualArrayOfBytes();
		byte[]? result = this.Serializer.Deserialize<byte[], Witness>(value, TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.Equal<byte>([1, 2, 3], result);
	}

	[Fact]
	public void ByteMemoryCanDeserializeSuboptimally()
	{
		Sequence<byte> sequence = this.GetByteArrayAsActualArrayOfBytes();

		Memory<byte> result = this.Serializer.Deserialize<Memory<byte>, Witness>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal<byte>([1, 2, 3], result.ToArray());
	}

	[Fact]
	public void ByteReadOnlyMemoryCanDeserializeSuboptimally()
	{
		Sequence<byte> sequence = this.GetByteArrayAsActualArrayOfBytes();

		ReadOnlyMemory<byte> result = this.Serializer.Deserialize<ReadOnlyMemory<byte>, Witness>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal<byte>([1, 2, 3], result.ToArray());
	}

	[Fact]
	public void CustomConverterVsBuiltIn_TopLevel()
	{
		this.Serializer.RegisterConverter(new CustomStringConverter());
		Sequence<byte> seq = new();
		this.Serializer.Serialize<string, Witness>(seq, "Hello", TestContext.Current.CancellationToken);
		this.LogFormattedBytes(seq);
		Assert.Equal("HelloWR", this.Serializer.Deserialize<string, Witness>(seq, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void CustomConverterVsBuiltIn_SubLevel()
	{
		this.Serializer.RegisterConverter(new CustomStringConverter());
		Sequence<byte> seq = new();
		this.Serializer.Serialize(seq, new OtherPrimitiveTypes("Hello", false, 0, 0), TestContext.Current.CancellationToken);
		this.LogFormattedBytes(seq);
		Assert.Equal("HelloWR", this.Serializer.Deserialize<OtherPrimitiveTypes>(seq, TestContext.Current.CancellationToken)?.AString);
	}

	[Fact]
	public void SerializeObject_DeserializeObject()
	{
		Fruit value = new() { Seeds = 5 };

		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		this.Serializer.SerializeObject(ref writer, value, Witness.ShapeProvider.GetShape(typeof(Fruit))!, TestContext.Current.CancellationToken);
		writer.Flush();

		this.LogFormattedBytes(seq);

		Reader reader = new(seq, this.Serializer.Deformatter);
		Fruit? deserialized = (Fruit?)this.Serializer.DeserializeObject(ref reader, Witness.ShapeProvider.GetShape(typeof(Fruit))!, TestContext.Current.CancellationToken);
		Assert.Equal(value, deserialized);
	}

	[Fact]
	public void CtorParameterNameMatchesSerializedInsteadOfDeclaredName_Roundtrips()
	{
		this.AssertRoundtrip(new TypeWithConstructorParameterMatchingSerializedPropertyName(2));
	}

	[Fact]
	public void CtorParameterNameMatchesSerializedInsteadOfDeclaredName_DefaultValueWorks()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		writer.WriteStartMap(0);
		writer.WriteEndMap();
		writer.Flush();
		this.LogFormattedBytes(seq);

		TypeWithConstructorParameterMatchingSerializedPropertyName? deserialized =
			this.Serializer.Deserialize<TypeWithConstructorParameterMatchingSerializedPropertyName>(seq, TestContext.Current.CancellationToken);
		Assert.NotNull(deserialized);
		Assert.Equal(8, deserialized.Marshaled);
	}

	[Fact]
	public void ClassWithIndexerCanBeSerialized()
	{
		this.AssertRoundtrip(new ClassWithIndexer { Member = 3 });
	}

	/// <summary>
	/// Carefully writes a msgpack-encoded array of bytes.
	/// </summary>
	private Sequence<byte> GetByteArrayAsActualArrayOfBytes()
	{
		Sequence<byte> sequence = new();
		Writer writer = new(sequence, this.Serializer.Formatter);
		writer.WriteStartVector(3);
		writer.Write(1);
		writer.WriteVectorElementSeparator();
		writer.Write(2);
		writer.WriteVectorElementSeparator();
		writer.Write(3);
		writer.WriteEndVector();
		writer.Flush();
		this.LogFormattedBytes(sequence);
		return sequence;
	}

	public partial class MsgPack() : GeneralSerializerTests(CreateMsgPackSerializer());

	public partial class Json() : GeneralSerializerTests(CreateJsonSerializer());
}
