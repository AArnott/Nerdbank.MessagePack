// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Converters;

[Trait("dynamic", "true")]
public partial class ExpandoObjectConverterTests : MessagePackSerializerTestBase
{
	public ExpandoObjectConverterTests()
	{
		this.Serializer = this.Serializer.WithExpandoObjectConverter();
	}

	[Fact]
	public void SimplePropertyValues()
	{
		dynamic e = new ExpandoObject();
		e.a = 5;
		e.b = "hi";
		e.Nothing = null;

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal(5, (int)e2!.a);
		Assert.Equal("hi", (string?)e2.b);
		Assert.Null(e2!.Nothing);
	}

	[Fact]
	public void Depth()
	{
		dynamic e = new ExpandoObject();
		e.a = new byte[] { 1, 2 };
		e.b = new ExpandoObject();
		e.b.c = "bah";

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal<byte>([1, 2], (byte[])e2.a);
		Assert.Equal("bah", e2.b.c);
	}

	[Fact]
	public void MaxPropertyCountHonoredOnSerialization()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with
			{
				Security = this.Serializer.StartingContext.Security with
				{
					ExpandoObjectMaxPropertyCount = 2,
				},
			},
		};

		dynamic e = new ExpandoObject();
		e.a = 1;
		e.b = 2;
		e.c = 3;

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize<ExpandoObject, Witness>((ExpandoObject)e, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.GetBaseException().Message);
		Assert.Contains($"3 properties", ex.GetBaseException().Message);
	}

	[Fact]
	public void MaxPropertyCountHonoredOnDeserialization()
	{
		const int MaxPropertyCount = 2;
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with
			{
				Security = this.Serializer.StartingContext.Security with
				{
					ExpandoObjectMaxPropertyCount = MaxPropertyCount,
				},
			},
		};

		dynamic? e = this.Serializer.Deserialize<ExpandoObject, Witness>(ConstructMap(MaxPropertyCount), TestContext.Current.CancellationToken);
		Assert.Equal(1, (int)e?.A);

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Deserialize<ExpandoObject, Witness>(ConstructMap(MaxPropertyCount + 1), TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.GetBaseException().Message);
		Assert.Contains($"{MaxPropertyCount + 1} properties", ex.GetBaseException().Message);

		ReadOnlySequence<byte> ConstructMap(int count)
		{
			Sequence<byte> msgpack = new();
			MessagePackWriter writer = new(msgpack);
			writer.WriteMapHeader(count);
			for (int i = 0; i < count; i++)
			{
				writer.Write($"{(char)('A' + i)}");
				writer.Write(i + 1);
			}

			writer.Flush();
			this.LogMsgPack(msgpack);
			return msgpack;
		}
	}

	[Fact]
	public void Null()
	{
		Assert.Null(this.Roundtrip<ExpandoObject, Witness>(null));
	}

	[GenerateShapeFor<int>]
	[GenerateShapeFor<ExpandoObject>]
	private partial class Witness;
}
