// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class MessagePackSerializerTestBase(ITestOutputHelper logger)
{
	protected MessagePackSerializer Serializer { get; set; } = new();

	protected ITestOutputHelper Logger => logger;

	protected void AssertRoundtrip<T>(T? value)
		where T : IShapeable<T> => this.AssertRoundtrip<T, T>(value);

	protected void AssertRoundtrip<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		T? roundtripped = this.Roundtrip<T, TProvider>(value);
		Assert.Equal(value, roundtripped);
	}

	protected T? Roundtrip<T>(T? value)
		where T : IShapeable<T> => this.Roundtrip<T, T>(value);

	protected T? Roundtrip<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize<T, TProvider>(sequence, value);
		logger.WriteLine(MessagePack.MessagePackSerializer.ConvertToJson(sequence, MessagePack.MessagePackSerializerOptions.Standard));
		return this.Serializer.Deserialize<T, TProvider>(sequence);
	}
}
