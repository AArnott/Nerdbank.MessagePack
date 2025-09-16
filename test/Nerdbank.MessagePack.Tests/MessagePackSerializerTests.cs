// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Xunit.Sdk;

public partial class MessagePackSerializerTests : MessagePackSerializerTestBase
{
	public enum SomeEnum
	{
		A,
		B,
		C,
	}

	/// <summary>
	/// Verifies that properties are independent on each instance of <see cref="MessagePackSerializer"/>
	/// of properties on other instances.
	/// </summary>
	[Fact]
	public void PropertiesAreIndependent()
	{
		this.Serializer = this.Serializer with { SerializeEnumValuesByName = true };
		MessagePackSerializer s1 = this.Serializer with { InternStrings = true };
		MessagePackSerializer s2 = this.Serializer with { InternStrings = false };

		s1 = s1 with { SerializeEnumValuesByName = false };
		Assert.True(s2.SerializeEnumValuesByName);
	}

	[Fact]
	public void SimpleNull() => this.AssertRoundtrip<Fruit>(null);

	[Fact]
	public void SimplePoco() => this.AssertRoundtrip(new Fruit { Seeds = 18 });

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
		catch (MessagePackSerializationException ex) when (ex.InnerException is PlatformNotSupportedException)
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
		MessagePackReader reader = new(msgpack);

		// The Sum field should not be serialized.
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[Fact]
	public void ReadOnlyPropertiesNotSerialized_NoCtor()
	{
		RecordWithReadOnlyProperties_NoConstructor obj = new(1, 2);
		byte[] msgpack = this.Serializer.Serialize(obj, TestContext.Current.CancellationToken);
		this.Logger.WriteLine(this.Serializer.ConvertToJson(msgpack));
		MessagePackReader reader = new(msgpack);

		// The Sum field should not be serialized.
		Assert.Equal(2, reader.ReadMapHeader());
	}

	[Fact]
	public void ReadOnlyPropertiesNotSerialized_Keyed()
	{
		RecordWithReadOnlyPropertiesKeyed obj = new(1, 2);
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		MessagePackReader reader = new(msgpack);

		// The Sum field should not be serialized.
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Fact]
	public void SystemObject()
	{
		Assert.NotNull(this.Roundtrip<object, Witness>(new object()));
	}

	[Fact]
	public async Task ReadOnlyCollectionProperties()
	{
		var testData = new ClassWithReadOnlyCollectionProperties
		{
			Dictionary = { { "a", "b" }, { "c", "d" } },
			List = { "c" },
		};
		this.AssertRoundtrip(testData);
		await this.AssertRoundtripAsync(testData);
	}

	[Fact]
	public void ReadOnlyObjectProperty_IsNotSerialized()
	{
		ClassWithReadOnlyObjectProperty obj = new() { AgeAccessor = 15 };
		byte[] msgpack = this.Serializer.Serialize(obj, TestContext.Current.CancellationToken);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Fact]
	public void ReadOnlyObjectPropertyWithCtorParameter_IsSerialized()
	{
		ClassWithReadOnlyObjectPropertyAndCtorParam obj = new(15);
		this.AssertRoundtrip(obj);
	}

	/// <summary>
	/// Verifies that an unexpected nil value doesn't disturb deserializing readonly collections.
	/// </summary>
	[Fact]
	public async Task ReadOnlyCollectionProperties_Nil()
	{
		ReadOnlySequence<byte> sequence = PrepareSequence();
		this.Serializer.Deserialize<ClassWithReadOnlyCollectionProperties>(sequence, TestContext.Current.CancellationToken);
		await this.Serializer.DeserializeAsync<ClassWithReadOnlyCollectionProperties>(PipeReader.Create(sequence), TestContext.Current.CancellationToken);

		Sequence<byte> PrepareSequence()
		{
			Sequence<byte> sequence = new();
			MessagePackWriter writer = new(sequence);
			writer.WriteMapHeader(2);
			writer.Write("List");
			writer.WriteNil();
			writer.Write("Dictionary");
			writer.WriteNil();
			writer.Flush();
			return sequence;
		}
	}

