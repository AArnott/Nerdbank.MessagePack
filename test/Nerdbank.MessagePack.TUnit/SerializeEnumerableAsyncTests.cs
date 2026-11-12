// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

public partial class SerializeEnumerableAsyncTests : MessagePackSerializerTestBase
{
	private static readonly byte[] ThreeIntsAsArray = [0x93, 1, 2, 3];

	private static readonly byte[] ThreeIntsConcatenated = [1, 2, 3];

	private enum FlushFailure
	{
		Throw,
		Canceled,
		Completed,
	}

	[Test]
	public void Serialize_Int32Array_GoldenBytes_Unchanged()
	{
		byte[] actual = this.Serializer.Serialize<int[], Witness>([1, 2, 3], this.TimeoutToken);
		Assert.Equal(ThreeIntsAsArray, actual);
	}

	[Test]
	public async Task SerializeAsync_Int32Array_MemoryStream_GoldenBytes_Unchanged()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeAsync<int[], Witness>(stream, [1, 2, 3], this.TimeoutToken);
		Assert.Equal(ThreeIntsAsArray, stream.ToArray());
	}

	[Test]
	public async Task SerializeAsync_Int32Array_PipeStream_GoldenBytes_Unchanged()
	{
		MemoryStream inner = new();
		using MonitoringStream stream = new(inner);
		await this.Serializer.SerializeAsync<int[], Witness>(stream, [1, 2, 3], this.TimeoutToken);
		Assert.Equal(ThreeIntsAsArray, inner.ToArray());
	}

	[Test]
	public async Task SerializeAsync_IAsyncEnumerable_StillWritesOneArray()
	{
		MemoryStream inner = new();
		using MonitoringStream stream = new(inner);
		await this.Serializer.SerializeAsync(stream, Count(3), Shape<IAsyncEnumerable<int>, Witness>(), this.TimeoutToken);
		Assert.Equal(ThreeIntsAsArray, inner.ToArray());
	}

	[Test]
	public async Task DeserializeEnumerableAsync_ConcatenatedInts_Unchanged()
	{
		PipeReader reader = PipeReader.Create(new ReadOnlySequence<byte>(ThreeIntsConcatenated));
		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(reader, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal([1, 2, 3], actual);
	}

	[Test]
	public async Task DeserializeEnumerableAsync_TopLevelArray_IsNotAnElementSequence()
	{
		PipeReader reader = PipeReader.Create(new ReadOnlySequence<byte>(ThreeIntsAsArray));
		await Assert.ThrowsAsync<MessagePackSerializationException>(async () =>
		{
			await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(reader, this.TimeoutToken))
			{
				_ = value;
			}
		});
	}

	[Test]
	public async Task SerializeEnumerableAsync_WritesConcatenatedValues()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeEnumerableAsync(stream, Count(3), Shape<int, Witness>(), this.TimeoutToken);

		Assert.Equal(ThreeIntsConcatenated, stream.ToArray());
		Assert.NotEqual(ThreeIntsAsArray, stream.ToArray());

		stream.Position = 0;
		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(stream, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal([1, 2, 3], actual);
	}

	[Test]
	public async Task SerializeEnumerableAsync_Empty_WritesNothing()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeEnumerableAsync(stream, Empty<int>(), Shape<int, Witness>(), this.TimeoutToken);
		Assert.Empty(stream.ToArray());

		stream.Position = 0;
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(stream, this.TimeoutToken))
		{
			_ = value;
			Assert.Fail("An empty sequence should not produce a value.");
		}
	}

	[Test]
	public async Task SerializeEnumerableAsync_NullElement_RoundTrips()
	{
		ITypeShape<string> shape = Shape<string, Witness>();
		byte[] expected = [.. this.Serializer.Serialize("a", shape, this.TimeoutToken), MessagePackCode.Nil, .. this.Serializer.Serialize("b", shape, this.TimeoutToken)];

		MemoryStream stream = new();
		await this.Serializer.SerializeEnumerableAsync<string, Witness>(stream, StringsWithNull(), this.TimeoutToken);
		Assert.Equal(expected, stream.ToArray());

		stream.Position = 0;
		List<string?> actual = [];
		await foreach (string? value in this.Serializer.DeserializeEnumerableAsync(stream, shape, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal(["a", null, "b"], actual);
	}

	[Test]
	public async Task SerializeEnumerableAsync_Objects_RoundTripThroughFriendlyOverload()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeEnumerableAsync(stream, People(), this.TimeoutToken);

		stream.Position = 0;
		List<Person?> actual = [];
		await foreach (Person? person in this.Serializer.DeserializeEnumerableAsync<Person>(stream, this.TimeoutToken))
		{
			actual.Add(person);
		}

		Assert.Equal([new Person(1, "a"), null, new Person(2, "b")], actual);
		Assert.True(stream.CanWrite);
	}

	[Test]
	public async Task SerializeEnumerableAsync_Pipe_RemainsOpenForAnotherValue()
	{
		Pipe pipe = new();
		await this.Serializer.SerializeEnumerableAsync(pipe.Writer, Count(2), Shape<int, Witness>(), this.TimeoutToken);
		await pipe.Writer.WriteAsync(new byte[] { 3 }, this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(pipe.Reader, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal([1, 2, 3], actual);
	}

	[Test]
	public async Task SerializeEnumerableAsync_FlushesBeforeSequenceCompletes()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with { UnflushedBytesThreshold = 16 },
		};

		Pipe pipe = new();
		TaskCompletionSource<bool> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Task writing = this.Serializer.SerializeEnumerableAsync(pipe.Writer, BlockAfter(30, observed), Shape<int, Witness>(), this.TimeoutToken).AsTask();
		try
		{
			using CancellationTokenSource readTimeout = new(TimeSpan.FromSeconds(2));
			ReadResult read = await pipe.Reader.ReadAsync(readTimeout.Token);
			Assert.False(read.Buffer.IsEmpty);

			// Leave the bytes unconsumed so the later enumeration still sees the prefix that was flushed early.
			pipe.Reader.AdvanceTo(read.Buffer.Start);
		}
		finally
		{
			observed.TrySetResult(true);
		}

		await writing;
		await pipe.Writer.CompleteAsync();

		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(pipe.Reader, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal(Enumerable.Range(0, 31), actual);
	}

	[Test]
	public async Task SerializeEnumerableAsync_PreservingReferences_ThrowsBeforeWriting()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		MemoryStream stream = new();
		await Assert.ThrowsAsync<NotSupportedException>(() => this.Serializer.SerializeEnumerableAsync(stream, Count(2), Shape<int, Witness>(), this.TimeoutToken).AsTask());
		Assert.Equal(0, stream.Length);
		Assert.True(stream.CanWrite);
	}

	[Test]
	public async Task SerializeEnumerableAsync_Cancellation_IsNotWrapped()
	{
		ITypeShape<int> shape = Shape<int, Witness>();
		using CancellationTokenSource cts = new();
		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeEnumerableAsync(Stream.Null, Count(1), shape, cts.Token).AsTask());

		using CancellationTokenSource during = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeEnumerableAsync(Stream.Null, CancelAfterFirst(during), shape, during.Token).AsTask());
	}

	[Test]
	public async Task SerializeEnumerableAsync_StartingContextCancellation_IsHonored()
	{
		SerializationContext context = new() { CancellationToken = new CancellationToken(canceled: true) };
		Pipe pipe = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeEnumerableAsync(pipe.Writer, Count(1), Shape<int, Witness>(), context).AsTask());
	}

	[Test]
	public async Task SerializeEnumerableAsync_ElementFailure_IncludesIndex_AndDoesNotFlushPartialStream()
	{
		WriteTrackingStream stream = new();
		MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeEnumerableAsync(stream, FailingElements(), Shape<ThrowsOnRead, Witness>(), this.TimeoutToken).AsTask());
		Assert.Contains("index 1", GetFullMessage(ex), StringComparison.Ordinal);
		Assert.Equal(0, stream.BytesWritten);
		Assert.False(stream.IsDisposed);
	}

	[Test]
	public async Task SerializeEnumerableAsync_NullArguments()
	{
		ITypeShape<int> shape = Shape<int, Witness>();
		Pipe pipe = new();
		await Assert.ThrowsAsync<ArgumentNullException>(() => this.Serializer.SerializeEnumerableAsync((PipeWriter)null!, Count(1), shape, this.TimeoutToken).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => this.Serializer.SerializeEnumerableAsync(pipe.Writer, null!, shape, this.TimeoutToken).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => this.Serializer.SerializeEnumerableAsync(pipe.Writer, Count(1), null!, this.TimeoutToken).AsTask());
		await Assert.ThrowsAsync<ArgumentNullException>(() => this.Serializer.SerializeEnumerableAsync((Stream)null!, Count(1), shape, this.TimeoutToken).AsTask());
	}

	[Test]
	public async Task SerializeArrayAsync_MatchesExistingArrayEncoding()
	{
		Person[] people = [new Person(1, "a"), new Person(2, "b")];
		byte[] expected = this.Serializer.Serialize<Person[], Witness>(people, this.TimeoutToken);

		MemoryStream stream = new();
		await this.Serializer.SerializeArrayAsync(stream, Repeat(people), people.Length, Shape<Person, Witness>(), this.TimeoutToken);
		Assert.Equal(expected, stream.ToArray());

		stream.Position = 0;
		Person[]? actual = await this.Serializer.DeserializeAsync<Person[], Witness>(stream, this.TimeoutToken);
		Assert.Equal(people, actual);
	}

	[Test]
	public async Task SerializeArrayAsync_Ints_UseStandardArrayHeader()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeArrayAsync<int, Witness>(stream, Count(3), 3, this.TimeoutToken);
		Assert.Equal(ThreeIntsAsArray, stream.ToArray());
		Assert.NotEqual(ThreeIntsConcatenated, stream.ToArray());
	}

	[Test]
	public async Task SerializeArrayAsync_Empty_WritesEmptyArrayHeader()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeArrayAsync(stream, Empty<int>(), 0, Shape<int, Witness>(), this.TimeoutToken);
		Assert.Equal(new byte[] { MessagePackCode.MinFixArray }, stream.ToArray());

		stream.Position = 0;
		int[]? actual = await this.Serializer.DeserializeAsync<int[], Witness>(stream, this.TimeoutToken);
		Assert.NotNull(actual);
		Assert.Empty(actual);
	}

	[Test]
	public async Task SerializeArrayAsync_CountMismatch_Throws()
	{
		ITypeShape<int> shape = Shape<int, Witness>();
		MessagePackSerializationException fewer = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeArrayAsync(Stream.Null, Count(1), 2, shape, this.TimeoutToken).AsTask());
		Assert.IsType<InvalidOperationException>(fewer.InnerException);
		Assert.Contains("fewer", fewer.InnerException.Message, StringComparison.Ordinal);

		MessagePackSerializationException more = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeArrayAsync(Stream.Null, Count(3), 2, shape, this.TimeoutToken).AsTask());
		Assert.IsType<InvalidOperationException>(more.InnerException);
		Assert.Contains("more", more.InnerException.Message, StringComparison.Ordinal);

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => this.Serializer.SerializeArrayAsync(Stream.Null, Count(1), -1, shape, this.TimeoutToken).AsTask());
	}

	[Test]
	public async Task SerializeArrayAsync_FlushesBeforeSequenceCompletes()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with { UnflushedBytesThreshold = 16 },
		};

		const int count = 40;
		Pipe pipe = new();
		TaskCompletionSource<bool> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Task writing = this.Serializer.SerializeArrayAsync(pipe.Writer, BlockAfter(20, observed, count), count, Shape<int, Witness>(), this.TimeoutToken).AsTask();
		try
		{
			using CancellationTokenSource readTimeout = new(TimeSpan.FromSeconds(2));
			ReadResult read = await pipe.Reader.ReadAsync(readTimeout.Token);
			Assert.False(read.Buffer.IsEmpty);

			// Leave the bytes unconsumed so deserialization still sees the array header that was flushed early.
			pipe.Reader.AdvanceTo(read.Buffer.Start);
		}
		finally
		{
			observed.TrySetResult(true);
		}

		await writing;
		await pipe.Writer.CompleteAsync();

		int[]? actual = await this.Serializer.DeserializeAsync<int[], Witness>(pipe.Reader, this.TimeoutToken);
		Assert.Equal(Enumerable.Range(0, count), actual);
	}

	[Test]
	public async Task SerializeArrayAsync_PreservesReferencesWithinTheArray()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		Node shared = new() { Name = "shared" };
		Node[] nodes = [shared, shared];
		byte[] expected = this.Serializer.Serialize<Node[], Witness>(nodes, this.TimeoutToken);

		MemoryStream stream = new();
		await this.Serializer.SerializeArrayAsync(stream, SharedNodes(shared), nodes.Length, Shape<Node, Witness>(), this.TimeoutToken);
		Assert.Equal(expected, stream.ToArray());

		stream.Position = 0;
		Node?[]? actual = await this.Serializer.DeserializeAsync<Node[], Witness>(stream, this.TimeoutToken);
		Assert.NotNull(actual);
		Assert.Equal(2, actual.Length);
		Assert.Same(actual[0], actual[1]);
		Assert.Equal("shared", actual[0]!.Name);
	}

	[Test]
	public async Task SerializeArrayAsync_TopLevelArray_CanBeStreamedByPath()
	{
		Pipe pipe = new();
		await this.Serializer.SerializeArrayAsync(pipe.Writer, Count(4), 4, Shape<int, Witness>(), this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		MessagePackSerializer.StreamingEnumerationOptions<int[], int> options = new(values => values);
		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializePathEnumerableAsync<int[], int, Witness>(pipe.Reader, options, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal([1, 2, 3, 4], actual);
	}

	[Test]
	public async Task SerializeEnumerableAsync_YieldAfterCancel_DoesNotWriteTheNextValue()
	{
		using CancellationTokenSource cts = new();
		Pipe pipe = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeEnumerableAsync(pipe.Writer, CancelThenNull(cts), Shape<string, Witness>(), cts.Token).AsTask());
		Assert.Equal(0, pipe.Writer.UnflushedBytes);

		await pipe.Writer.CompleteAsync();
		ReadResult read = await pipe.Reader.ReadAsync(this.TimeoutToken);
		Assert.True(read.Buffer.IsEmpty);
	}

	[Test]
	public async Task SerializeArrayAsync_YieldNullAfterCancel_DoesNotWriteNil()
	{
		using CancellationTokenSource cts = new();
		Pipe pipe = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeArrayAsync(pipe.Writer, CancelThenNullArray(cts), 2, Shape<int[], Witness>(), cts.Token).AsTask());
		Assert.Equal(0, pipe.Writer.UnflushedBytes);

		await pipe.Writer.CompleteAsync();
		ReadResult read = await pipe.Reader.ReadAsync(this.TimeoutToken);
		Assert.True(read.Buffer.IsEmpty);
	}

	[Test]
	public async Task SerializeEnumerableAsync_CanceledDuringElement_DoesNotForwardTheChunk()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with { UnflushedBytesThreshold = 0 },
		};

		using CancellationTokenSource cts = new();
		Pipe pipe = new();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => this.Serializer.SerializeEnumerableAsync(pipe.Writer, CancelsWhileWriting(cts), Shape<CancelsOnGet, Witness>(), cts.Token).AsTask());
		Assert.Equal(0, pipe.Writer.UnflushedBytes);

		await pipe.Writer.CompleteAsync();
		ReadResult read = await pipe.Reader.ReadAsync(this.TimeoutToken);
		Assert.True(read.Buffer.IsEmpty);
	}

	[Test]
	public async Task SerializeArrayAsync_Failure_DoesNotLeavePartialBytesOnThePipe()
	{
		Pipe pipe = new();
		await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeArrayAsync(pipe.Writer, Count(1), 2, Shape<int, Witness>(), this.TimeoutToken).AsTask());
		Assert.Equal(0, pipe.Writer.UnflushedBytes);

		await this.Serializer.SerializeEnumerableAsync(pipe.Writer, Count(1), Shape<int, Witness>(), this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(pipe.Reader, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal([1], actual);
	}

	[Test]
	public async Task SerializeSequence_DestinationFlushFailure_RetainsCopiedBytesAndDoesNotCompleteTheWriter()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with { UnflushedBytesThreshold = 0 },
		};

		await AssertFailureAsync(FlushFailure.Throw);
		await AssertFailureAsync(FlushFailure.Canceled);
		await AssertFailureAsync(FlushFailure.Completed);

		async Task AssertFailureAsync(FlushFailure failure)
		{
			FlushFailingWriter enumerableWriter = new(failure);
			MessagePackSerializationException enumerable = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeEnumerableAsync(enumerableWriter, Count(1), Shape<int, Witness>(), this.TimeoutToken).AsTask());
			AssertFlushFailure(enumerable, enumerableWriter, failure, [1]);

			FlushFailingWriter arrayWriter = new(failure);
			MessagePackSerializationException array = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeArrayAsync(arrayWriter, Count(2), 2, Shape<int, Witness>(), this.TimeoutToken).AsTask());
			AssertFlushFailure(array, arrayWriter, failure, [0x92, 1]);
		}
	}

	[Test]
	public async Task SerializeEnumerableAsync_FlushWhileBufferIsCheckedOut_KeepsLaterBytes()
	{
		MemoryStream stream = new();
		await this.Serializer.SerializeEnumerableAsync(stream, One(new BufferProbe()), Shape<BufferProbe, BufferProbe>(), this.TimeoutToken);

		MessagePackReader reader = new(stream.ToArray());
		for (int i = 0; i < 200; i++)
		{
			Assert.Equal(1, reader.ReadInt32());
		}

		Assert.Equal(0x01020304, reader.ReadInt32());
		Assert.True(reader.End);
	}

	[Test]
	public async Task SerializeEnumerableAsync_FlushesWhenWriterCannotReportUnflushedBytes()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with { UnflushedBytesThreshold = 16 },
		};

		Pipe pipe = new();
		OpaqueBufferedWriter writer = new(pipe.Writer);
		TaskCompletionSource<bool> observed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Task writing = this.Serializer.SerializeEnumerableAsync(writer, BlockAfter(30, observed), Shape<int, Witness>(), this.TimeoutToken).AsTask();
		try
		{
			using CancellationTokenSource readTimeout = new(TimeSpan.FromSeconds(2));
			ReadResult read = await pipe.Reader.ReadAsync(readTimeout.Token);
			Assert.False(read.Buffer.IsEmpty);
			pipe.Reader.AdvanceTo(read.Buffer.Start);
		}
		finally
		{
			observed.TrySetResult(true);
		}

		await writing;
		await writer.FlushAsync(this.TimeoutToken);
		await pipe.Writer.CompleteAsync();

		List<int> actual = [];
		await foreach (int value in this.Serializer.DeserializeEnumerableAsync<int, Witness>(pipe.Reader, this.TimeoutToken))
		{
			actual.Add(value);
		}

		Assert.Equal(Enumerable.Range(0, 31), actual);
	}

	[Test]
	public async Task SerializeArrayAsync_ElementFailure_IncludesIndex()
	{
		MessagePackSerializationException ex = await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeArrayAsync(Stream.Null, FailingElements(), 3, Shape<ThrowsOnRead, Witness>(), this.TimeoutToken).AsTask());
		Assert.Contains("index 1", GetFullMessage(ex), StringComparison.Ordinal);
	}

	private static async IAsyncEnumerable<T> One<T>(T value)
	{
		await Task.CompletedTask;
		yield return value;
	}

	private static async IAsyncEnumerable<int> Count(int count)
	{
		for (int i = 1; i <= count; i++)
		{
			await Task.Yield();
			yield return i;
		}
	}

	private static async IAsyncEnumerable<int> BlockAfter(int flushCount, TaskCompletionSource<bool> observed, int? total = null)
	{
		int limit = total ?? flushCount + 1;
		for (int i = 0; i < limit; i++)
		{
			if (i == flushCount)
			{
				await observed.Task;
			}

			yield return i;
		}
	}

	private static void AssertFlushFailure(MessagePackSerializationException exception, FlushFailingWriter writer, FlushFailure failure, byte[] expectedPrefix)
	{
		switch (failure)
		{
			case FlushFailure.Throw:
				Assert.IsType<IOException>(exception.InnerException);
				Assert.Contains("flush failed", exception.InnerException.Message, StringComparison.Ordinal);
				break;
			case FlushFailure.Canceled:
				Assert.IsType<OperationCanceledException>(exception.InnerException);
				break;
			case FlushFailure.Completed:
				Assert.IsType<EndOfStreamException>(exception.InnerException);
				Assert.Contains("stopped listening", exception.InnerException.Message, StringComparison.Ordinal);
				break;
		}

		Assert.Equal(0, writer.CompleteCalls);
		byte[] written = writer.Written.ToArray();
		Assert.True(written.Length >= expectedPrefix.Length);
		Assert.Equal(expectedPrefix, written.AsSpan(0, expectedPrefix.Length).ToArray());
	}

	private static async IAsyncEnumerable<CancelsOnGet> CancelsWhileWriting(CancellationTokenSource cancellationSource)
	{
		await Task.CompletedTask;
		yield return new CancelsOnGet(cancellationSource);
	}

	private static async IAsyncEnumerable<string?> CancelThenNull(CancellationTokenSource cancellationSource)
	{
		yield return "a";
		cancellationSource.Cancel();
		await Task.CompletedTask;
		yield return null;
	}

	private static async IAsyncEnumerable<int[]?> CancelThenNullArray(CancellationTokenSource cancellationSource)
	{
		yield return [1];
		cancellationSource.Cancel();
		await Task.CompletedTask;
		yield return null;
	}

	private static async IAsyncEnumerable<int> CancelAfterFirst(CancellationTokenSource cancellationSource)
	{
		yield return 1;
		cancellationSource.Cancel();
		await Task.Yield();
		yield return 2;
	}

	private static async IAsyncEnumerable<string?> StringsWithNull()
	{
		yield return "a";
		await Task.Yield();
		yield return null;
		yield return "b";
	}

	private static async IAsyncEnumerable<Person?> Repeat(Person[] people)
	{
		foreach (Person person in people)
		{
			await Task.Yield();
			yield return person;
		}
	}

	private static async IAsyncEnumerable<Person?> People()
	{
		yield return new Person(1, "a");
		await Task.Yield();
		yield return null;
		yield return new Person(2, "b");
	}

	private static async IAsyncEnumerable<T> Empty<T>()
	{
		await Task.CompletedTask;
		yield break;
	}

	private static async IAsyncEnumerable<ThrowsOnRead?> FailingElements()
	{
		yield return new ThrowsOnRead { Before = 1 };
		await Task.Yield();
		yield return new ThrowsOnRead { Before = 2 };
	}

	private static async IAsyncEnumerable<Node?> SharedNodes(Node shared)
	{
		yield return shared;
		await Task.Yield();
		yield return shared;
	}

	private static ITypeShape<T> Shape<T, TProvider>()
