// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Tests that guard against memory allocation amplification, where a small msgpack payload
/// declares many nested containers whose declared element counts all draw upon the same
/// trailing bytes as evidence that the elements exist.
/// </summary>
public partial class AllocationAmplificationTests
{
	/// <summary>
	/// Builds a payload of <paramref name="levels"/> nested array16 headers, each declaring
	/// <paramref name="declaredCount"/> elements, followed by <paramref name="declaredCount"/>
	/// one-byte nil values.
	/// </summary>
	/// <remarks>
	/// Every nested header individually satisfies a "remaining bytes >= declared count" test
	/// because they all count the same trailing nil bytes.
	/// </remarks>
	private static byte[] NestedArrays(int levels, int declaredCount)
	{
		List<byte> bytes = new((levels * 3) + declaredCount);
		for (int i = 0; i < levels; i++)
		{
			bytes.Add(MessagePackCode.Array16);
			bytes.Add((byte)(declaredCount >> 8));
			bytes.Add((byte)declaredCount);
		}

		bytes.AddRange(Enumerable.Repeat(MessagePackCode.Nil, declaredCount));
		return bytes.ToArray();
	}

	/// <inheritdoc cref="NestedArrays"/>
	private static byte[] NestedMaps(int levels, int declaredCount)
	{
		List<byte> bytes = new((levels * 3) + (declaredCount * 2));
		for (int i = 0; i < levels; i++)
		{
			bytes.Add(MessagePackCode.Map16);
			bytes.Add((byte)(declaredCount >> 8));
			bytes.Add((byte)declaredCount);
		}

		bytes.AddRange(Enumerable.Repeat(MessagePackCode.Nil, declaredCount * 2));
		return bytes.ToArray();
	}

	private static void ReadArrayHeaders(ReadOnlyMemory<byte> payload, int levels)
	{
		MessagePackReader reader = new(payload);
		for (int i = 0; i < levels; i++)
		{
			reader.ReadArrayHeader();
		}
	}

	private static void ReadMapHeaders(ReadOnlyMemory<byte> payload, int levels)
	{
		MessagePackReader reader = new(payload);
		for (int i = 0; i < levels; i++)
		{
			reader.ReadMapHeader();
		}
	}

	public class Attacks : AllocationAmplificationTests
	{
		[Test]
		public void NestedArrayHeaders_CannotAllShareTheSameTrailingBytes()
		{
			// 100 nested arrays each declaring 1000 elements, but only 1000 trailing bytes exist.
			// Only a small number of these headers can legitimately be honored.
			byte[] payload = NestedArrays(levels: 100, declaredCount: 1000);
			Assert.Throws<EndOfStreamException>(() => ReadArrayHeaders(payload, 100));
		}

		[Test]
		public void NestedMapHeaders_CannotAllShareTheSameTrailingBytes()
		{
			byte[] payload = NestedMaps(levels: 100, declaredCount: 1000);
			Assert.Throws<EndOfStreamException>(() => ReadMapHeaders(payload, 100));
		}

		[Test]
		public void MixedArrayAndMapNesting_IsRejected()
		{
			List<byte> bytes = new();
			for (int i = 0; i < 100; i++)
			{
				bytes.Add(i % 2 == 0 ? MessagePackCode.Array16 : MessagePackCode.Map16);
				bytes.Add(0x03);
				bytes.Add(0xE8); // 1000
			}

			bytes.AddRange(Enumerable.Repeat(MessagePackCode.Nil, 2000));
			byte[] payload = bytes.ToArray();

			Assert.Throws<EndOfStreamException>(() =>
			{
				MessagePackReader reader = new(payload);
				for (int i = 0; i < 100; i++)
				{
					if (i % 2 == 0)
					{
						reader.ReadArrayHeader();
					}
					else
					{
						reader.ReadMapHeader();
					}
				}
			});
		}

