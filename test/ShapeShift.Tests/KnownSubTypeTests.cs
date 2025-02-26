// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ShapeShift.MessagePack;

public partial class KnownSubTypeTests : MessagePackSerializerTestBase
{
	[Theory, PairwiseData]
	public async Task BaseType(bool async)
	{
		BaseClass value = new() { BaseClassProperty = 5 };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);

		// Assert that it's serialized in its special syntax that allows for derived types.
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		reader.ReadNull();
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
	}

	[Fact]
	public void DerivedAType()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 });

		// Assert that this has no special header because it has no Union attribute of its own.
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartMap());
		Assert.Equal(nameof(DerivedA.DerivedAProperty), reader.ReadString());
		Assert.Equal(6, reader.ReadInt32());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
		Assert.Equal(5, reader.ReadInt32());
		Assert.True(reader.End);
	}

	[Theory, PairwiseData]
	public async Task DerivedA_AsBaseType(bool async)
	{
		var value = new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 };
		if (async)
		{
			await this.AssertRoundtripAsync<BaseClass>(value);
		}
		else
		{
			this.AssertRoundtrip<BaseClass>(value);
		}
	}

	[Fact]
	public void DerivedAA_AsBaseType() => this.AssertRoundtrip<BaseClass>(new DerivedAA { BaseClassProperty = 5, DerivedAProperty = 6 });

	[Fact]
	public void DerivedB_AsBaseType() => this.AssertRoundtrip<BaseClass>(new DerivedB(10) { BaseClassProperty = 5 });

	[Fact]
	public void EnumerableDerived_BaseType()
	{
		// This is a lossy operation. Only the collection elements are serialized,
		// and the class cannot be deserialized because the constructor doesn't take a collection.
		EnumerableDerived value = new(3) { BaseClassProperty = 5 };
		byte[] msgpack = this.Serializer.Serialize<BaseClass>(value, TestContext.Current.CancellationToken);
		this.Logger.WriteLine(new JsonExporter(this.Serializer).ConvertToJson(msgpack));
	}

	[Fact]
	public void ClosedGenericDerived_BaseType() => this.AssertRoundtrip<BaseClass>(new DerivedGeneric<int>(5) { BaseClassProperty = 10 });

	[Theory, PairwiseData]
	public async Task Null(bool async)
	{
		if (async)
		{
			await this.AssertRoundtripAsync<BaseClass>(null);
		}
		else
		{
			this.AssertRoundtrip<BaseClass>(null);
		}
	}

	[Fact]
	public void UnrecognizedAlias()
	{
		Sequence<byte> sequence = new();
		Writer writer = new(sequence, MessagePackFormatter.Default);
		writer.WriteStartVector(2);
		writer.Write(100);
		writer.WriteStartMap(0);
		writer.Flush();

		SerializationException ex = Assert.Throws<SerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void UnrecognizedArraySize()
	{
		Sequence<byte> sequence = new();
		Writer writer = new(sequence, MessagePackFormatter.Default);
		writer.WriteStartVector(3);
		writer.Write(100);
		writer.WriteNull();
		writer.WriteNull();
		writer.Flush();

		SerializationException ex = Assert.Throws<SerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void UnknownDerivedType()
	{
		SerializationException ex = Assert.Throws<SerializationException>(() => this.Roundtrip<BaseClass>(new UnknownDerived()));
		this.Logger.WriteLine(ex.Message);
	}

	[Theory, PairwiseData]
	public async Task MixedAliasTypes(bool async)
	{
		MixedAliasBase value = new MixedAliasDerivedA();
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		Assert.Equal("A", reader.ReadString());

		value = new MixedAliasDerived1();
		msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);
		reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		Assert.Equal(1, reader.ReadInt32());
	}

	[Fact]
	public void ImpliedAlias()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<ImpliedAliasBase>(new ImpliedAliasDerived());
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		Assert.Equal(typeof(ImpliedAliasDerived).FullName, reader.ReadString());
	}

	[Fact]
	public void RecursiveSubTypes()
	{
		SerializationException ex = Assert.Throws<SerializationException>(
			() => this.Serializer.Serialize<RecursiveBase>(new RecursiveDerivedDerived(), TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.Message);

#if false
		// If it were to work, this is how we expect it to work:
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<RecursiveBase>(new RecursiveDerivedDerived());
		Reader reader = new(msgpack, MsgPackDeformatter.Default);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(1, reader.ReadInt32());
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(13, reader.ReadInt32());
#endif
	}

	[Fact]
	public void RuntimeRegistration()
	{
		KnownSubTypeMapping<DynamicallyRegisteredBase> mapping = new();
#if NET
		mapping.Add<DynamicallyRegisteredDerivedA>(1);
		mapping.Add<DynamicallyRegisteredDerivedB>(2);
#else
		mapping.Add<DynamicallyRegisteredDerivedA>(1, Witness.ShapeProvider);
		mapping.Add<DynamicallyRegisteredDerivedB>(2, Witness.ShapeProvider);
#endif
		this.Serializer.RegisterKnownSubTypes(mapping);

		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredBase());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedA());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedB());
	}

	[Fact]
	public void RuntimeRegistration_OverridesStatic()
	{
		KnownSubTypeMapping<BaseClass> mapping = new();
		mapping.Add<DerivedB>(1, Witness.ShapeProvider);
		this.Serializer.RegisterKnownSubTypes(mapping);

		// Verify that the base type has just one header.
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<BaseClass>(new BaseClass { BaseClassProperty = 5 });
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		reader.ReadNull();
		Assert.Equal(1, reader.ReadStartMap());

		// Verify that the header type value is the runtime-specified 1 instead of the static 3.
		msgpack = this.AssertRoundtrip<BaseClass>(new DerivedB(13));
		reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		Assert.Equal(1, reader.ReadInt32());

		// Verify that statically set subtypes are not recognized if no runtime equivalents are registered.
		SerializationException ex = Assert.Throws<SerializationException>(() => this.Roundtrip<BaseClass>(new DerivedA()));
		this.Logger.WriteLine(ex.Message);
	}

	/// <summary>
	/// Verify that an empty mapping is allowed and produces the schema that allows for sub-types to be added in the future.
	/// </summary>
	[Fact]
	public void RuntimeRegistration_EmptyMapping()
	{
		KnownSubTypeMapping<DynamicallyRegisteredBase> mapping = new();
		this.Serializer.RegisterKnownSubTypes(mapping);
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new DynamicallyRegisteredBase());
		Reader reader = new(msgpack, MessagePackDeformatter.Default);
		Assert.Equal(2, reader.ReadStartVector());
		reader.ReadNull();
		Assert.Equal(0, reader.ReadStartMap());
	}

	[GenerateShape<DerivedGeneric<int>>]
	internal partial class Witness;

	[GenerateShape]
