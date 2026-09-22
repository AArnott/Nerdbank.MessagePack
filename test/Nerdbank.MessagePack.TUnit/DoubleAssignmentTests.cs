// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DoubleAssignmentTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task MapNoArgState_Collision(bool async)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(3);
		writer.Write("Prop1");
		writer.Write(1);
		writer.Write("Prop2");
		writer.Write(2);
		writer.Write("Prop2");
		writer.Write(3);
		writer.Flush();

		await this.ExpectDeserializationThrowsAsync<MapNoArgState>(seq, async);
	}

	[Test, MatrixDataSource]
	public async Task MapWithArgState_Collision(bool async)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(3);
		writer.Write("Prop1");
		writer.Write(1);
		writer.Write("Prop2");
		writer.Write(2);
		writer.Write("Prop2");
		writer.Write(3);
		writer.Flush();

		await this.ExpectDeserializationThrowsAsync<MapWithArgState>(seq, async);
	}

	[Test]
	public async Task VeryLargeType_Collision()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(3);
		writer.Write("P1");
		writer.Write(false);
		writer.Write("P2");
		writer.Write(false);
		writer.Write("P2");
		writer.Write(true);
		writer.Flush();

		MessagePackSerializationException ex = await this.ExpectDeserializationThrowsAsync<SharedTestTypes.RecordWith66RequiredProperties>(seq, async: false);
		Assert.Contains("P2", ex.Message);
	}

	[Test, MatrixDataSource]
	public async Task ArrayNoArgState_Collision(bool async)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);

		// Ordinal key objects usually serialize as arrays, but they *can* serialize as objects,
		// which is useful to get a smaller payload size by skipping over large gaps in the array.
		writer.WriteMapHeader(3);
		writer.Write(0);
		writer.Write(true);
		writer.Write(1);
		writer.Write(true);
		writer.Write(1);
		writer.Write(false);
		writer.Flush();

		await this.ExpectDeserializationThrowsAsync<ArrayNoArgState>(seq, async);
	}

	[Test, MatrixDataSource]
	public async Task ArrayWithArgState_Collision(bool async)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);

		// Ordinal key objects usually serialize as arrays, but they *can* serialize as objects,
		// which is useful to get a smaller payload size by skipping over large gaps in the array.
		writer.WriteMapHeader(3);
		writer.Write(0);
		writer.Write(true);
		writer.Write(1);
		writer.Write(true);
		writer.Write(1);
		writer.Write(false);
		writer.Flush();

		await this.ExpectDeserializationThrowsAsync<ArrayWithArgState>(seq, async);
	}

	private async ValueTask<MessagePackSerializationException> ExpectDeserializationThrowsAsync<T>(ReadOnlySequence<byte> msgpack, bool async)
#if NET
		where T : IShapeable<T>
#endif
	{
		MessagePackSerializationException ex = async
			? await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.DeserializeAsync<T>(new MemoryStream(msgpack.ToArray()), this.TimeoutToken).AsTask())
			: Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<T>(msgpack, this.TimeoutToken));
		Console.WriteLine(ex.GetBaseException().Message);
		MessagePackSerializationException rootCauseException = Assert.IsType<MessagePackSerializationException>(ex.GetBaseException());
		Assert.Equal(MessagePackSerializationException.ErrorCode.DoublePropertyAssignment, rootCauseException.Code);
		return rootCauseException;
	}

	[GenerateShape]
	public partial class MapNoArgState
	{
		public int Prop1 { get; set; }

		public int Prop2 { get; set; }
	}

	[GenerateShape]
	public partial record MapWithArgState(int Prop1, int Prop2);

	[GenerateShape]
	public partial class ArrayNoArgState
	{
		[Key(0)]
		public bool Prop1 { get; set; }

		[Key(1)]
		public bool Prop2 { get; set; }
	}

	[GenerateShape]
	public partial record ArrayWithArgState([property: Key(0)] bool Prop1, [property: Key(1)] bool Prop2);
}
