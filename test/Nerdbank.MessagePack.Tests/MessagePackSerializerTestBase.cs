// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

public abstract class MessagePackSerializerTestBase
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
		};
	}

	/// <summary>
	/// Gets the time for a delay that is likely (but not guaranteed) to let concurrent work make progress in a way that is conducive to the test's intent.
	/// </summary>
	public static TimeSpan AsyncDelay => TimeSpan.FromMilliseconds(250);

	protected MessagePackSerializer Serializer { get; set; }

	protected ITestOutputHelper Logger => TestContext.Current.TestOutputHelper ?? throw new InvalidOperationException("No logger available.");

#if !NET
	internal static ITypeShapeProvider GetShapeProvider<TProvider>()
	{
		PropertyInfo shapeProperty = typeof(TProvider).GetProperty("GeneratedTypeShapeProvider", BindingFlags.Public | BindingFlags.Static) ?? throw new InvalidOperationException($"{typeof(TProvider).FullName} is not a witness class.");
		Assert.NotNull(shapeProperty);
		return (ITypeShapeProvider)shapeProperty.GetValue(null)!;
	}
#endif

	internal static ITypeShape<T> GetTypeShape<T, TProvider>()
#if NET
		where TProvider : IShapeable<T>
#endif
	{
#if NET
		return TProvider.GetTypeShape();
#else
		return (ITypeShape<T>)GetShapeProvider<TProvider>().GetTypeShapeOrThrow(typeof(T));
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
		T? roundtripped = this.Roundtrip(value, TProvider.GetTypeShape());
#else
		T? roundtripped = this.Roundtrip(value, GetTypeShape<T, TProvider>());
#endif
		if (value is IStructuralSecureEqualityComparer<T> deepComparer)
		{
			Assert.True(deepComparer.StructuralEquals(roundtripped), "Roundtripped value does not match the original value by deep equality.");
		}
		else
		{
			Assert.Equal(value, roundtripped);
		}

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
		where T : IShapeable<T> => this.Roundtrip(value, T.GetTypeShape());
#else
		=> this.Roundtrip(value, GetTypeShape<T, MessagePackSerializerPolyfill.Witness>());
#endif

	protected T? Roundtrip<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.Roundtrip(value, TProvider.GetTypeShape());
#else
		=> this.Roundtrip(value, GetTypeShape<T, TProvider>());
#endif

	protected T? Roundtrip<T>(T? value, ITypeShape<T> shape)
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value, shape, TestContext.Current.CancellationToken);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return this.Serializer.Deserialize(sequence, shape, TestContext.Current.CancellationToken);
	}

	protected ValueTask<T?> RoundtripAsync<T>(T? value)
#if NET
		where T : IShapeable<T>
		=> this.RoundtripAsync(value, T.GetTypeShape());
#else
		=> this.RoundtripAsync(value, GetTypeShape<T, MessagePackSerializerPolyfill.Witness>());
#endif

	protected ValueTask<T?> RoundtripAsync<T, TProvider>(T? value)
#if NET
		where TProvider : IShapeable<T>
		=> this.RoundtripAsync(value, TProvider.GetTypeShape());
#else
		=> this.RoundtripAsync(value, GetTypeShape<T, TProvider>());
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

	protected bool DataMatchesSchema(ReadOnlySequence<byte> msgpack, ITypeShape shape)
	{
		JsonObject schema = this.Serializer.GetJsonSchema(shape);
		string schemaString = SchemaToString(schema);
		JSchema parsedSchema = JSchema.Parse(schemaString);

		// We ignore known extensions while writing to JSON because that's what will lead the emitted JSON
		// to match the JSON schema, which really describes the msgpack schema.
		string json = this.Serializer.ConvertToJson(msgpack, new() { IgnoreKnownExtensions = true });

		var parsed = JsonNode.Parse(json);
		try
		{
			JToken.Parse(json).Validate(parsedSchema);
			return true;
		}
		catch (Exception ex)
		{
			this.Logger.WriteLine(ex.Message);
			return false;
		}
	}

	protected void LogMsgPack(ReadOnlyMemory<byte> msgPack) => this.LogMsgPack(new ReadOnlySequence<byte>(msgPack));

	protected void LogMsgPack(ReadOnlySequence<byte> msgPack)
	{
		this.Logger.WriteLine(this.Serializer.ConvertToJson(msgPack));
	}
}