#if NET
	[KnownSubType<DerivedA>(1)]
	[KnownSubType<DerivedAA>(2)]
	[KnownSubType<DerivedB>(3)]
	[KnownSubType<EnumerableDerived>(4)]
	[KnownSubType<DerivedGeneric<int>, Witness>(5)]
#else
	[KnownSubType(typeof(DerivedA), 1)]
	[KnownSubType(typeof(DerivedAA), 2)]
	[KnownSubType(typeof(DerivedB), 3)]
	[KnownSubType(typeof(EnumerableDerived), 4)]
	[KnownSubType(typeof(DerivedGeneric<int>), 5)]
#endif
	public partial record BaseClass
	{
		public int BaseClassProperty { get; set; }
	}

	[GenerateShape]
	public partial record DerivedA() : BaseClass
	{
		public int DerivedAProperty { get; set; }
	}

	[GenerateShape]
	public partial record DerivedAA : DerivedA
	{
	}

	[GenerateShape]
	public partial record DerivedB(int DerivedBProperty) : BaseClass
	{
	}

	[GenerateShape]
	public partial record EnumerableDerived(int Count) : BaseClass, IEnumerable<int>
	{
		public IEnumerator<int> GetEnumerator() => Enumerable.Range(0, this.Count).GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	public partial record DerivedGeneric<T>(T Value) : BaseClass
	{
	}

	[GenerateShape]
	public partial record UnknownDerived : BaseClass;

	[GenerateShape]
#if NET
	[KnownSubType<MixedAliasDerivedA>("A")]
	[KnownSubType<MixedAliasDerived1>(1)]
#else
	[KnownSubType(typeof(MixedAliasDerivedA), "A")]
	[KnownSubType(typeof(MixedAliasDerived1), 1)]
#endif
	public partial record MixedAliasBase;

	[GenerateShape]
	public partial record MixedAliasDerivedA : MixedAliasBase;

	[GenerateShape]
	public partial record MixedAliasDerived1 : MixedAliasBase;

	[GenerateShape]
#if NET
	[KnownSubType<ImpliedAliasDerived>]
#else
	[KnownSubType(typeof(ImpliedAliasDerived))]
#endif
	public partial record ImpliedAliasBase;

	[GenerateShape]
	public partial record ImpliedAliasDerived : ImpliedAliasBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredDerivedA : DynamicallyRegisteredBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredDerivedB : DynamicallyRegisteredBase;

	[GenerateShape]
#if NET
	[KnownSubType<RecursiveDerived>(1)]
#else
	[KnownSubType(typeof(RecursiveDerived), 1)]
#endif
	public partial record RecursiveBase;

	[GenerateShape]
#if NET
	[KnownSubType<RecursiveDerivedDerived>(13)]
#else
	[KnownSubType(typeof(RecursiveDerivedDerived), 13)]
#endif
	public partial record RecursiveDerived : RecursiveBase;

	[GenerateShape]
	public partial record RecursiveDerivedDerived : RecursiveDerived;
}
