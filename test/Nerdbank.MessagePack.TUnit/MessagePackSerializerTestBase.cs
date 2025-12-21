// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

public abstract partial class MessagePackSerializerTestBase
{
	protected ReadOnlySequence<byte> lastRoundtrippedMsgpack;

	public MessagePackSerializerTestBase()
	{
		this.Serializer = new MessagePackSerializer
		{
			// Most async tests primarily mean to exercise the async code paths,
			// so disable the buffer that would lead it down the synchronous paths since we have
			// small test data sizes.
			MaxAsyncBuffer = 0,

			// Also pause async Serialization to flush frequently to exercise those code paths.
			StartingContext = new SerializationContext { UnflushedBytesThreshold = 50 },
		};
	}

	/// <summary>
	/// Gets the time for a delay that is likely (but not guaranteed) to let concurrent work make progress in a way that is conducive to the test's intent.
	/// </summary>
	public static TimeSpan AsyncDelay => TimeSpan.FromMilliseconds(250);

	protected MessagePackSerializer Serializer { get; set; }

	protected CancellationToken TimeoutToken => TestContext.Current?.Execution.CancellationToken ?? default;

	protected DefaultLogger Logger => TestContext.Current?.GetDefaultLogger() ?? throw new InvalidOperationException();

	public static string GetFullMessage(Exception ex)
	{
		StringBuilder builder = new();
		Exception? current = ex;
		while (current is not null)
		{
			if (builder.Length > 0)
			{
				builder.Append(' ');
			}

			builder.Append(current.Message);
			current = current.InnerException;
		}

		return builder.ToString();
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

	protected static string SchemaToString(JsonObject schema)
	{
		string schemaString = schema
			.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
			.Replace($"Nerdbank.MessagePack.Tests, Version={ThisAssembly.AssemblyVersion}", "Nerdbank.MessagePack.Tests, Version=x.x.x.x");

#if NETFRAMEWORK
		// Normalize from .NET Framework specific strings to .NET strings.
		schemaString = schemaString.Replace("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
#endif

		// Normalize across .NET versions.
		schemaString = Regex.Replace(schemaString, @"System\.Private\.CoreLib, Version=\d+\.0\.0\.0", "System.Private.CoreLib, Version=x.0.0.0");

		return schemaString;
	}

	protected Task<ReadOnlySequence<byte>> AssertRoundtrip<T>(T? value)
#if NET
		where T : IShapeable<T>
		=> this.AssertRoundtrip<T, T>(value);
#else
		=> this.AssertRoundtrip<T, T>(value);
#endif

	protected async Task<ReadOnlySequence<byte>> AssertRoundtrip<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
#endif
	{
		T? roundtripped = this.Roundtrip<T, TProvider>(value);
		if (value is IStructuralSecureEqualityComparer<T> deepComparer)
		{
			await Assert.That(deepComparer.StructuralEquals(roundtripped)).IsTrue();
		}
		else
		{
			await Assert.That(roundtripped).IsEqualTo(value);
		}

		return this.lastRoundtrippedMsgpack;
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T>(T? value)
#if NET
		where T : IShapeable<T>
#endif
	{
		return await this.AssertRoundtripAsync<T, T>(value);
	}

	protected async Task<ReadOnlySequence<byte>> AssertRoundtripAsync<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
#endif
	{
		T? roundtripped = await this.RoundtripAsync<T, TProvider>(value);
		await Assert.That(roundtripped).IsEqualTo(value);
		return this.lastRoundtrippedMsgpack;
	}

	protected T? Roundtrip<T>(T? value)
#if NET
		where T : IShapeable<T> => this.Roundtrip(value, T.GetTypeShape());
#else
		=> this.Roundtrip(value, TypeShapeResolver.ResolveDynamicOrThrow<T>());
#endif

	protected T? Roundtrip<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.Roundtrip(value, TProvider.GetTypeShape());
#else
		=> this.Roundtrip(value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());
#endif

	protected T? Roundtrip<T>(T? value, ITypeShape<T> shape)
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value, shape, this.TimeoutToken);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return this.Serializer.Deserialize(sequence, shape, this.TimeoutToken);
	}

	protected ValueTask<T?> RoundtripAsync<T>(T? value)
#if NET
		where T : IShapeable<T>
		=> this.RoundtripAsync(value, T.GetTypeShape());
#else
		=> this.RoundtripAsync(value, TypeShapeResolver.ResolveDynamicOrThrow<T>());
#endif

	protected ValueTask<T?> RoundtripAsync<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.RoundtripAsync(value, TProvider.GetTypeShape());
#else
		=> this.RoundtripAsync(value, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());
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

		await pipeForSerializing.Writer.CompleteAsync();

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

	protected void LogMsgPack(ReadOnlyMemory<byte> msgPack) => this.LogMsgPack(new ReadOnlySequence<byte>(msgPack));

	protected void LogMsgPack(ReadOnlySequence<byte> msgPack)
	{
		this.Logger.LogTrace(this.Serializer.ConvertToJson(msgPack));
	}

	[GenerateShapeFor<bool>]
	private partial class Witness;
}
