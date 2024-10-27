// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
using Nerdbank.Streams;
using TypeShape;
using Xunit;
using Xunit.Abstractions;

public partial class MessagePackSerializerTests(ITestOutputHelper logger)
{
	private readonly MessagePackSerializer serializer = new();

	[Fact]
	public void SimpleNull() => this.AssertRoundtrip<Fruit>(null);

	[Fact]
	public void SimplePoco() => this.AssertRoundtrip(new Fruit { Seeds = 18 });

	[Fact]
	public void AllIntTypes() => this.AssertRoundtrip(new IntRichPoco { Int8 = -1, Int16 = -2, Int32 = -3, Int64 = -4, UInt8 = 1, UInt16 = 2, UInt32 = 3, UInt64 = 4 });

	[Fact]
	public void SimpleRecordClass() => this.AssertRoundtrip(new RecordClass(42) { Weight = 5, ChildNumber = 2 });

	protected void AssertRoundtrip<T>(T? value)
		where T : IShapeable<T>
	{
		T? roundtripped = this.Roundtrip(value);
		Assert.Equal(value, roundtripped);
	}

	protected T? Roundtrip<T>(T? value)
		where T : IShapeable<T>
	{
		Sequence<byte> sequence = new();
		this.serializer.Serialize(sequence, value);
		logger.WriteLine(MessagePack.MessagePackSerializer.ConvertToJson(sequence, MessagePack.MessagePackSerializerOptions.Standard));
		return this.serializer.Deserialize<T>(sequence);
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
	public partial record RecordClass(int Seeds)
	{
		public int Weight { get; set; }

		public int ChildNumber { get; init; }
	}
}
