// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public abstract class MessagePackSerializerTestBase(ITestOutputHelper logger)
{
	protected ReadOnlySequence<byte> lastRoundtrippedMsgpack;

	/// <summary>
	/// Gets the time for a delay that is likely (but not guaranteed) to let concurrent work make progress in a way that is conducive to the test's intent.
	/// </summary>
	public static TimeSpan AsyncDelay => TimeSpan.FromMilliseconds(250);

	protected MessagePackSerializer Serializer { get; set; } = new();

	protected ITestOutputHelper Logger => logger;

#if !NET
	internal static ITypeShapeProvider GetShapeProvider<TProvider>()
	{
		PropertyInfo shapeProperty = typeof(TProvider).GetProperty("ShapeProvider", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException($"{typeof(TProvider).FullName} is not a witness class.");
		Assert.NotNull(shapeProperty);
		return (ITypeShapeProvider)shapeProperty.GetValue(null)!;
	}
#endif

	internal static ITypeShape<T> GetShape<T, TProvider>()
#if NET
		where TProvider : IShapeable<T>
#endif
	{
#if NET
		return TProvider.GetShape();
#else
		return (ITypeShape<T>?)GetShapeProvider<TProvider>().GetShape(typeof(T)) ?? throw new InvalidOperationException("Shape not found.");
#endif
	}

	internal static ValueTask<ReadResult> FetchOneByteAtATimeAsync(object? state, SequencePosition consumed, SequencePosition examined, CancellationToken cancellationToken)
	{
		ReadOnlySequence<byte> wholeBuffer = (ReadOnlySequence<byte>)state!;

		// Always provide just one more byte.
		ReadOnlySequence<byte> slice = wholeBuffer.Slice(consumed, wholeBuffer.GetPosition(1, examined));
		return new(new ReadResult(slice, isCanceled: false, isCompleted: slice.End.Equals(wholeBuffer.End)));
	}

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
#if NET
		where T : IShapeable<T>
		=> this.AssertRoundtrip<T, T>(value);
#else
		=> this.AssertRoundtrip<T, MessagePackSerializerPolyfill.Witness>(value);
#endif

	protected ReadOnlySequence<byte> AssertRoundtrip<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
#endif
	{
#if NET
		T? roundtripped = this.Roundtrip(value, TProvider.GetShape());
#else
		T? roundtripped = this.Roundtrip(value, GetShape<T, TProvider>());
#endif
		Assert.Equal(value, roundtripped);
		return this.lastRoundtrippedMsgpack;
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T>(T? value)
#if NET
		where T : IShapeable<T>
#endif
	{
#if NET
		await this.AssertRoundtripAsync<T, T>(value);
#else
		await this.AssertRoundtripAsync<T, MessagePackSerializerPolyfill.Witness>(value);
#endif
		return this.lastRoundtrippedMsgpack;
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
#endif
	{
		T? roundtripped = await this.RoundtripAsync<T, TProvider>(value);
		Assert.Equal(value, roundtripped);
		return this.lastRoundtrippedMsgpack;
	}

	protected T? Roundtrip<T>(T? value)
#if NET
		where T : IShapeable<T> => this.Roundtrip(value, T.GetShape());
#else
		=> this.Roundtrip(value, GetShape<T, MessagePackSerializerPolyfill.Witness>());
#endif

	protected T? Roundtrip<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.Roundtrip(value, TProvider.GetShape());
#else
		=> this.Roundtrip(value, GetShape<T, TProvider>());
#endif

	protected T? Roundtrip<T>(T? value, ITypeShape<T> shape)
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value, shape);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return this.Serializer.Deserialize(sequence, shape);
	}

	protected ValueTask<T?> RoundtripAsync<T>(T? value)
#if NET
		where T : IShapeable<T>
		=> this.RoundtripAsync(value, T.GetShape());
#else
		=> this.RoundtripAsync(value, GetShape<T, MessagePackSerializerPolyfill.Witness>());
#endif

	protected ValueTask<T?> RoundtripAsync<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.RoundtripAsync(value, TProvider.GetShape());
#else
		=> this.RoundtripAsync(value, GetShape<T, TProvider>());
#endif

	protected async ValueTask<T?> RoundtripAsync<T>(T? value, ITypeShape<T> shape)
	{
		Pipe pipeForSerializing = new();
		Pipe pipeForDeserializing = new();

		// Arrange the reader first to avoid deadlocks if the Pipe gets full.
		ValueTask<T?> resultTask = this.Serializer.DeserializeAsync(pipeForDeserializing.Reader, shape);

		// Log along the way.
		Sequence<byte> loggingSequence = new();
		CapturePipe(pipeForSerializing.Reader, pipeForDeserializing.Writer, loggingSequence);

		await this.Serializer.SerializeAsync(pipeForSerializing.Writer, value, shape);
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
