// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class KnownSubTypeTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void BaseType()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new BaseClass { BaseClassProperty = 5 });

		// Assert that it's serialized in its special syntax that allows for derived types.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		reader.ReadNil();
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
	}

	[Fact]
	public void DerivedAType()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 });

		// Assert that this has no special header because it has no Union attribute of its own.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadMapHeader());
		Assert.Equal(nameof(DerivedA.DerivedAProperty), reader.ReadString());
		Assert.Equal(6, reader.ReadInt32());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
		Assert.Equal(5, reader.ReadInt32());
		Assert.True(reader.End);
	}

	[Fact]
	public void DerivedA_AsBaseType() => this.AssertRoundtrip<BaseClass>(new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 });

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
		byte[] msgpack = this.Serializer.Serialize<BaseClass>(value);
		this.Logger.WriteLine(MessagePackSerializer.ConvertToJson(msgpack));
	}

	[Fact]
	public void ClosedGenericDerived_BaseType() => this.AssertRoundtrip<BaseClass>(new DerivedGeneric<int>(5) { BaseClassProperty = 10 });

	[Fact]
	public void Null() => this.AssertRoundtrip<BaseClass>(null);

	[Fact]
	public void UnrecognizedAlias()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(2);
		writer.Write(100);
		writer.WriteMapHeader(0);
		writer.Flush();

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void UnrecognizedArraySize()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(3);
		writer.Write(100);
		writer.WriteNil();
		writer.WriteNil();
		writer.Flush();

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void UnknownDerivedType()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip<BaseClass>(new UnknownDerived()));
		this.Logger.WriteLine(ex.Message);
	}

	[GenerateShape<DerivedGeneric<int>>]
	internal partial class Witness;

	[GenerateShape]
	[KnownSubType<DerivedA>(1)]
	[KnownSubType<DerivedAA>(2)]
	[KnownSubType<DerivedB>(3)]
	[KnownSubType<EnumerableDerived>(4)]
	[KnownSubType<DerivedGeneric<int>, Witness>(5)]
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
}
