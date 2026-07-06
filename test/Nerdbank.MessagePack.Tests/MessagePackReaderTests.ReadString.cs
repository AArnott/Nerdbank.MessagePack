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
		ReadOnlySequence<byte> seq = SequenceBuilder.Create(new[]
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
		ReadOnlySequence<byte> seq = SequenceBuilder.Create(
			new[] { (byte)(MessagePackCode.MinFixStr + 2), (byte)'A' },
			new[] { (byte)'B' });

		var reader = new MessagePackReader(seq);
		var result = reader.ReadString();
		Assert.Equal("AB", result);
	}

	[Fact]
	public void ReadString_HandlesMultipleSegments_WithEmptySegment()
	{
		ReadOnlySequence<byte> seq = SequenceBuilder.Create(
			new[] { (byte)(MessagePackCode.MinFixStr + 2), (byte)'A' },
			[],
			new[] { (byte)'B' });

		var reader = new MessagePackReader(seq);
		var result = reader.ReadString();
		Assert.Equal("AB", result);
	}

	[Fact]
	[Trait("CWE", "682")]
	public void ReadString_HandlesMultipleSegments_WithExpectedRemainingStructures()
	{
		ReadOnlySequence<byte> seq = SequenceBuilder.Create(
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

}