		/// <summary>
		/// Verifies the second nested header is rejected, proving the guard is aggregate
		/// rather than merely capping the total.
		/// </summary>
		[Test]
		public void SecondNestedHeader_IsRejectedImmediately()
		{
			byte[] payload = NestedArrays(levels: 100, declaredCount: 1000);
			MessagePackReader reader = new(payload);

			// The first header is legitimately satisfiable by the trailing bytes.
			Assert.Equal(1000, reader.ReadArrayHeader());

			// The second cannot be, because the first already laid claim to those bytes.
			Assert.Throws<EndOfStreamException>(() =>
			{
				MessagePackReader inner = new(payload);
				inner.ReadArrayHeader();
				inner.ReadArrayHeader();
			});
		}

		/// <summary>
		/// The end-to-end deserialization of a recursive type containing an array.
		/// This requires no opt-in converters.
		/// </summary>
		[Test]
		public void RecursiveArrayType_DoesNotAmplifyAllocation()
		{
			byte[] payload = BuildRecursiveNodePayload(levels: 34, declaredCount: 4096);
			MessagePackSerializer serializer = new();

			AssertRejectedWithinAllocationBudget<MessagePackSerializationException>(
				() => serializer.Deserialize<ArrayNode>(payload, TestContext.Current!.Execution.CancellationToken),
				allocationBudget: 64 * 1024,
				payload.Length);
		}

		/// <summary>
		/// Raising <see cref="SerializationContext.MaxDepth"/> must not restore the amplification,
		/// proving the depth limit is not the control doing the work.
		/// </summary>
		[Test]
		public void RecursiveArrayType_WithHighMaxDepth_DoesNotAmplifyAllocation()
		{
			// Only 60 levels of nesting, but a MaxDepth far above what this payload reaches,
			// so the depth limit cannot be what rejects the payload.
			byte[] payload = BuildRecursiveNodePayload(levels: 60, declaredCount: 0xFFFF);
			MessagePackSerializer serializer = new()
			{
				StartingContext = new SerializationContext { MaxDepth = 1000 },
			};

			AssertRejectedWithinAllocationBudget<MessagePackSerializationException>(
				() => serializer.Deserialize<ArrayNode>(payload, TestContext.Current!.Execution.CancellationToken),
				allocationBudget: 256 * 1024,
				payload.Length);
		}

		/// <summary>
		/// Opting out of the collection preallocation cap must not restore the amplification,
		/// proving that setting is not the only control doing the work.
		/// </summary>
		[Test]
		public void RecursiveArrayType_WithTrustedDataSecurity_DoesNotAmplifyAllocation()
		{
			byte[] payload = BuildRecursiveNodePayload(levels: 34, declaredCount: 0xFFFF);
			MessagePackSerializer serializer = new()
			{
				StartingContext = new SerializationContext { Security = SecuritySettings.TrustedData },
			};

			AssertRejectedWithinAllocationBudget<MessagePackSerializationException>(
				() => serializer.Deserialize<ArrayNode>(payload, TestContext.Current!.Execution.CancellationToken),
				allocationBudget: 2 * 1024 * 1024,
				payload.Length);
		}

		/// <summary>
		/// The optional untyped converter reads arbitrary msgpack into object graphs and must be guarded too.
		/// </summary>
		[Test]
		public void ObjectConverter_DoesNotAmplifyAllocation()
		{
			byte[] payload = NestedArrays(levels: 60, declaredCount: 4096);
			MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();

			AssertRejectedWithinAllocationBudget<MessagePackSerializationException>(
				() => serializer.Deserialize<object, Witness>(payload, TestContext.Current!.Execution.CancellationToken),
				allocationBudget: 64 * 1024,
				payload.Length);
		}

		/// <summary>
		/// A payload that nests far more deeply than <see cref="SerializationContext.MaxDepth"/> must be
		/// rejected by the depth limit before converter recursion can exhaust the stack.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Each level of this payload costs only one byte, so it is proportionally sized and the aggregate
		/// byte guard permits it. The depth limit is therefore the only control that can stop it, and this
		/// test pins that it does.
		/// </para>
		/// <para>
		/// Measured on a default 1 MB stack, roughly 2,000 nested converter frames are enough to terminate
		/// the process, while the default <see cref="SerializationContext.MaxDepth"/> of 64 caps recursion
		/// about 25 times below that. If depth limiting ever regressed, this test would fail by crashing the
		/// test process with a stack overflow rather than by a normal assertion failure.
		/// </para>
		/// </remarks>
		[Test]
		public void DeeplyNestedSingleElementArrays_AreRejectedBeforeStackExhaustion()
		{
			byte[] payload = [.. Enumerable.Repeat((byte)(MessagePackCode.MinFixArray | 0x01), 10_000), MessagePackCode.Nil];
			MessagePackSerializer serializer = new MessagePackSerializer().WithObjectConverter();

			MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
				() => serializer.Deserialize<object, Witness>(payload, TestContext.Current!.Execution.CancellationToken));

