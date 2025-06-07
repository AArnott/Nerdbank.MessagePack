// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ObjectsAsArraysTests : MessagePackSerializerTestBase
{
	[Theory, PairwiseData]
	public async Task Person_Roundtrip(bool async)
	{
		var person = new Person { FirstName = "Andrew", LastName = "Arnott" };
		if (async)
		{
			await this.AssertRoundtripAsync(person);
		}
		else
		{
			this.AssertRoundtrip(person);
		}
	}

	[Fact]
	public void PersonWithDefaultConstructor_Roundtrip() => this.AssertRoundtrip(new PersonWithDefaultConstructor { FirstName = "Andrew", LastName = "Arnott" });

	[Fact]
	public void Null() => this.AssertRoundtrip<Person>(null);

	[Fact]
	public void Null_DefaultCtro() => this.AssertRoundtrip<PersonWithDefaultConstructor>(null);

	[Fact]
	public void Person_SerializesAsArray()
	{
		Person person = new() { FirstName = "Andrew", LastName = "Arnott" };
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, person, TestContext.Current.CancellationToken);
		this.LogMsgPack(buffer);

		MessagePackReader reader = new(buffer);
		Assert.Equal(3, reader.ReadArrayHeader());
		Assert.Equal("Andrew", reader.ReadString());
		Assert.True(reader.TryReadNil());
		Assert.Equal("Arnott", reader.ReadString());
		Assert.True(reader.End);

		Person? deserialized = this.Serializer.Deserialize<Person>(buffer, TestContext.Current.CancellationToken);
		Assert.Equal(person, deserialized);
	}

	[Trait("ShouldSerialize", "true")]
	[Theory, PairwiseData]
	public async Task Person_WithoutLastName(bool async)
	{
		this.Serializer = this.Serializer with
		{
			SerializeDefaultValues = SerializeDefaultValuesPolicy.Never,
			DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties,
		};

		// The most compact representation of this is an array of length 1.
		// Verify that this is what the converter chose.
		Person person = new() { FirstName = "Andrew", LastName = null };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadArrayHeader());
	}

	[Trait("ShouldSerialize", "true")]
	[Theory, PairwiseData]
	public async Task Person_WithoutFirstName(bool async)
	{
		this.Serializer = this.Serializer with
		{
			SerializeDefaultValues = SerializeDefaultValuesPolicy.Never,
			DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties,
		};

		// The most compact representation of this is a map of length 1.
		// Verify that this is what the converter chose.
		Person person = new() { FirstName = null, LastName = "Arnott" };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadMapHeader());
	}

	[Trait("ShouldSerialize", "true")]
	[Theory, PairwiseData]
	public async Task Person_AllDefaultValues(bool async)
	{
		this.Serializer = this.Serializer with
		{
			SerializeDefaultValues = SerializeDefaultValuesPolicy.Never,
			DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties,
		};

		// The most compact representation of this is a map of length 1.
		// Verify that this is what the converter chose.
		Person person = new Person { FirstName = null, LastName = null };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(0, reader.ReadArrayHeader());
	}

	[Theory, PairwiseData]
	public async Task Person_UnexpectedlyLongArray(bool async)
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(4);
		writer.Write("A");
		writer.WriteNil();
		writer.Write("B");
		writer.Write("C"); // This should be ignored.
		writer.Flush();

		Person? person = async
			? await this.Serializer.DeserializeAsync<Person>(PipeReader.Create(sequence), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<Person>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal(new Person { FirstName = "A", LastName = "B" }, person);
	}

	[Theory, PairwiseData]
	public async Task Person_UnknownIndexesInMap(bool async)
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(3);

		writer.Write(0);
		writer.Write("A");

		writer.Write(15);  // This should be ignored.
		writer.Write("C");

		writer.Write(2);
		writer.Write("B");
		writer.Flush();

		Person? person = async
			? await this.Serializer.DeserializeAsync<Person>(PipeReader.Create(sequence), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<Person>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal(new Person { FirstName = "A", LastName = "B" }, person);
	}

	[Theory, PairwiseData]
	public async Task PersonWithDefaultConstructor_WithoutLastName(bool async)
	{
		// The most compact representation of this is an array of length 1.
		// Verify that this is what the converter chose.
		PersonWithDefaultConstructor person = new() { FirstName = "Andrew", LastName = null };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadArrayHeader());
	}

	[Theory, PairwiseData]
	public async Task PersonWithDefaultConstructor_WithoutFirstName(bool async)
	{
		// The most compact representation of this is a map of length 1.
		// Verify that this is what the converter chose.
		PersonWithDefaultConstructor person = new() { FirstName = null, LastName = "Arnott" };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadMapHeader());
	}

	[Trait("ShouldSerialize", "true")]
	[Theory, PairwiseData]
	public async Task PersonWithDefaultConstructor_AllDefaultValues(
		bool async,
		[CombinatorialValues(SerializeDefaultValuesPolicy.Always, SerializeDefaultValuesPolicy.Never)] SerializeDefaultValuesPolicy serializeDefaultValues)
	{
		// The most compact representation of this is a map of length 1.
		// Verify that this is what the converter chose, iff we're in that mode.
		this.Serializer = this.Serializer with { SerializeDefaultValues = serializeDefaultValues };

		PersonWithDefaultConstructor person = new() { FirstName = null, LastName = null };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(person) : this.AssertRoundtrip(person);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(serializeDefaultValues == SerializeDefaultValuesPolicy.Always ? 3 : 0, reader.ReadArrayHeader());
	}

	[Theory, PairwiseData]
	public async Task PersonWithDefaultConstructor_UnexpectedlyLongArray(bool async)
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(4);
		writer.Write("A");
		writer.WriteNil();
		writer.Write("B");
		writer.Write("C"); // This should be ignored.
		writer.Flush();

		PersonWithDefaultConstructor? person = async
			? await this.Serializer.DeserializeAsync<PersonWithDefaultConstructor>(PipeReader.Create(sequence), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<PersonWithDefaultConstructor>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal(new PersonWithDefaultConstructor { FirstName = "A", LastName = "B" }, person);
	}

	[Theory, PairwiseData]
	public async Task PersonWithDefaultConstructor_UnknownIndexesInMap(bool async)
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(3);

		writer.Write(0);
		writer.Write("A");

		writer.Write(15);  // This should be ignored.
		writer.Write("C");

		writer.Write(2);
		writer.Write("B");
		writer.Flush();

		PersonWithDefaultConstructor? person = async
			? await this.Serializer.DeserializeAsync<PersonWithDefaultConstructor>(PipeReader.Create(sequence), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<PersonWithDefaultConstructor>(sequence, TestContext.Current.CancellationToken);
		Assert.Equal(new PersonWithDefaultConstructor { FirstName = "A", LastName = "B" }, person);
	}

	[Fact]
	public async Task AsyncAndSyncPropertyMix()
	{
		FamilyWithAsyncProperties family = new()
		{
			Father = new() { FirstName = "Dad", LastName = "family" },
			Mother = new() { FirstName = "Mom", LastName = "family" },
			FirstChild = new() { FirstName = "Child", LastName = "family" },
			FamilySize = 3,
		};

		ReadOnlySequence<byte> msgpack = await this.AssertRoundtripAsync(family);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(5, reader.ReadArrayHeader());
	}

	[Trait("ShouldSerialize", "true")]
	[Fact]
	public async Task AsyncAndSyncPropertyMix_AsMap()
	{
		this.Serializer = this.Serializer with
		{
			SerializeDefaultValues = SerializeDefaultValuesPolicy.Never,
			DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties,
		};

		// Initialize a default late in the array to motivate serialization as a map.
		FamilyWithAsyncProperties family = new()
		{
			Father = null,
			Mother = null,
			FirstChild = new() { FirstName = "Child", LastName = "family" },
			FamilySize = 0,
		};

		ReadOnlySequence<byte> msgpack = await this.AssertRoundtripAsync(family);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadMapHeader());
	}

	[Theory, PairwiseData]
	public async Task AsyncAndSyncPropertyMix_ReadMapFromNonContiguousBuffer(bool breakBeforeIndex)
	{
		this.Serializer = this.Serializer with
		{
			DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties,
		};

		FamilyWithAsyncProperties expectedFamily = new()
		{
			Father = null,
			Mother = null,
			FirstChild = new() { FirstName = "child", LastName = "family" },
			FamilySize = 2,
		};

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(2);

		writer.Write(3);
		writer.Write(expectedFamily.FamilySize);

		writer.Flush();
		long positionBeforeIndex = sequence.Length;
		writer.Write(4);
		writer.Flush();
		long positionAfterIndex = sequence.Length;

		this.Serializer.Serialize(ref writer, expectedFamily.FirstChild, GetShape<Person>(), TestContext.Current.CancellationToken);
		writer.Flush();
		this.LogMsgPack(sequence);

		// Split the buffer up.
		long splitPosition = breakBeforeIndex ? positionBeforeIndex : positionAfterIndex;
		byte[] firstSegment = sequence.AsReadOnlySequence.Slice(0, splitPosition).ToArray();
		byte[] secondSegment = sequence.AsReadOnlySequence.Slice(splitPosition).ToArray();

		// Recombine as a sequence as two segments.
		sequence.Reset();
		sequence.Append(firstSegment);
		sequence.Append(secondSegment);

		// Deserialize, through a pipe that lets us control the buffer segments.
		FragmentedPipeReader pipeReader = new(sequence, sequence.AsReadOnlySequence.GetPosition(splitPosition));

		FamilyWithAsyncProperties? family = await this.Serializer.DeserializeAsync<FamilyWithAsyncProperties>(pipeReader, TestContext.Current.CancellationToken);

		Assert.Equal(expectedFamily, family);
	}

	[Theory, PairwiseData]
	public async Task AsyncAndSyncPropertyMix_ReadMapFromNonContiguousBuffer_DefaultCtor(bool breakBeforeIndex)
	{
		FamilyWithAsyncPropertiesWithDefaultCtor expectedFamily = new()
		{
			Father = null,
			Mother = null,
			FirstChild = new() { FirstName = "child", LastName = "family" },
			FamilySize = 2,
		};

		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(2);

		writer.Write(3);
		writer.Write(expectedFamily.FamilySize);

		writer.Flush();
		long positionBeforeIndex = sequence.Length;
		writer.Write(4);
		writer.Flush();
		long positionAfterIndex = sequence.Length;

		this.Serializer.Serialize(ref writer, expectedFamily.FirstChild, GetShape<Person>(), TestContext.Current.CancellationToken);
		writer.Flush();
		this.LogMsgPack(sequence);

		// Split the buffer up.
		long splitPosition = breakBeforeIndex ? positionBeforeIndex : positionAfterIndex;
		byte[] firstSegment = sequence.AsReadOnlySequence.Slice(0, splitPosition).ToArray();
		byte[] secondSegment = sequence.AsReadOnlySequence.Slice(splitPosition).ToArray();

		// Recombine as a sequence as two segments.
		sequence.Reset();
		sequence.Append(firstSegment);
		sequence.Append(secondSegment);

		// Deserialize, through a pipe that lets us control the buffer segments.
		FragmentedPipeReader pipeReader = new(sequence, sequence.AsReadOnlySequence.GetPosition(splitPosition));

		FamilyWithAsyncPropertiesWithDefaultCtor? family = await this.Serializer.DeserializeAsync<FamilyWithAsyncPropertiesWithDefaultCtor>(pipeReader, TestContext.Current.CancellationToken);

		Assert.Equal(expectedFamily, family);
	}

	[Fact]
	public void PropertyGettersIgnored()
	{
		ClassWithUnserializedPropertyGetters obj = new() { Value = true };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadArrayHeader());
	}

	[Fact]
	public void PropertyGetterWithCtorParamAndMissingKey()
	{
		ClassWithPropertyGettersWithCtorParamAndMissingKey obj = new("hi") { Value = true };

		// We expect this to throw because a qualified property is not attributed with KeyAttribute.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(obj, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void PropertyGetterWithCtorParam()
	{
		ClassWithPropertyGettersWithCtorParam obj = new(true);
		this.AssertRoundtrip(obj);
	}

	private static ITypeShape<T> GetShape<T>()
#if NET
		where T : IShapeable<T> => T.GetShape();
#else
		=> GetShape<T, Witness>();
#endif

	[GenerateShape<int>]
	private partial class Witness;

	[GenerateShape]
	public partial record Person
	{
		[Key(0)]
		public required string? FirstName { get; init; }

		[Key(2)] // Deliberately skip index 1 to test array "hole" handling.
		public required string? LastName { get; init; }
	}

	[GenerateShape]
	public partial record PersonWithDefaultConstructor
	{
		[Key(0)]
		public string? FirstName { get; set; }

		[Key(2)] // Deliberately skip index 1 to test array "hole" handling.
		public string? LastName { get; set; }
	}

	/// <summary>
	/// A data type made up of complex and primitive properties such that they'll exercise the async converter code.
	/// </summary>
	/// <remarks>
	/// The key indexes are chosen to include holes to test handling of that.
	/// </remarks>
	[GenerateShape]
	public partial record FamilyWithAsyncProperties
	{
		[Key(0)]
		public required Person? Father { get; init; }

		[Key(1)]
		public required Person? Mother { get; init; }

		[Key(3)]
		public int FamilySize { get; init; }

		[Key(4)]
		public required Person? FirstChild { get; init; }
	}

	/// <summary>
	/// A data type made up of complex and primitive properties such that they'll exercise the async converter code.
	/// </summary>
	/// <remarks>
	/// The key indexes are chosen to include holes to test handling of that.
	/// </remarks>
	[GenerateShape]
	public partial record FamilyWithAsyncPropertiesWithDefaultCtor
	{
		[Key(0)]
		public Person? Father { get; set; }

		[Key(1)]
		public Person? Mother { get; set; }

		[Key(3)]
		public int FamilySize { get; set; }

		[Key(4)]
		public Person? FirstChild { get; set; }
	}

	[GenerateShape]
	public partial record ClassWithUnserializedPropertyGetters
	{
		// This property should never be serialized.
		// And we don't Ignore it properly because it may be inherited where it cannot be attributed, say from ReactiveObject.
		public string PropertyChanged => throw new NotImplementedException();

		[Key(0)]
		public bool Value { get; set; }
	}

	[GenerateShape]
	public partial record ClassWithPropertyGettersWithCtorParamAndMissingKey
	{
		public ClassWithPropertyGettersWithCtorParamAndMissingKey(string propertyChanged) => this.PropertyChanged = propertyChanged;

		public string PropertyChanged { get; }

		[Key(0)]
#pragma warning disable NBMsgPack001 // We WANT this to verify runtime failure.
		public bool Value { get; set; }
#pragma warning restore NBMsgPack001
	}

	[GenerateShape]
	public partial record ClassWithPropertyGettersWithCtorParam
	{
		public ClassWithPropertyGettersWithCtorParam(bool value) => this.Value = value;

		[Key(0)]
		public bool Value { get; set; }
	}
}
