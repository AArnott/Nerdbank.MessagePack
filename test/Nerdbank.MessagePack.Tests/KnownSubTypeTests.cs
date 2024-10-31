// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class UnionTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
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

	[GenerateShape]
	[KnownSubType(1, typeof(DerivedA))]
	[KnownSubType(2, typeof(DerivedAA))]
	[KnownSubType(3, typeof(DerivedB))]
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
}