	[Fact]
	public void ByteArraySerializedOptimally()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<byte[], Witness>([1, 2, 3]);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(MessagePackType.Binary, reader.NextMessagePackType);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteMemorySerializedOptimally()
	{
		Memory<byte> original = new byte[] { 1, 2, 3 };
		Memory<byte> deserialized = this.Roundtrip<Memory<byte>, Witness>(original);
		Assert.Equal(original.ToArray(), deserialized.ToArray());
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(MessagePackType.Binary, reader.NextMessagePackType);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteReadOnlyMemorySerializedOptimally()
	{
		ReadOnlyMemory<byte> original = new byte[] { 1, 2, 3 };
		ReadOnlyMemory<byte> deserialized = this.Roundtrip<ReadOnlyMemory<byte>, Witness>(original);
		Assert.Equal(original.ToArray(), deserialized.ToArray());
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(MessagePackType.Binary, reader.NextMessagePackType);
		Assert.NotNull(reader.ReadBytes());
	}

	[Fact]
	public void ByteArrayCanDeserializeSuboptimally()
	{
		Sequence<byte> sequence = GetByteArrayAsActualMsgPackArray();

		byte[]? result = this.Serializer.Deserialize<byte[], Witness>(sequence, TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.Equal<byte>([1, 2, 3], result);
	}

	[Fact]
	public void ByteMemoryCanDeserializeSuboptimally()
	{
		Sequence<byte> sequence = GetByteArrayAsActualMsgPackArray();

		Memory<byte> result = this.Serializer.Deserialize<Memory<byte>, Witness>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal<byte>([1, 2, 3], result.ToArray());
	}

	[Fact]
	public void ByteReadOnlyMemoryCanDeserializeSuboptimally()
	{
		Sequence<byte> sequence = GetByteArrayAsActualMsgPackArray();

		ReadOnlyMemory<byte> result = this.Serializer.Deserialize<ReadOnlyMemory<byte>, Witness>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal<byte>([1, 2, 3], result.ToArray());
	}

	[Fact]
	public void CustomConverterVsBuiltIn_TopLevel()
	{
		this.Serializer = this.Serializer with { Converters = [new CustomStringConverter()] };
		byte[] msgpack = this.Serializer.Serialize<string, Witness>("Hello", TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);
		Assert.Equal("HelloWR", this.Serializer.Deserialize<string, Witness>(msgpack, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void CustomConverterVsBuiltIn_SubLevel()
	{
		this.Serializer = this.Serializer with { Converters = [new CustomStringConverter()] };
		byte[] msgpack = this.Serializer.Serialize(new OtherPrimitiveTypes("Hello", false, 0, 0), TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);
		Assert.Equal("HelloWR", this.Serializer.Deserialize<OtherPrimitiveTypes>(msgpack, TestContext.Current.CancellationToken)?.AString);
	}

	[Fact]
	public void SerializeObject_DeserializeObject()
	{
		Fruit value = new() { Seeds = 5 };

		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		this.Serializer.SerializeObject(ref writer, value, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit)), TestContext.Current.CancellationToken);
		writer.Flush();

		this.LogMsgPack(seq);

		MessagePackReader reader = new(seq);
		Fruit? deserialized = (Fruit?)this.Serializer.DeserializeObject(ref reader, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit)), TestContext.Current.CancellationToken);
		Assert.Equal(value, deserialized);
	}

	[Fact]
	public void SerializeObject_ByteArray()
	{
		Fruit value = new() { Seeds = 5 };
		ITypeShape shape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit));
		byte[] serialized = this.Serializer.SerializeObject(value, shape, TestContext.Current.CancellationToken);
		Fruit? deserialized = (Fruit?)this.Serializer.DeserializeObject(serialized, shape, TestContext.Current.CancellationToken);
		Assert.Equal(value, deserialized);
	}

	[Fact]
	public void SerializeObject_IBufferWriter()
	{
		Fruit value = new() { Seeds = 5 };
		ITypeShape shape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit));
		var bufferWriter = new Sequence<byte>();
		this.Serializer.SerializeObject(bufferWriter, value, shape, TestContext.Current.CancellationToken);
		Fruit? deserialized = (Fruit?)this.Serializer.DeserializeObject(bufferWriter.AsReadOnlySequence, shape, TestContext.Current.CancellationToken);
		Assert.Equal(value, deserialized);
	}

	[Fact]
	public void SerializeObject_Stream()
	{
		Fruit value = new() { Seeds = 5 };
		ITypeShape shape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit));
		var stream = new MemoryStream();
		this.Serializer.SerializeObject(stream, value, shape, TestContext.Current.CancellationToken);
		stream.Position = 0;
		Fruit? deserialized = (Fruit?)this.Serializer.DeserializeObject(stream, shape, TestContext.Current.CancellationToken);
		Assert.Equal(value, deserialized);
	}

	[Fact]
	public async Task SerializeObjectAsync_Stream()
	{
		Fruit value = new() { Seeds = 5 };
		ITypeShape shape = Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow(typeof(Fruit));
		var stream = new MemoryStream();
		await this.Serializer.SerializeObjectAsync(stream, value, shape, TestContext.Current.CancellationToken);
		stream.Position = 0;
		Fruit? deserialized = (Fruit?)await this.Serializer.DeserializeObjectAsync(stream, shape, TestContext.Current.CancellationToken);
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
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(0);
		writer.Flush();

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

	[Fact]
	public void TupleSerializedAsArray()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<Tuple<int, bool>, Witness>(new(1, true));
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Fact]
	public void ValueTupleSerializedAsArray()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<(int, bool), Witness>(new(1, true));
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Fact]
	public void IImmutableList()
	{
		IImmutableList<int> list = ImmutableList.Create(1, 2, 3);
		IImmutableList<int>? deserialized = this.Roundtrip<IImmutableList<int>, Witness>(list);
		Assert.NotNull(deserialized);
		Assert.Equal(list, deserialized);
	}

	/// <summary>
	/// Regression test for <see href="https://github.com/AArnott/Nerdbank.MessagePack/issues/416">issue 416</see>.
	/// </summary>
	[Fact]
	public void WriteLargeStringToStream()
	{
		string value = new string('x', 100 * 1024);
		this.Serializer.Serialize(Stream.Null, value, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// Verifies that an object-keyed dictionary is not supported.
	/// </summary>
	/// <remarks>
	/// Object-keys are not supported both because they leave nothing to be serialized and because they cannot be securely hashed.
	/// Manual verification of the logged output should confirm that the exception message is helpful.
	/// </remarks>
	[Fact]
	public void ObjectKeyedCollections()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<System.Collections.IDictionary>(new Dictionary<string, object>(), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken));
		NotSupportedException innerException = Assert.IsType<NotSupportedException>(ex.GetBaseException());
		this.Logger.WriteLine(innerException.Message);

		ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<IDictionary<object, string>>(new Dictionary<object, string>(), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken));
		innerException = Assert.IsType<NotSupportedException>(ex.GetBaseException());
		this.Logger.WriteLine(innerException.Message);

		ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(new HashSet<object>(), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken));
		innerException = Assert.IsType<NotSupportedException>(ex.GetBaseException());
		this.Logger.WriteLine(innerException.Message);

		this.Logger.WriteLine(ex.ToString());
	}

	[Fact]
	public void CustomDictionaryWithCustomConverter()
	{
		HasCustomDictionary original = new(new CustomDictionary<string, int> { { "a", 1 }, { "b", 2 } });
		HasCustomDictionary? deserialized = this.Roundtrip(original);
		Assert.Equal(original.Dict.Count, deserialized?.Dict.Count);

		// Assert that the custom converter was used by verifying the serialized form.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(HasCustomDictionary.Dict), reader.ReadString());

		// What makes our custom dictionary unique is that its custom converter uses arrays instead of maps.
		Assert.Equal(2 * 2, reader.ReadArrayHeader());
	}

	[Fact]
	public void CustomListWithCustomConverter()
	{
		HasCustomList original = new(new CustomList<string> { "hi" });
		HasCustomList? deserialized = this.Roundtrip(original);
		Assert.Equal(original.List.Count, deserialized?.List.Count);

		// Assert that the custom converter was used by verifying the serialized form.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(HasCustomList.List), reader.ReadString());

		// What makes our custom list unique is that its array is +1 too long.
		Assert.Equal(2, reader.ReadArrayHeader());
	}

	[Fact]
	public void CustomConverterOnParameter()
	{
		HasCustomConverterOnParameter original = new(10);
		HasCustomConverterOnParameter? deserialized = this.Roundtrip(original);
		Assert.Equal(original.Value / 2, deserialized?.Value);
	}

	/// <summary>
	/// Carefully writes a msgpack-encoded array of bytes.
	/// </summary>
	private static Sequence<byte> GetByteArrayAsActualMsgPackArray()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(3);
		writer.Write(1);
		writer.Write(2);
		writer.Write(3);
		writer.Flush();
		return sequence;
	}

	[GenerateShape]
	public partial class KeyedCollections
	{
		public required HashSet<string> StringSet { get; set; }

		public required Dictionary<string, int> StringDictionary { get; set; }

		public required HashSet<Fruit> FruitSet { get; set; }

		public required Dictionary<Fruit, int> FruitDictionary { get; set; }
	}

	[GenerateShape]
	public partial class ClassWithReadOnlyCollectionProperties : IEquatable<ClassWithReadOnlyCollectionProperties>
	{
		public List<string> List { get; } = new();

		public Dictionary<string, string> Dictionary { get; } = new();

		public bool Equals(ClassWithReadOnlyCollectionProperties? other)
			=> StructuralEquality.Equal(this.List, other?.List) && StructuralEquality.Equal(this.Dictionary, other?.Dictionary);
	}

	[GenerateShape]
	public partial class ClassWithReadOnlyObjectProperty : IEquatable<ClassWithReadOnlyObjectProperty>
	{
		public int Age => this.AgeAccessor;

		internal int AgeAccessor { get; set; }

		public bool Equals(ClassWithReadOnlyObjectProperty? other) => other is not null && this.Age == other.Age;
	}

	[GenerateShape]
	public partial class ClassWithReadOnlyObjectPropertyAndCtorParam : IEquatable<ClassWithReadOnlyObjectPropertyAndCtorParam>
	{
		public ClassWithReadOnlyObjectPropertyAndCtorParam(int age) => this.AgeAccessor = age;

		public int Age => this.AgeAccessor;

		internal int AgeAccessor { get; set; }

		public bool Equals(ClassWithReadOnlyObjectPropertyAndCtorParam? other) => other is not null && this.Age == other.Age;
	}

	[GenerateShape]
	public partial class Fruit : IEquatable<Fruit>
	{
		public int Seeds { get; set; }

		public bool Equals(Fruit? other) => other is not null && this.Seeds == other.Seeds;
	}

	[GenerateShape]
	public partial class IntRichPoco : IEquatable<IntRichPoco>
	{
		public byte UInt8 { get; set; }

		public ushort UInt16 { get; set; }

		public uint UInt32 { get; set; }

		public ulong UInt64 { get; set; }

		public sbyte Int8 { get; set; }

		public short Int16 { get; set; }

		public int Int32 { get; set; }

		public long Int64 { get; set; }

		public bool Equals(IntRichPoco? other)
			=> other is not null
			&& this.UInt8 == other.UInt8
			&& this.UInt16 == other.UInt16
			&& this.UInt32 == other.UInt32
			&& this.UInt64 == other.UInt64
			&& this.Int8 == other.Int8
			&& this.Int16 == other.Int16
			&& this.Int32 == other.Int32
			&& this.Int64 == other.Int64;
	}

	[GenerateShape]
	public partial record OtherPrimitiveTypes(string AString, bool ABoolean, float AFloat, double ADouble);

	[GenerateShape]
	public partial class EmptyClass : IEquatable<EmptyClass>
	{
		public bool Equals(EmptyClass? other) => other is not null;
	}

	[GenerateShape]
	public partial record RecordClass(int Seeds)
	{
		public int Weight { get; set; }

		public int ChildNumber { get; init; }
	}

	[GenerateShape]
	public partial class DefaultCtorWithInitProperty : IEquatable<DefaultCtorWithInitProperty>
	{
		public int Age { get; init; }

		public bool Equals(DefaultCtorWithInitProperty? other) => other is not null && this.Age == other.Age;
	}

	[GenerateShape]
	public partial record RecordWithNullableStruct(int? Value);

	[GenerateShape]
	public partial record HasCustomConverterOnParameter
	{
		public HasCustomConverterOnParameter([MessagePackConverter(typeof(IntDoublingConverter))] int value)
		{
			this.Value = value;
		}

		public int Value { get; }
	}

	[GenerateShape]
	public partial record HasCustomDictionary(CustomDictionary<string, int> Dict);

	[MessagePackConverter(typeof(CustomDictionaryConverter<,>))]
	public class CustomDictionary<TKey, TValue> : IDictionary<TKey, TValue>
		where TKey : notnull
	{
		private readonly Dictionary<TKey, TValue> inner = new();

		public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)this.inner).Keys;

		public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)this.inner).Values;

		public int Count => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).Count;

		public bool IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).IsReadOnly;

		public TValue this[TKey key]
		{
			get => ((IDictionary<TKey, TValue>)this.inner)[key];
			set => ((IDictionary<TKey, TValue>)this.inner)[key] = value;
		}

		public void Add(TKey key, TValue value) => ((IDictionary<TKey, TValue>)this.inner).Add(key, value);

		public void Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).Add(item);

		public void Clear() => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).Clear();

		public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).Contains(item);

		public bool ContainsKey(TKey key) => ((IDictionary<TKey, TValue>)this.inner).ContainsKey(key);

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).CopyTo(array, arrayIndex);

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this.inner).GetEnumerator();

		public bool Remove(TKey key) => ((IDictionary<TKey, TValue>)this.inner).Remove(key);

		public bool Remove(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)this.inner).Remove(item);

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => ((IDictionary<TKey, TValue>)this.inner).TryGetValue(key, out value);
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();
	}

	[GenerateShape]
	public partial record HasCustomList(CustomList<string> List);

	[MessagePackConverter(typeof(CustomListConverter<>))]
	public class CustomList<T> : IList<T>
	{
		private readonly List<T> inner = new();

		public int Count => ((ICollection<T>)this.inner).Count;

		public bool IsReadOnly => ((ICollection<T>)this.inner).IsReadOnly;

		public T this[int index]
		{
			get => ((IList<T>)this.inner)[index];
			set => ((IList<T>)this.inner)[index] = value;
		}

		public void Add(T item) => ((ICollection<T>)this.inner).Add(item);

		public void Clear() => ((ICollection<T>)this.inner).Clear();

		public bool Contains(T item) => ((ICollection<T>)this.inner).Contains(item);

		public void CopyTo(T[] array, int arrayIndex) => ((ICollection<T>)this.inner).CopyTo(array, arrayIndex);

		public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)this.inner).GetEnumerator();

		public int IndexOf(T item) => ((IList<T>)this.inner).IndexOf(item);

		public void Insert(int index, T item) => ((IList<T>)this.inner).Insert(index, item);

		public bool Remove(T item) => ((ICollection<T>)this.inner).Remove(item);

		public void RemoveAt(int index) => ((IList<T>)this.inner).RemoveAt(index);

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.inner).GetEnumerator();
	}

	[GenerateShape]
	public partial class ClassWithDictionary : IEquatable<ClassWithDictionary>
	{
		public Dictionary<string, int>? StringInt { get; set; }

		public bool Equals(ClassWithDictionary? other) => other is not null && StructuralEquality.Equal(this.StringInt, other.StringInt);
	}

	[GenerateShape]
	public partial class ClassWithImmutableDictionary : IEquatable<ClassWithImmutableDictionary>
	{
		public ImmutableDictionary<string, int>? StringInt { get; set; }

		public bool Equals(ClassWithImmutableDictionary? other) => other is not null && StructuralEquality.Equal(this.StringInt, other.StringInt);
	}

	[GenerateShape]
	public partial class ClassWithArray : IEquatable<ClassWithArray>
	{
		public int[]? IntArray { get; set; }

		public bool Equals(ClassWithArray? other) => other is not null && StructuralEquality.Equal(this.IntArray, other.IntArray);
	}

	[GenerateShape]
	public partial class ClassWithEnumerable : IEquatable<ClassWithEnumerable>
	{
		public IEnumerable<int>? IntEnum { get; set; }

		public bool Equals(ClassWithEnumerable? other) => other is not null && StructuralEquality.Equal(this.IntEnum, other.IntEnum);
	}

	[GenerateShape]
	public partial record HasEnum(SomeEnum Value);

	[GenerateShape]
	public partial class HasMultiDimensionalArray : IEquatable<HasMultiDimensionalArray>
	{
		public int[,]? Array2D { get; set; }

		public int[,,]? Array3D { get; set; }

		public bool Equals(HasMultiDimensionalArray? other) => other is not null && StructuralEquality.Equal<int>(this.Array2D, other.Array2D) && StructuralEquality.Equal<int>(this.Array3D, other.Array3D);
	}

	public record UnannotatedPoco
	{
		public int Value { get; set; }
	}

	[GenerateShape]
	public partial record ReferencesUnannotatedPoco
	{
		public UnannotatedPoco? Poco { get; set; }
	}

	[GenerateShape]
	internal partial record InternalRecordWithPrivateField
	{
		[PropertyShape]
		private int privateField;

		[PropertyShape(Ignore = true)]
		internal int PrivateFieldAccessor
		{
			get => this.privateField;
			set => this.privateField = value;
		}

		[PropertyShape(Ignore = true)]
		internal int PrivatePropertyAccessor
		{
			get => this.PrivateProperty;
			set => this.PrivateProperty = value;
		}

		[PropertyShape]
		private int PrivateProperty { get; set; }
	}

	[GenerateShape]
	internal partial record RecordWithReadOnlyProperties(int A, int B)
	{
		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial class RecordWithReadOnlyProperties_NoConstructor
	{
		internal RecordWithReadOnlyProperties_NoConstructor(int a, int b)
		{
			this.A = a;
			this.B = b;
		}

		public int A { get; set; }

		public int B { get; set; }

		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial record RecordWithReadOnlyPropertiesKeyed([property: Key(0)] int A, [property: Key(1)] int B)
	{
		[PropertyShape(Ignore = true)]
		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial record TypeWithConstructorParameterMatchingSerializedPropertyName
	{
		public TypeWithConstructorParameterMatchingSerializedPropertyName(int otherName = 8)
			=> this.Marshaled = otherName;

		[PropertyShape(Name = "otherName")]
		public int Marshaled { get; set; }
	}

	[GenerateShape]
	internal partial class ClassWithIndexer
	{
		public int Member { get; set; }

		public int this[int index] => index;

		public override bool Equals(object? obj) => obj is ClassWithIndexer other && this.Member == other.Member;

		public override int GetHashCode() => this.Member;
	}

	internal class CustomDictionaryConverter<TKey, TValue> : MessagePackConverter<CustomDictionary<TKey, TValue>>
		where TKey : notnull
	{
		public override CustomDictionary<TKey, TValue>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			CustomDictionary<TKey, TValue> dict = new();
			int count = reader.ReadArrayHeader();

			MessagePackConverter<TKey> keyConverter = context.GetConverter<TKey>(null);
			MessagePackConverter<TValue> valueConverter = context.GetConverter<TValue>(null);

			for (int i = 0; i < count; i += 2)
			{
				dict.Add(keyConverter.Read(ref reader, context)!, valueConverter.Read(ref reader, context)!);
			}

			return dict;
		}

		public override void Write(ref MessagePackWriter writer, in CustomDictionary<TKey, TValue>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(value.Count * 2);

			MessagePackConverter<TKey> keyConverter = context.GetConverter<TKey>(null);
			MessagePackConverter<TValue> valueConverter = context.GetConverter<TValue>(null);

			foreach (KeyValuePair<TKey, TValue> kvp in value)
			{
				keyConverter.Write(ref writer, kvp.Key, context);
				valueConverter.Write(ref writer, kvp.Value, context);
			}
		}
	}

	internal class CustomListConverter<T> : MessagePackConverter<CustomList<T>>
	{
		public override CustomList<T>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count < 1)
			{
				throw new MessagePackSerializationException("Expected at least one item in the array to distinguish this from a regular list.");
			}

			reader.ReadNil(); // something odd to distinguish this from a regular list

			CustomList<T> list = new();
			MessagePackConverter<T> itemConverter = context.GetConverter<T>(null);

			for (int i = 1; i < count; i++)
			{
				list.Add(itemConverter.Read(ref reader, context)!);
			}

			return list;
		}

		public override void Write(ref MessagePackWriter writer, in CustomList<T>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(value.Count + 1);
			writer.WriteNil(); // something odd to distinguish this from a regular list

			MessagePackConverter<T> itemConverter = context.GetConverter<T>(null);

			foreach (T item in value)
			{
				itemConverter.Write(ref writer, item, context);
			}
		}
	}

	internal class IntDoublingConverter : MessagePackConverter<int>
	{
		public override int Read(ref MessagePackReader reader, SerializationContext context)
		{
			int value = reader.ReadInt32();
			return value / 2; // Halve the value when deserializing
		}

		public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context)
		{
			writer.Write(value * 2); // Double the value when serializing
		}
	}

	[GenerateShapeFor<UnannotatedPoco>]
	[GenerateShapeFor<object>]
	[GenerateShapeFor<string>]
	[GenerateShapeFor<byte[]>]
	[GenerateShapeFor<Memory<byte>>]
	[GenerateShapeFor<ReadOnlyMemory<byte>>]
	[GenerateShapeFor<IImmutableList<int>>]
	[GenerateShapeFor<Tuple<int, bool>>]
	[GenerateShapeFor<(int, bool)>]
	[GenerateShapeFor<System.Collections.IDictionary>]
	[GenerateShapeFor<IDictionary<object, string>>]
	[GenerateShapeFor<HashSet<object>>]
	internal partial class Witness;

	private class CustomStringConverter : MessagePackConverter<string>
	{
		public override string? Read(ref MessagePackReader reader, SerializationContext context)
			=> reader.ReadString() + "R";

		public override void Write(ref MessagePackWriter writer, in string? value, SerializationContext context)
			=> writer.Write(value + "W");
	}
}
