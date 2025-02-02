// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Trait("AsyncSerialization", "true")]
public partial class StreamingEnumerableTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	/// <summary>
	/// Streams multiple elements with no array envelope.
	/// </summary>
	[Fact]
	public async Task DeserializeEnumerableAsync_TopLevel_PipeReader()
	{
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		for (int i = 1; i <= 10; i++)
		{
			writer.Write(i);
		}

		writer.Flush();

		int readCount = 0;
		PipeReader reader = PipeReader.Create(sequence);
		await foreach (int current in this.Serializer.DeserializeEnumerableAsync<int>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken))
		{
			readCount++;
			this.Logger.WriteLine(current.ToString());
		}

		Assert.Equal(10, readCount);
	}

	/// <summary>
	/// Streams multiple elements with no array envelope.
	/// </summary>
	[Fact]
	public async Task DeserializeEnumerableAsync_TopLevel_Empty()
	{
		PipeReader reader = PipeReader.Create(new([]));
		await foreach (int current in this.Serializer.DeserializeEnumerableAsync<int>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken))
		{
			Assert.Fail("No items should have been read.");
		}
	}

	/// <summary>
	/// Streams multiple elements with no array envelope.
	/// </summary>
	[Fact]
	public async Task DeserializeEnumerableAsync_TopLevel_Stream()
	{
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		for (int i = 1; i <= 10; i++)
		{
			writer.Write(i);
		}

		writer.Flush();

		int readCount = 0;
		MemoryStream reader = new(sequence.AsReadOnlySequence.ToArray());
		await foreach (int current in this.Serializer.DeserializeEnumerableAsync<int>(reader, Witness.ShapeProvider.Resolve<int>(), TestContext.Current.CancellationToken))
		{
			readCount++;
			this.Logger.WriteLine(current.ToString());
		}

		Assert.Equal(10, readCount);
	}

	/// <summary>
	/// Streams elements of a top-level msgpack array.
	/// </summary>
	[Fact]
	public async Task DeserializeEnumerableAsync_Array()
	{
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(10);
		for (int i = 1; i <= 10; i++)
		{
			writer.Write(i);
		}

		writer.Flush();

		int readCount = 0;
		PipeReader reader = PipeReader.Create(sequence);
		MessagePackSerializer.StreamingEnumerationOptions<int[], int> options = new(a => a);
		await foreach (int current in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			readCount++;
			this.Logger.WriteLine(current.ToString());
		}

		Assert.Equal(10, readCount);
	}

	[Trait("ReferencePreservation", "true")]
	[Fact]
	public async Task DeserializeEnumerableAsync_ReferencesPreserved()
	{
		this.Serializer = this.Serializer with { PreserveReferences = true };
		SimpleStreamingContainerKeyed original = new();
		byte[] msgpack = this.Serializer.Serialize<SimpleStreamingContainerKeyed[], Witness>([original, original], TestContext.Current.CancellationToken);
		this.LogMsgPack(new(msgpack));

		PipeReader reader = PipeReader.Create(new(msgpack));
		MessagePackSerializer.StreamingEnumerationOptions<SimpleStreamingContainerKeyed[], SimpleStreamingContainerKeyed> options = new(a => a);
		List<SimpleStreamingContainerKeyed?> actual = new();
		NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async delegate
		{
			await foreach (SimpleStreamingContainerKeyed? item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
			{
				actual.Add(item);
			}

			Assert.Equal(2, actual.Count);
			Assert.Same(actual[0], actual[1]);
		});
		this.Logger.WriteLine(ex.Message);
	}

	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_SequenceWithinTwoContainers(bool leaveOpen)
	{
		OuterStreamingContainer container = new(new(true, [1, 2, 3], true));
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainer, int> options = new(c => c.Inner!.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_SequenceWithinTwoContainers_Keyed(bool leaveOpen, bool asMap)
	{
		SimpleStreamingContainerKeyed container = new() { Before = asMap ? null : "a", Values = [1, 2, 3], After = asMap ? null : "b" };
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<SimpleStreamingContainerKeyed, int> options = new(c => c.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path includes an array indexer.
	/// </summary>
	/// <param name="leaveOpen">Whether to the reader should be positioned at the next element.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_StepThroughArray(bool leaveOpen)
	{
		OuterStreamingContainerByArray container = new([null, new(true, [1, 2, 3], false), new(true, [1], false)]);
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainerByArray, int> options = new(c => c.Inner[1]!.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path includes an indexer.
	/// </summary>
	/// <param name="leaveOpen">Whether to the reader should be positioned at the next element.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_StepThroughImmutableArray(bool leaveOpen)
	{
		OuterStreamingContainerByImmutableArray container = new([null, new(true, [1, 2, 3], false), new(true, [1], false)]);
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainerByImmutableArray, int> options = new(c => c.Inner[1]!.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path includes an indexer.
	/// </summary>
	/// <param name="leaveOpen">Whether to the reader should be positioned at the next element.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_StepThroughDictionary(bool leaveOpen)
	{
		OuterStreamingContainerByDictionary container = new(new() { ["a"] = null, ["b"] = new(true, [1, 2, 3], false) });
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainerByDictionary, int> options = new(c => c.Inner["b"]!.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path includes an indexer.
	/// </summary>
	/// <param name="leaveOpen">Whether to the reader should be positioned at the next element.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_StepThroughDictionaryCustomKey(bool leaveOpen)
	{
		OuterStreamingContainerByDictionaryCustomKey container = new(new() { [new CustomKey(5)] = null, [new CustomKey(3)] = new(true, [1, 2, 3], false) });
		Sequence<byte> msgpack = new();
		msgpack.Append(this.Serializer.Serialize(container, TestContext.Current.CancellationToken));
		msgpack.Append(this.Serializer.Serialize<string, Witness>("hi", TestContext.Current.CancellationToken));
		this.LogMsgPack(msgpack);
		PipeReader reader = PipeReader.Create(msgpack);

		CustomKey key = new(3);
		int count = 0;
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainerByDictionaryCustomKey, int> options = new(c => c.Inner[key]!.Values!)
		{
			LeaveOpen = leaveOpen,
		};
		await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
		{
			Assert.Equal(count++ + 1, item);
		}

		Assert.Equal(3, count);

		if (leaveOpen)
		{
			// Verify correct positioning by deserializing the next top-level structure in the pipe.
			string? actual = await this.Serializer.DeserializeAsync<string>(reader, Witness.ShapeProvider, TestContext.Current.CancellationToken);
			Assert.Equal("hi", actual);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path turns out to be a null value.
	/// </summary>
	/// <param name="preferEmptySequence">A value indicating whether we're verifying behavior that prefers an empty sequence over throwing when a null is encountered.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_NullRoot(bool preferEmptySequence)
	{
		byte[] msgpack = this.Serializer.Serialize<OuterStreamingContainer>(null, TestContext.Current.CancellationToken);
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainer, int> options = new(c => c.Inner!.Values!)
		{
			EmptySequenceForUndiscoverablePath = preferEmptySequence,
		};
		PipeReader reader = PipeReader.Create(new(msgpack));
		try
		{
			await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
			{
				Assert.Fail("Should not have received any items.");
			}

			Assert.True(preferEmptySequence, "Should have thrown an exception.");
		}
		catch (MessagePackSerializationException ex)
		{
			this.Logger.WriteLine(ex.ToString());
			Assert.False(preferEmptySequence, "Should not have thrown an exception.");
			Assert.Matches(@"\Wc(?!\.Inner)", ex.Message);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path turns out to be a null value.
	/// </summary>
	/// <param name="preferEmptySequence">A value indicating whether we're verifying behavior that prefers an empty sequence over throwing when a null is encountered.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_NullMidPath(bool preferEmptySequence)
	{
		byte[] msgpack = this.Serializer.Serialize(new OuterStreamingContainer(null), TestContext.Current.CancellationToken);
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainer, int> options = new(c => c.Inner!.Values!)
		{
			EmptySequenceForUndiscoverablePath = preferEmptySequence,
		};
		PipeReader reader = PipeReader.Create(new(msgpack));
		try
		{
			await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
			{
				Assert.Fail("Should not have received any items.");
			}

			Assert.True(preferEmptySequence, "Should have thrown an exception.");
		}
		catch (MessagePackSerializationException ex)
		{
			this.Logger.WriteLine(ex.ToString());
			Assert.False(preferEmptySequence, "Should not have thrown an exception.");
			Assert.Matches(@"\Wc\.Inner(?!\.Values)", ex.Message);
		}
	}

	/// <summary>
	/// Verifies handling when the specified path turns out to be a null value.
	/// </summary>
	/// <param name="preferEmptySequence">A value indicating whether we're verifying behavior that prefers an empty sequence over throwing when a null is encountered.</param>
	[Theory, PairwiseData]
	public async Task DeserializeEnumerableAsync_NullSequenceMember(bool preferEmptySequence)
	{
		byte[] msgpack = this.Serializer.Serialize(new OuterStreamingContainer(new(true, null, false)), TestContext.Current.CancellationToken);
		MessagePackSerializer.StreamingEnumerationOptions<OuterStreamingContainer, int> options = new(c => c.Inner!.Values!)
		{
			EmptySequenceForUndiscoverablePath = preferEmptySequence,
		};
		PipeReader reader = PipeReader.Create(new(msgpack));
		try
		{
			await foreach (int item in this.Serializer.DeserializeEnumerableAsync(reader, Witness.ShapeProvider, options, TestContext.Current.CancellationToken))
			{
				Assert.Fail("Should not have received any items.");
			}

			Assert.True(preferEmptySequence, "Should have thrown an exception.");
		}
		catch (MessagePackSerializationException ex)
		{
			this.Logger.WriteLine(ex.ToString());
			Assert.False(preferEmptySequence, "Should not have thrown an exception.");
			Assert.Matches(@"\Wc\.Inner\.Values", ex.Message);
		}
	}

	public record struct CustomKey(int Key);

	[GenerateShape<string>]
	[GenerateShape<int>]
	[GenerateShape<SimpleStreamingContainerKeyed[]>]
	private partial class Witness;

	[GenerateShape]
	public partial record SimpleStreamingContainer(bool Before, int[]? Values, bool After);

	[GenerateShape]
	public partial record OuterStreamingContainer(SimpleStreamingContainer? Inner);

	[GenerateShape]
	public partial record OuterStreamingContainerByArray(SimpleStreamingContainer?[] Inner);

	[GenerateShape]
	public partial record OuterStreamingContainerByImmutableArray(ImmutableArray<SimpleStreamingContainer?> Inner);

	[GenerateShape]
	public partial record OuterStreamingContainerByDictionary(Dictionary<string, SimpleStreamingContainer?> Inner);

	[GenerateShape]
	public partial record OuterStreamingContainerByDictionaryCustomKey(Dictionary<CustomKey, SimpleStreamingContainer?> Inner);

	[GenerateShape]
	public partial class SimpleStreamingContainerKeyed
	{
		[Key(0)]
		public string? Before { get; set; }

		[Key(1)]
		public string? After { get; set; }

		[Key(2)]
		public int[]? Values { get; set; }
	}
}
