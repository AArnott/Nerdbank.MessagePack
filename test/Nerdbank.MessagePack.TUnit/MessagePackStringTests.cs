// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class MessagePackStringTests : MessagePackSerializerTestBase
{
	[Test]
	public void CtorAndProperties()
	{
		MessagePackString msgpackString = new("abc");
		Assert.Equal("abc", msgpackString.Value);
		Assert.Equal("abc"u8.ToArray(), msgpackString.Utf8.ToArray());
		Assert.Equal([MessagePackCode.MinFixStr | 3, .. "abc"u8], msgpackString.MsgPack.ToArray());
	}

	[Test]
	public void IsMatch_Span()
	{
		MessagePackString msgpackString = new("abc");
		Assert.True(msgpackString.IsMatch("abc"u8));
		Assert.False(msgpackString.IsMatch("abcdef"u8));
		Assert.False(msgpackString.IsMatch("ab"u8));
		Assert.False(msgpackString.IsMatch("def"u8));
	}

	[Test]
	public void IsMatch_Sequence_Contiguous()
	{
		MessagePackString msgpackString = new("abc");
		Assert.True(msgpackString.IsMatch(ContiguousSequence("abc"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("abcdef"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("ab"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("def"u8)));
	}

	[Test]
	public void IsMatch_Sequence_NonContiguous()
	{
		MessagePackString msgpackString = new("abc");

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

	[Test]
	public void TryRead()
	{
		MessagePackReader matchingReaderContiguous = new(this.Serializer.Serialize<string, Witness>("abc", this.TimeoutToken));
		MessagePackReader matchingReaderFragmented = new(SplitSequence<byte>(this.Serializer.Serialize<string, Witness>("abc", this.TimeoutToken), 2));
		MessagePackReader mismatchingReader = new(this.Serializer.Serialize<string, Witness>("def", this.TimeoutToken));
		MessagePackReader nilReader = new(this.Serializer.Serialize<string, Witness>(null, this.TimeoutToken));
		MessagePackReader intReader = new(this.Serializer.Serialize<int, Witness>(3, this.TimeoutToken));

		MessagePackString msgpackString = new("abc");

		Assert.True(msgpackString.TryRead(ref matchingReaderContiguous));
		Assert.True(msgpackString.TryRead(ref matchingReaderFragmented));
		Assert.False(msgpackString.TryRead(ref mismatchingReader));
		Assert.False(msgpackString.TryRead(ref nilReader));
		Assert.False(msgpackString.TryRead(ref intReader));

		Assert.True(matchingReaderContiguous.End);
		Assert.False(intReader.End);
	}

	[Test]
	public void Equals_GetHashCode()
	{
		MessagePackString abc1 = new("abc");
		MessagePackString abc2 = new("abc");
		MessagePackString def = new("def");

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

	[GenerateShapeFor<string>]
	[GenerateShapeFor<int>]
	private partial class Witness;
}
