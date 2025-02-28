// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract partial class PreformattedStringTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	[Fact]
	public void IsMatch_Span()
	{
		PreformattedString msgpackString = new("abc", this.Serializer.Formatter);
		Assert.True(msgpackString.IsMatch("abc"u8));
		Assert.False(msgpackString.IsMatch("abcdef"u8));
		Assert.False(msgpackString.IsMatch("ab"u8));
		Assert.False(msgpackString.IsMatch("def"u8));
	}

	[Fact]
	public void IsMatch_Sequence_Contiguous()
	{
		PreformattedString msgpackString = new("abc", this.Serializer.Formatter);
		Assert.True(msgpackString.IsMatch(ContiguousSequence("abc"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("abcdef"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("ab"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("def"u8)));
	}

	[Fact]
	public void IsMatch_Sequence_NonContiguous()
	{
		PreformattedString msgpackString = new("abc", this.Serializer.Formatter);

		for (int i = 0; i <= 3; i++)
		{
			Assert.True(msgpackString.IsMatch(SplitSequence("abc"u8, i)));
		}

		Assert.True(msgpackString.IsMatch(SplitSequence("abc"u8, 1, 2)));

		Assert.False(msgpackString.IsMatch(SplitSequence("abcdef"u8, 2)));
		Assert.False(msgpackString.IsMatch(SplitSequence("abcdef"u8, 3)));
		Assert.False(msgpackString.IsMatch(SplitSequence("abcdef"u8, 4)));

		Assert.False(msgpackString.IsMatch(SplitSequence("ab"u8, 1)));
		Assert.False(msgpackString.IsMatch(SplitSequence("def"u8, 2)));
	}

	[Fact]
	public void TryRead()
	{
		Reader matchingReaderContiguous = new Reader(this.Serializer.Serialize<string, Witness>("abc", TestContext.Current.CancellationToken), this.Serializer.Deformatter);
		Reader matchingReaderFragmented = new Reader(SplitSequence<byte>(this.Serializer.Serialize<string, Witness>("abc", TestContext.Current.CancellationToken), 2), this.Serializer.Deformatter);
		Reader mismatchingReader = new Reader(this.Serializer.Serialize<string, Witness>("def", TestContext.Current.CancellationToken), this.Serializer.Deformatter);
		Reader nilReader = new Reader(this.Serializer.Serialize<string, Witness>(null, TestContext.Current.CancellationToken), this.Serializer.Deformatter);
		Reader intReader = new Reader(this.Serializer.Serialize<int, Witness>(3, TestContext.Current.CancellationToken), this.Serializer.Deformatter);

		PreformattedString msgpackString = new("abc", this.Serializer.Formatter);

		Assert.True(msgpackString.TryRead(ref matchingReaderContiguous));
		Assert.True(msgpackString.TryRead(ref matchingReaderFragmented));
		Assert.False(msgpackString.TryRead(ref mismatchingReader));
		Assert.False(msgpackString.TryRead(ref nilReader));
		Assert.False(msgpackString.TryRead(ref intReader));

		Assert.True(matchingReaderContiguous.End);
		Assert.False(intReader.End);
	}

	[Fact]
	public void Equals_GetHashCode()
	{
		PreformattedString abc1 = new("abc", this.Serializer.Formatter);
		PreformattedString abc2 = new("abc", this.Serializer.Formatter);
		PreformattedString def = new("def", this.Serializer.Formatter);

		Assert.True(abc1.Equals(abc2));
		Assert.True(abc1.Equals((object?)abc2));
		Assert.False(abc1.Equals(def));
		Assert.False(abc1.Equals((object?)def));

		Assert.Equal(abc1.GetHashCode(), abc2.GetHashCode());
		Assert.NotEqual(abc1.GetHashCode(), def.GetHashCode());
	}

	private static ReadOnlySequence<T> ContiguousSequence<T>(ReadOnlySpan<T> span) => new(span.ToArray());

	private static ReadOnlySequence<T> SplitSequence<T>(ReadOnlySpan<T> span, params ReadOnlySpan<int> positions)
	{
		Sequence<T> seq = new();
		for (int i = 0; i < positions.Length; i++)
		{
			int start = i == 0 ? 0 : positions[i - 1];
			int end = positions[i];
			seq.Append(span[start..end].ToArray());
		}

		if (positions[^1] < span.Length)
		{
			seq.Append(span[positions[^1]..].ToArray());
		}

		return seq;
	}

	public class Json() : PreformattedStringTests(CreateJsonSerializer());

	public class MsgPack() : PreformattedStringTests(CreateMsgPackSerializer())
	{
		[Fact]
		public void CtorAndProperties()
		{
			PreformattedString msgpackString = new("abc", this.Serializer.Formatter);
			Assert.Equal("abc", msgpackString.Value);
			Assert.Equal("abc"u8.ToArray(), msgpackString.Encoded.ToArray());
			Assert.Equal([ShapeShift.MessagePack.MessagePackCode.MinFixStr | 3, .. "abc"u8], msgpackString.Formatted.ToArray());
		}
	}

	[GenerateShape<string>]
	[GenerateShape<int>]
	private partial class Witness;
}
