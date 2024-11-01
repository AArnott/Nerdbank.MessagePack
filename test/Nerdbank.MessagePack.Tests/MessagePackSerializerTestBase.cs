// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

	protected T? Roundtrip<T>(T? value)
		where T : IShapeable<T> => this.Roundtrip(value, T.GetShape());

	protected T? Roundtrip<T>(T? value, ITypeShape<T> shape)
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value, shape);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return this.Serializer.Deserialize(sequence, shape);
	}

	protected void LogMsgPack(ReadOnlySequence<byte> msgPack)
	{
		logger.WriteLine(MessagePack.MessagePackSerializer.ConvertToJson(msgPack, MessagePack.MessagePackSerializerOptions.Standard));
	}
}