			// Confirm the depth limit is what rejected it, rather than some incidental failure.
			Assert.Contains("depth", ex.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// The asynchronous deserialization path reads container headers from a streaming reader
		/// whose buffer may still be growing, so it cannot use the same byte-based reservation.
		/// It must nonetheless refuse to preallocate storage that the input has not paid for.
		/// </summary>
		[Test]
		public async Task RecursiveArrayType_Async_DoesNotAmplifyAllocation()
		{
			// No filler bytes at all: the async reader never sees evidence that any element exists.
			byte[] payload = BuildRecursiveNodePayload(levels: 34, declaredCount: 0xFFFF, fillerCount: 0);
			MessagePackSerializer serializer = new() { MaxAsyncBuffer = 0 };

			async Task DeserializeAsync()
			{
				PipeReader pipeReader = PipeReader.Create(new MemoryStream(payload));
				try
				{
					await serializer.DeserializeAsync<ArrayNode>(pipeReader, TestContext.Current!.Execution.CancellationToken);
				}
				finally
				{
					await pipeReader.CompleteAsync();
				}
			}

			// Warm up so JIT and first-use allocations are not attributed to the measurement.
			try
			{
				await DeserializeAsync();
			}
			catch (Exception)
			{
			}

#if NET
			long before = GC.GetAllocatedBytesForCurrentThread();
#endif
			await Assert.ThrowsAsync<MessagePackSerializationException>(DeserializeAsync);
#if NET
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.True(
				allocated < 256 * 1024,
				$"Allocated {allocated} bytes from a {payload.Length} byte payload ({(double)allocated / payload.Length:N0}x amplification).");
#endif
		}

		/// <summary>
		/// Builds the msgpack for a chain of <see cref="ArrayNode"/> instances, each of whose
		/// <see cref="ArrayNode.Children"/> array declares a large element count.
		/// </summary>
		/// <param name="levels">The number of nested nodes to declare.</param>
		/// <param name="declaredCount">The element count each node's array header declares.</param>
		/// <param name="fillerCount">
		/// The number of trailing one-byte nil values to append, or <see langword="null"/>
		/// to append <paramref name="declaredCount"/> of them.
		/// </param>
		private static byte[] BuildRecursiveNodePayload(int levels, int declaredCount, int? fillerCount = null)
		{
			List<byte> bytes = new();
			for (int i = 0; i < levels; i++)
			{
				bytes.Add(MessagePackCode.MinFixArray | 0x01); // The node, as an array of 1 property.
				bytes.Add(MessagePackCode.Array16); // The Children property.
				bytes.Add((byte)(declaredCount >> 8));
				bytes.Add((byte)declaredCount);
			}

			bytes.AddRange(Enumerable.Repeat(MessagePackCode.Nil, fillerCount ?? declaredCount));
			return bytes.ToArray();
		}

		/// <summary>
		/// Asserts that <paramref name="action"/> rejects a malicious payload, and (where the runtime
		/// can measure it) that the rejection happens without allocating a disproportionate amount of memory.
		/// </summary>
		/// <typeparam name="TException">The type of exception expected from <paramref name="action"/>.</typeparam>
		/// <param name="action">The deserialization to attempt. It is invoked more than once.</param>
		/// <param name="allocationBudget">The maximum number of bytes that may be allocated by <paramref name="action"/>.</param>
		/// <param name="payloadLength">The length of the attack payload, for diagnostic messages.</param>
		private static void AssertRejectedWithinAllocationBudget<TException>(Action action, long allocationBudget, int payloadLength)
			where TException : Exception
		{
			// Warm up so JIT and first-use allocations are not attributed to the measurement.
			try
			{
				action();
			}
			catch (Exception)
			{
			}

#if NET
			long before = GC.GetAllocatedBytesForCurrentThread();
#endif
			Assert.Throws<TException>(action);
#if NET
			long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
			Assert.True(
				allocated < allocationBudget,
				$"Allocated {allocated} bytes from a {payloadLength} byte payload ({(double)allocated / payloadLength:N0}x amplification).");
#endif
		}
	}

