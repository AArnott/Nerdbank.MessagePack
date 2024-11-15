// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

public abstract class MessagePackSerializerTestBase(ITestOutputHelper logger)
{
	private ReadOnlySequence<byte> lastRoundtrippedMsgpack;

	protected MessagePackSerializer Serializer { get; set; } = new();

	protected ITestOutputHelper Logger => logger;

	protected ReadOnlySequence<byte> AssertRoundtrip<T>(T? value)
		where T : IShapeable<T> => this.AssertRoundtrip<T, T>(value);

	protected ReadOnlySequence<byte> AssertRoundtrip<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		T? roundtripped = this.Roundtrip(value, TProvider.GetShape());
		Assert.Equal(value, roundtripped);
		return this.lastRoundtrippedMsgpack;
	}

	protected Task AssertRoundtripAsync<T>(T? value)
		where T : IShapeable<T> => this.AssertRoundtripAsync<T, T>(value);

	protected async Task AssertRoundtripAsync<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		T? roundtripped = await this.RoundtripAsync<T, TProvider>(value);
		Assert.Equal(value, roundtripped);
	}

	protected T? Roundtrip<T>(T? value)
		where T : IShapeable<T> => this.Roundtrip(value, T.GetShape());

	protected T? Roundtrip<T, TProvider>(T? value)
		where TProvider : IShapeable<T> => this.Roundtrip(value, TProvider.GetShape());

	protected T? Roundtrip<T>(T? value, ITypeShape<T> shape)
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value, shape);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return this.Serializer.Deserialize(sequence, shape);
	}

	protected ValueTask<T?> RoundtripAsync<T>(T? value)
		where T : IShapeable<T> => this.RoundtripAsync<T, T>(value);

	protected async ValueTask<T?> RoundtripAsync<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		Pipe pipe = new();

		// Arrange the reader first to avoid deadlocks if the Pipe gets full.
		ValueTask<T?> result = this.Serializer.DeserializeAsync<T, TProvider>(pipe.Reader);

		await this.Serializer.SerializeAsync<T, TProvider>(pipe.Writer, value);
		await pipe.Writer.FlushAsync();

		// The deserializer should complete even *without* our completing the writer.
		// But if tests hang, enabling this can help turn them into EndOfStreamException.
		////await pipe.Writer.CompleteAsync();

		return await result;
	}

	protected void LogMsgPack(ReadOnlySequence<byte> msgPack)
	{
		logger.WriteLine(MessagePackSerializer.ConvertToJson(msgPack));
	}
}
