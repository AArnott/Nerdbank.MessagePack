// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class MessagePackSerializerTestBase : SerializerTestBase<MessagePackSerializer>
{
	public MessagePackSerializerTestBase(ITestOutputHelper logger)
		: base(new MessagePackSerializer
		{
			// Most async tests primarily mean to exercise the async code paths,
			// so disable the buffer that would lead it down the synchronous paths since we have
			// small test data sizes.
			MaxAsyncBuffer = 0,
		})
	{
	}

	protected override void LogFormattedBytes(ReadOnlySequence<byte> formattedBytes)
	{
		this.Logger.WriteLine(new JsonExporter(this.Serializer).ConvertToJson(formattedBytes));
	}
}