	public class PositiveCompatibility : AllocationAmplificationTests
	{
		[Test]
		public void ValidDeeplyNestedArrays_AreAccepted()
		{
			// 50 nested single-element arrays terminated by a nil.
			byte[] payload = [.. Enumerable.Repeat((byte)(MessagePackCode.MinFixArray | 0x01), 50), MessagePackCode.Nil];

			MessagePackReader reader = new(payload);
			for (int i = 0; i < 50; i++)
			{
				Assert.Equal(1, reader.ReadArrayHeader());
			}

			Assert.True(reader.TryReadNil());
			Assert.True(reader.End);
		}

		[Test]
		public void ValidNestedMaps_AreAccepted()
		{
			// { 1: { 2: 3 } }
			byte[] payload = [MessagePackCode.MinFixMap | 0x01, 0x01, MessagePackCode.MinFixMap | 0x01, 0x02, 0x03];

			MessagePackReader reader = new(payload);
			Assert.Equal(1, reader.ReadMapHeader());
			Assert.Equal(1, reader.ReadInt32());
			Assert.Equal(1, reader.ReadMapHeader());
			Assert.Equal(2, reader.ReadInt32());
			Assert.Equal(3, reader.ReadInt32());
			Assert.True(reader.End);
		}

		[Test]
		public void EmptyContainers_AreAccepted()
		{
			byte[] payload = [MessagePackCode.MinFixArray, MessagePackCode.MinFixMap, MessagePackCode.Array16, 0, 0];

			MessagePackReader reader = new(payload);
			Assert.Equal(0, reader.ReadArrayHeader());
			Assert.Equal(0, reader.ReadMapHeader());
			Assert.Equal(0, reader.ReadArrayHeader());
			Assert.True(reader.End);
		}

		/// <summary>
		/// After a container's contents are fully consumed, its reservation must be released
		/// so a subsequent top-level value gets the full remaining budget.
		/// </summary>
		[Test]
		public void ConsecutiveTopLevelContainers_AreAccepted()
		{
			byte[] payload =
			[
				MessagePackCode.MinFixArray | 0x02, MessagePackCode.Nil, MessagePackCode.Nil,
				MessagePackCode.MinFixArray | 0x02, MessagePackCode.Nil, MessagePackCode.Nil,
			];

			MessagePackReader reader = new(payload);
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.TryReadNil());
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.End);
		}

		[Test]
		public void SiblingContainers_AreAccepted()
		{
			// [ [1,2], [3,4] ]
			byte[] payload =
			[
				MessagePackCode.MinFixArray | 0x02,
				MessagePackCode.MinFixArray | 0x02, 0x01, 0x02,
				MessagePackCode.MinFixArray | 0x02, 0x03, 0x04,
			];

			MessagePackReader reader = new(payload);
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(1, reader.ReadInt32());
			Assert.Equal(2, reader.ReadInt32());
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(3, reader.ReadInt32());
			Assert.Equal(4, reader.ReadInt32());
			Assert.True(reader.End);
		}

		/// <summary>
		/// Children encoded with more than the one-byte minimum must not exhaust the budget.
		/// </summary>
		[Test]
		public void LargeChildEncodings_AreAccepted()
		{
			Sequence<byte> seq = new();
			MessagePackWriter writer = new(seq);
			writer.WriteArrayHeader(3);
			for (int i = 0; i < 3; i++)
			{
				writer.WriteArrayHeader(2);
				writer.Write(long.MaxValue);
				writer.Write("a somewhat lengthy string value");
			}

			writer.Flush();

			MessagePackReader reader = new(seq.AsReadOnlySequence);
			Assert.Equal(3, reader.ReadArrayHeader());
			for (int i = 0; i < 3; i++)
			{
				Assert.Equal(2, reader.ReadArrayHeader());
				Assert.Equal(long.MaxValue, reader.ReadInt64());
				Assert.Equal("a somewhat lengthy string value", reader.ReadString());
			}

			Assert.True(reader.End);
		}

