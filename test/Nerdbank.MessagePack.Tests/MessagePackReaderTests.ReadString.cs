// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET
using System.Runtime.CompilerServices;
#endif

public partial class MessagePackReaderTests
{
	[Fact]
	public void ReadString_HandlesSingleSegment()
	{
		ReadOnlySequence<byte> seq = this.BuildSequence(new[]
		{
			(byte)(MessagePackCode.MinFixStr + 2),
			(byte)'A', (byte)'B',
		});

		var reader = new MessagePackReader(seq);
		var result = reader.ReadString();
		Assert.Equal("AB", result);
	}

	[Fact]
	public void ReadString_HandlesMultipleSegments()
	{
		ReadOnlySequence<byte> seq = this.BuildSequence(
			new[] { (byte)(MessagePackCode.MinFixStr + 2), (byte)'A' },
			new[] { (byte)'B' });

		var reader = new MessagePackReader(seq);
		var result = reader.ReadString();
		Assert.Equal("AB", result);
	}

	[Fact]
	[Trait("CWE", "682")]
	public void ReadString_HandlesMultipleSegments_WithExpectedRemainingStructures()
	{
		ReadOnlySequence<byte> seq = this.BuildSequence(
			new[] { (byte)(MessagePackCode.MinFixArray + 2), (byte)(MessagePackCode.MinFixStr + 3), (byte)'A' },
			new[] { (byte)'B', (byte)'C', (byte)MessagePackCode.Nil });

		var reader = new MessagePackReader(seq);
		Assert.Equal(2, reader.ReadArrayHeader());
		AssertExpectedRemainingStructures(ref reader, 2);

		Assert.Equal("ABC", reader.ReadString());
		AssertExpectedRemainingStructures(ref reader, 1);

		Assert.True(reader.TryReadNil());
		AssertExpectedRemainingStructures(ref reader, 0);
	}

	private static void AssertExpectedRemainingStructures(ref MessagePackReader reader, uint expected)
	{
#if NET
		Assert.Equal(expected, GetExpectedRemainingStructures(ref reader));
#else
		Assert.Skip("This test validates internal reader accounting that requires UnsafeAccessor.");
#endif
	}

#if NET
	[UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ExpectedRemainingStructures")]
	private static extern uint GetExpectedRemainingStructures(ref MessagePackReader reader);
#endif

	private ReadOnlySequence<T> BuildSequence<T>(params T[][] segmentContents)
	{
		if (segmentContents.Length == 1)
		{
			return new ReadOnlySequence<T>(segmentContents[0].AsMemory());
		}

		var bufferSegment = new BufferSegment<T>(segmentContents[0].AsMemory());
		BufferSegment<T>? last = default;
		for (var i = 1; i < segmentContents.Length; i++)
		{
			last = bufferSegment.Append(segmentContents[i]);
		}

		return new ReadOnlySequence<T>(bufferSegment, 0, last!, last!.Memory.Length);
	}

	internal class BufferSegment<T> : ReadOnlySequenceSegment<T>
	{
		public BufferSegment(ReadOnlyMemory<T> memory)
		{
			this.Memory = memory;
		}

		public BufferSegment<T> Append(ReadOnlyMemory<T> memory)
		{
			var segment = new BufferSegment<T>(memory)
			{
				RunningIndex = this.RunningIndex + this.Memory.Length,
			};
			this.Next = segment;
			return segment;
		}
	}
}
