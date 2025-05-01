// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DoubleAssignmentTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Theory, PairwiseData]
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

		await this.DeserializeMaybeAsync<MapNoArgState>(seq, async);
	}

	[Theory, PairwiseData]
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

		await this.DeserializeMaybeAsync<MapWithArgState>(seq, async);
	}

	[Theory, PairwiseData]
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

		await this.DeserializeMaybeAsync<ArrayNoArgState>(seq, async);
	}

	[Theory, PairwiseData]
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

		await this.DeserializeMaybeAsync<ArrayWithArgState>(seq, async);
	}

	private async ValueTask<MessagePackSerializationException> DeserializeMaybeAsync<T>(ReadOnlySequence<byte> msgpack, bool async)
#if NET
		where T : IShapeable<T>
#endif
	{
		MessagePackSerializationException ex = async
			? await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.DeserializeAsync<T>(new MemoryStream(msgpack.ToArray()), TestContext.Current.CancellationToken).AsTask())
			: Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<T>(msgpack, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.GetBaseException().Message);
		return ex;
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