		[Test]
		public void FragmentedSequence_IsAccepted()
		{
			ReadOnlySequence<byte> payload = SequenceBuilder.Create(
				new byte[] { MessagePackCode.MinFixArray | 0x02 },
				new byte[] { MessagePackCode.MinFixArray | 0x01 },
				new byte[] { MessagePackCode.Nil },
				new byte[] { MessagePackCode.Nil });

			MessagePackReader reader = new(payload);
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(1, reader.ReadArrayHeader());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.End);
		}

		/// <summary>
		/// A peek reader must inherit the accounting state so it enforces the same limits,
		/// and must not disturb the original reader's state.
		/// </summary>
		[Test]
		public void PeekReader_InheritsAccountingAndDoesNotDisturbOriginal()
		{
			byte[] payload = NestedArrays(levels: 100, declaredCount: 1000);

			MessagePackReader reader = new(payload);
			Assert.Equal(1000, reader.ReadArrayHeader());

			// A peek reader taken after the first reservation must enforce that same reservation.
			Assert.Throws<EndOfStreamException>(() => PeekThenReadAnotherHeader(payload));

			// The original reader is unaffected and still positioned where it was.
			Assert.Equal(3, reader.Consumed);

			// It can still complete its own read of the elements it reserved.
			MessagePackReader peek = reader.CreatePeekReader();
			Assert.Equal(3, peek.Consumed);
		}

		/// <summary>
		/// A reader cloned over a replacement buffer starts with a fresh budget for that buffer.
		/// </summary>
		[Test]
		public void CloneOverReplacementBuffer_GetsFreshBudget()
		{
			byte[] outer = NestedArrays(levels: 100, declaredCount: 1000);
			MessagePackReader reader = new(outer);
			Assert.Equal(1000, reader.ReadArrayHeader());

			byte[] replacement = [MessagePackCode.MinFixArray | 0x02, MessagePackCode.Nil, MessagePackCode.Nil];
			MessagePackReader clone = reader.Clone(new ReadOnlySequence<byte>(replacement));
			Assert.Equal(2, clone.ReadArrayHeader());
			Assert.True(clone.TryReadNil());
			Assert.True(clone.TryReadNil());
		}

		/// <summary>
		/// Skip must continue to work, and must retire reservations as it consumes bytes.
		/// </summary>
		[Test]
		public void Skip_RetiresReservations()
		{
			// [ [nil,nil], [nil,nil] ]
			byte[] payload =
			[
				MessagePackCode.MinFixArray | 0x02,
				MessagePackCode.MinFixArray | 0x02, MessagePackCode.Nil, MessagePackCode.Nil,
				MessagePackCode.MinFixArray | 0x02, MessagePackCode.Nil, MessagePackCode.Nil,
			];

			SerializationContext context = new();
			MessagePackReader reader = new(payload);
			Assert.Equal(2, reader.ReadArrayHeader());
			reader.Skip(context);
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.TryReadNil());
			Assert.True(reader.End);
		}

		/// <summary>
		/// The non-allocating probe APIs intentionally remain unguarded so that streaming
		/// callers can inspect a header before the body has arrived.
		/// </summary>
		[Test]
		public void TryReadArrayHeader_RemainsUnguarded()
		{
			byte[] payload = [MessagePackCode.Array16, 0x03, 0xE8];

			MessagePackReader reader = new(payload);
			Assert.True(reader.TryReadArrayHeader(out int count));
			Assert.Equal(1000, count);
		}

		/// <summary>
		/// Round-trips a genuinely deep, valid object graph to prove ordinary data still works.
		/// </summary>
		[Test]
		public void DeepValidObjectGraph_RoundTrips()
		{
			ArrayNode root = new();
			ArrayNode current = root;
			for (int i = 0; i < 20; i++)
			{
				ArrayNode child = new();
				current.Children = [child];
				current = child;
			}

			MessagePackSerializer serializer = new();
			byte[] msgpack = serializer.Serialize(root, TestContext.Current!.Execution.CancellationToken);
			ArrayNode? result = serializer.Deserialize<ArrayNode>(msgpack, TestContext.Current!.Execution.CancellationToken);

			int depth = 0;
			for (ArrayNode? node = result; node?.Children is [ArrayNode child]; node = child)
			{
				depth++;
			}

			Assert.Equal(20, depth);
		}

		[Test]
		public void WideValidArray_RoundTrips()
		{
			int[] values = Enumerable.Range(0, 50_000).ToArray();
			MessagePackSerializer serializer = new();
			byte[] msgpack = serializer.Serialize<int[], Witness>(values, TestContext.Current!.Execution.CancellationToken);
			Assert.Equal(values, serializer.Deserialize<int[], Witness>(msgpack, TestContext.Current!.Execution.CancellationToken));
		}

		[Test]
		public void WideValidDictionary_RoundTrips()
		{
			Dictionary<int, int> values = Enumerable.Range(0, 20_000).ToDictionary(i => i, i => i);
			MessagePackSerializer serializer = new();
			byte[] msgpack = serializer.Serialize<Dictionary<int, int>, Witness>(values, TestContext.Current!.Execution.CancellationToken);
			Assert.Equal(values, serializer.Deserialize<Dictionary<int, int>, Witness>(msgpack, TestContext.Current!.Execution.CancellationToken));
		}

		private static void PeekThenReadAnotherHeader(ReadOnlyMemory<byte> payload)
		{
			MessagePackReader reader = new(payload);
			reader.ReadArrayHeader();
			MessagePackReader peek = reader.CreatePeekReader();
			peek.ReadArrayHeader();
		}
	}

	public class Arithmetic : AllocationAmplificationTests
	{
		/// <summary>
		/// A declared count at <see cref="int.MaxValue"/> must be rejected without allocating.
		/// </summary>
		[Test]
		public void ArrayCountAtIntMaxValue_IsRejected()
		{
			byte[] payload = [MessagePackCode.Array32, 0x7F, 0xFF, 0xFF, 0xFF, MessagePackCode.Nil];
			Assert.Throws<EndOfStreamException>(() => ReadArrayHeaders(payload, 1));
		}

		/// <summary>
		/// A wire count exceeding <see cref="int.MaxValue"/> is rejected by the count conversion.
		/// </summary>
		[Test]
		public void ArrayCountExceedingIntMaxValue_IsRejected()
		{
			byte[] payload = [MessagePackCode.Array32, 0xFF, 0xFF, 0xFF, 0xFF, MessagePackCode.Nil];
			Assert.ThrowsAny<OverflowException>(() => ReadArrayHeaders(payload, 1));
		}

		/// <summary>
		/// A map count whose doubling would overflow a 32-bit integer must not wrap around
		/// into a small (and therefore permissive) value.
		/// </summary>
		[Test]
		public void MapCountThatWouldOverflowWhenDoubled_IsRejected()
		{
			// 0x40000000 * 2 overflows Int32 to a negative number.
			byte[] payload = [MessagePackCode.Map32, 0x40, 0x00, 0x00, 0x00, MessagePackCode.Nil];
			Assert.Throws<EndOfStreamException>(() => ReadMapHeaders(payload, 1));
		}

		[Test]
		public void MapCountAtUIntMaxValue_IsRejected()
		{
			byte[] payload = [MessagePackCode.Map32, 0xFF, 0xFF, 0xFF, 0xFF, MessagePackCode.Nil];
			Assert.ThrowsAny<Exception>(() => ReadMapHeaders(payload, 1));
		}
	}

	[GenerateShape]
	public partial class ArrayNode
	{
		[Key(0)]
		public ArrayNode[]? Children { get; set; }
	}

	[GenerateShapeFor<object>]
	[GenerateShapeFor<int[]>]
	[GenerateShapeFor<Dictionary<int, int>>]
	internal partial class Witness;
}