#if NET
		where TProvider : IShapeable<T>
		=> TProvider.GetTypeShape();
#else
		=> TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>();
#endif

	[GenerateShape]
	public partial record Person(int Id, string? Name);

	[GenerateShape]
	public partial class Node
	{
		public string? Name { get; set; }
	}

	[GenerateShape]
	public partial class CancelsOnGet(CancellationTokenSource source)
	{
		public int Value
		{
			get
			{
				source.Cancel();
				return 1;
			}

			set
			{
			}
		}
	}

	[GenerateShape]
	public partial class ThrowsOnRead
	{
		public int Before { get; set; }

		public int Value
		{
			get => this.Before == 2 ? throw new InvalidOperationException("element failed") : 0;
			set { }
		}
	}

	[GenerateShape]
	[MessagePackConverter(typeof(BufferProbeConverter))]
	public partial class BufferProbe;

	public sealed class BufferProbeConverter : MessagePackConverter<BufferProbe>
	{
		public override bool PreferAsyncSerialization => true;

		public override BufferProbe? Read(ref MessagePackReader reader, SerializationContext context) => throw new NotSupportedException();

		public override void Write(ref MessagePackWriter writer, in BufferProbe? value, SerializationContext context) => throw new NotSupportedException();

		public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, BufferProbe? value, SerializationContext context)
		{
			MessagePackWriter syncWriter = writer.CreateWriter();
			for (int i = 0; i < 200; i++)
			{
				syncWriter.Write(1);
			}

			writer.ReturnWriter(ref syncWriter);
			writer.Flush();

			// Check out the next buffer and then flush without writing into it.
			syncWriter = writer.CreateWriter();
			writer.ReturnWriter(ref syncWriter);
			await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);

			syncWriter = writer.CreateWriter();
			syncWriter.Write(0x01020304);
			writer.ReturnWriter(ref syncWriter);
		}
	}

	/// <summary>
	/// A pipe writer that keeps copied bytes and then fails <see cref="FlushAsync(CancellationToken)"/>.
	/// </summary>
	private sealed class FlushFailingWriter(FlushFailure failure) : PipeWriter
	{
		private readonly Sequence<byte> buffer = new();

		public int CompleteCalls { get; private set; }

		public ReadOnlySequence<byte> Written => this.buffer;

		public override bool CanGetUnflushedBytes => true;

		public override long UnflushedBytes => this.buffer.Length;

		public override void Advance(int bytes) => this.buffer.Advance(bytes);

		public override Memory<byte> GetMemory(int sizeHint = 0) => this.buffer.GetMemory(sizeHint);

		public override Span<byte> GetSpan(int sizeHint = 0) => this.buffer.GetSpan(sizeHint);

		public override void CancelPendingFlush()
		{
		}

		public override void Complete(Exception? exception = null) => this.CompleteCalls++;

		public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
		{
			if (failure == FlushFailure.Throw)
			{
				throw new IOException("flush failed");
			}

			return new(new FlushResult(isCanceled: failure == FlushFailure.Canceled, isCompleted: failure == FlushFailure.Completed));
		}
	}

	/// <summary>
	/// A pipe writer that hides its unflushed byte count and publishes bytes only from <see cref="FlushAsync(CancellationToken)"/>.
	/// </summary>
	private sealed class OpaqueBufferedWriter(PipeWriter inner) : PipeWriter
	{
		private readonly Sequence<byte> buffer = new();

		public override bool CanGetUnflushedBytes => false;

		public override void Advance(int bytes) => this.buffer.Advance(bytes);

		public override Memory<byte> GetMemory(int sizeHint = 0) => this.buffer.GetMemory(sizeHint);

		public override Span<byte> GetSpan(int sizeHint = 0) => this.buffer.GetSpan(sizeHint);

		public override void CancelPendingFlush() => inner.CancelPendingFlush();

		public override void Complete(Exception? exception = null) => inner.Complete(exception);

		public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
		{
			ReadOnlySequence<byte> pending = this.buffer;
			if (!pending.IsEmpty)
			{
				foreach (ReadOnlyMemory<byte> segment in pending)
				{
					inner.Write(segment.Span);
				}

				this.buffer.Reset();
			}

			return inner.FlushAsync(cancellationToken);
		}
	}

	private sealed class WriteTrackingStream : Stream
	{
		public long BytesWritten { get; private set; }

		public bool IsDisposed { get; private set; }

		public override bool CanRead => false;

		public override bool CanSeek => false;

		public override bool CanWrite => true;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

		public override void SetLength(long value) => throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count) => this.BytesWritten += count;

#if NET
		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			this.BytesWritten += buffer.Length;
			return default;
		}
#endif

		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	[GenerateShapeFor<int>]
	[GenerateShapeFor<int[]>]
	[GenerateShapeFor<string>]
	[GenerateShapeFor<IAsyncEnumerable<int>>]
	[GenerateShapeFor<Person>]
	[GenerateShapeFor<Person[]>]
	[GenerateShapeFor<Node>]
	[GenerateShapeFor<Node[]>]
	[GenerateShapeFor<CancelsOnGet>]
	[GenerateShapeFor<ThrowsOnRead>]
	private partial class Witness;
}
