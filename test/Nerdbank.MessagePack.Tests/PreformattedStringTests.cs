// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class PreformattedStringTests
{
	private static readonly MessagePackSerializer Serializer = new();

	[Fact]
	public void CtorAndProperties()
	{
		PreformattedString msgpackString = new("abc", MsgPackFormatter.Default);
		Assert.Equal("abc", msgpackString.Value);
		Assert.Equal("abc"u8.ToArray(), msgpackString.Encoded.ToArray());
		Assert.Equal([MessagePackCode.MinFixStr | 3, .. "abc"u8], msgpackString.Formatted.ToArray());
	}

	[Fact]
	public void IsMatch_Span()
	{
		PreformattedString msgpackString = new("abc", MsgPackFormatter.Default);
		Assert.True(msgpackString.IsMatch("abc"u8));
		Assert.False(msgpackString.IsMatch("abcdef"u8));
		Assert.False(msgpackString.IsMatch("ab"u8));
		Assert.False(msgpackString.IsMatch("def"u8));
	}

	[Fact]
	public void IsMatch_Sequence_Contiguous()
	{
		PreformattedString msgpackString = new("abc", MsgPackFormatter.Default);
		Assert.True(msgpackString.IsMatch(ContiguousSequence("abc"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("abcdef"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("ab"u8)));
		Assert.False(msgpackString.IsMatch(ContiguousSequence("def"u8)));
	}

	[Fact]
	public void IsMatch_Sequence_NonContiguous()
	{
		PreformattedString msgpackString = new("abc", MsgPackFormatter.Default);

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
		Reader matchingReaderContiguous = new Reader(Serializer.Serialize<string, Witness>("abc", TestContext.Current.CancellationToken), MsgPackDeformatter.Default);
		Reader matchingReaderFragmented = new Reader(SplitSequence<byte>(Serializer.Serialize<string, Witness>("abc", TestContext.Current.CancellationToken), 2), MsgPackDeformatter.Default);
		Reader mismatchingReader = new Reader(Serializer.Serialize<string, Witness>("def", TestContext.Current.CancellationToken), MsgPackDeformatter.Default);
		Reader nilReader = new Reader(Serializer.Serialize<string, Witness>(null, TestContext.Current.CancellationToken), MsgPackDeformatter.Default);
		Reader intReader = new Reader(Serializer.Serialize<int, Witness>(3, TestContext.Current.CancellationToken), MsgPackDeformatter.Default);

		PreformattedString msgpackString = new("abc", MsgPackFormatter.Default);

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
		PreformattedString abc1 = new("abc", MsgPackFormatter.Default);
		PreformattedString abc2 = new("abc", MsgPackFormatter.Default);
		PreformattedString def = new("def", MsgPackFormatter.Default);

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

	[GenerateShape<string>]
	[GenerateShape<int>]
	private partial class Witness;
}
