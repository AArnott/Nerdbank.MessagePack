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
	public void SimpleNull()
	{
		this.AssertRoundtrip<Fruit>(null);
	}

	[Fact]
	public void SimplePoco()
	{
		this.AssertRoundtrip(new Fruit { Seeds = 18 });
	}

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
}
