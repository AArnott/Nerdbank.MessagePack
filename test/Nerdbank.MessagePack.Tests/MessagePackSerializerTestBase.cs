// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract class MessagePackSerializerTestBase(ITestOutputHelper logger)
{
	private ReadOnlySequence<byte> lastRoundtrippedMsgpack;

	/// <summary>
	/// Gets the time for a delay that is likely (but not guaranteed) to let concurrent work make progress in a way that is conducive to the test's intent.
	/// </summary>
	public static TimeSpan AsyncDelay => TimeSpan.FromMilliseconds(250);

	protected MessagePackSerializer Serializer { get; set; } = new();

	protected ITestOutputHelper Logger => logger;

	protected static void CapturePipe(PipeReader reader, PipeWriter forwardTo, Sequence<byte> logger)
	{
		_ = Task.Run(async delegate
		{
			while (true)
			{
				try
				{
					ReadResult read = await reader.ReadAsync();
					if (!read.Buffer.IsEmpty)
					{
						foreach (ReadOnlyMemory<byte> segment in read.Buffer)
						{
							logger.Write(segment.Span);
							forwardTo.Write(segment.Span);
						}

						await forwardTo.FlushAsync();
					}

					reader.AdvanceTo(read.Buffer.End);
					if (read.IsCompleted)
					{
						await forwardTo.CompleteAsync();
						return;
					}
				}
				catch (Exception ex)
				{
					await forwardTo.CompleteAsync(ex);
				}
			}
		});
	}

	protected ReadOnlySequence<byte> AssertRoundtrip<T>(T? value)
		where T : IShapeable<T> => this.AssertRoundtrip<T, T>(value);

	protected ReadOnlySequence<byte> AssertRoundtrip<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		T? roundtripped = this.Roundtrip(value, TProvider.GetShape());
		Assert.Equal(value, roundtripped);
		return this.lastRoundtrippedMsgpack;
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T>(T? value)
		where T : IShapeable<T>
	{
		await this.AssertRoundtripAsync<T, T>(value);
		return this.lastRoundtrippedMsgpack;
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T, TProvider>(T? value)
		where TProvider : IShapeable<T>
	{
		T? roundtripped = await this.RoundtripAsync<T, TProvider>(value);
		Assert.Equal(value, roundtripped);
		return this.lastRoundtrippedMsgpack;
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
		Pipe pipeForSerializing = new();
		Pipe pipeForDeserializing = new();

		// Arrange the reader first to avoid deadlocks if the Pipe gets full.
		ValueTask<T?> resultTask = this.Serializer.DeserializeAsync<T, TProvider>(pipeForDeserializing.Reader);

		// Log along the way.
		Sequence<byte> loggingSequence = new();
		CapturePipe(pipeForSerializing.Reader, pipeForDeserializing.Writer, loggingSequence);

		await this.Serializer.SerializeAsync<T, TProvider>(pipeForSerializing.Writer, value);
		await pipeForSerializing.Writer.FlushAsync();

		// The deserializer should complete even *without* our completing the writer.
		// But if tests hang, enabling this can help turn them into EndOfStreamException.
		////await pipe.Writer.CompleteAsync();

		try
		{
			T? result = await resultTask;
			return result;
		}
		finally
		{
			this.lastRoundtrippedMsgpack = loggingSequence;
			this.LogMsgPack(loggingSequence);
		}
	}

	protected void LogMsgPack(ReadOnlySequence<byte> msgPack)
	{
		logger.WriteLine(MessagePackSerializer.ConvertToJson(msgPack));
	}
}
