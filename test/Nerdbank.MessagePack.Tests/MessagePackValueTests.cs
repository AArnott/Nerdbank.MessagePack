// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Runtime.CompilerServices;

public class MessagePackValueTests
{
	[Fact]
	public void StructSize()
	{
		// This test is here to ensure that the struct doesn't grow unexpectedly.
		Assert.Equal(24, Unsafe.SizeOf<MessagePackValue>());
	}

	[Fact]
	public void ByteConversion()
	{
		// Test normal value
		MessagePackValue token = (byte)42;
		Assert.Equal(42, (byte)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = byte.MinValue;
		Assert.Equal(byte.MinValue, (byte)token);

		token = byte.MaxValue;
		Assert.Equal(byte.MaxValue, (byte)token);
	}

	[Fact]
	public void SByteConversion()
	{
		// Test normal value
		MessagePackValue token = -42;
		Assert.Equal(-42, (sbyte)token);
		Assert.Equal(MessagePackValueKind.SignedInteger, token.Kind);

		// Test boundary values
		token = sbyte.MinValue;
		Assert.Equal(sbyte.MinValue, (sbyte)token);

		token = sbyte.MaxValue;
		Assert.Equal(sbyte.MaxValue, (sbyte)token);
	}

	[Fact]
	public void UShortConversion()
	{
		// Test normal value
		MessagePackValue token = (ushort)54321;
		Assert.Equal(54321, (ushort)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = ushort.MinValue;
		Assert.Equal(ushort.MinValue, (ushort)token);

		token = ushort.MaxValue;
		Assert.Equal(ushort.MaxValue, (ushort)token);
	}

	[Fact]
	public void ShortConversion()
	{
		// Test normal value
		MessagePackValue token = (short)12345;
		Assert.Equal(12345, (short)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = short.MinValue;
		Assert.Equal(short.MinValue, (short)token);

		token = short.MaxValue;
		Assert.Equal(short.MaxValue, (short)token);
	}

	[Fact]
	public void UIntConversion()
	{
		// Test normal value
		MessagePackValue token = 3000000000U;
		Assert.Equal(3000000000U, (uint)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = uint.MinValue;
		Assert.Equal(uint.MinValue, (uint)token);

		token = uint.MaxValue;
		Assert.Equal(uint.MaxValue, (uint)token);
	}

	[Fact]
	public void IntConversion()
	{
		// Test normal value
		MessagePackValue token = 123456789;
		Assert.Equal(123456789, (int)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = int.MinValue;
		Assert.Equal(int.MinValue, (int)token);

		token = int.MaxValue;
		Assert.Equal(int.MaxValue, (int)token);
	}

	[Fact]
	public void ULongConversion()
	{
		// Test normal value
		MessagePackValue token = 10000000000000000000UL;
		Assert.Equal(10000000000000000000UL, (ulong)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = ulong.MinValue;
		Assert.Equal(ulong.MinValue, (ulong)token);

		token = ulong.MaxValue;
		Assert.Equal(ulong.MaxValue, (ulong)token);
	}

	[Fact]
	public void LongConversion()
	{
		// Test normal value
		MessagePackValue token = 9223372036854775000L;
		Assert.Equal(9223372036854775000L, (long)token);
		Assert.Equal(MessagePackValueKind.UnsignedInteger, token.Kind);

		// Test boundary values
		token = long.MinValue;
		Assert.Equal(long.MinValue, (long)token);

		token = long.MaxValue;
		Assert.Equal(long.MaxValue, (long)token);
	}

	[Fact]
	public void ByteConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 256; // One more than byte.MaxValue
		Assert.Throws<OverflowException>(() => (byte)token);

		token = -1; // One less than byte.MinValue
		Assert.Throws<OverflowException>(() => (byte)token);
	}

	[Fact]
	public void SByteConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 128; // One more than sbyte.MaxValue
		Assert.Throws<OverflowException>(() => (sbyte)token);

		token = -129; // One less than sbyte.MinValue
		Assert.Throws<OverflowException>(() => (sbyte)token);
	}

	[Fact]
	public void UShortConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 65536; // One more than ushort.MaxValue
		Assert.Throws<OverflowException>(() => (ushort)token);

		token = -1; // One less than ushort.MinValue
		Assert.Throws<OverflowException>(() => (ushort)token);
	}

	[Fact]
	public void ShortConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 32768; // One more than short.MaxValue
		Assert.Throws<OverflowException>(() => (short)token);

		token = -32769; // One less than short.MinValue
		Assert.Throws<OverflowException>(() => (short)token);
	}

	[Fact]
	public void UIntConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 4294967296L; // One more than uint.MaxValue
		Assert.Throws<OverflowException>(() => (uint)token);

		token = -1; // One less than uint.MinValue
		Assert.Throws<OverflowException>(() => (uint)token);
	}

	[Fact]
	public void IntConversion_Overflow()
	{
		// Test overflow
		MessagePackValue token = 2147483648L; // One more than int.MaxValue
		Assert.Throws<OverflowException>(() => (int)token);

		token = -2147483649L; // One less than int.MinValue
		Assert.Throws<OverflowException>(() => (int)token);
	}

	[Fact]
	public void ULongConversion_Overflow()
	{
		// Test overflow for ulong (only negative values can overflow)
		MessagePackValue token = -1;
		Assert.Throws<OverflowException>(() => (ulong)token);
	}

	[Fact]
	public void LongConversion_Overflow()
	{
		// Since MessagePackToken uses long/ulong internally,
		// we need to test conversion from ulong values that are too large for long
		MessagePackValue token = ulong.MaxValue;
		Assert.Throws<OverflowException>(() => (long)token);
	}

	[Fact]
	public void BooleanConversion()
	{
		// Test normal value
		MessagePackValue token = true;
		Assert.True((bool)token);
		Assert.Equal(MessagePackValueKind.Boolean, token.Kind);

		token = false;
		Assert.False((bool)token);
	}

	[Fact]
	public void FloatConversion()
	{
		// Test normal value
		MessagePackValue token = 3.14159f;
		Assert.Equal(3.14159f, (float)token);
		Assert.Equal(MessagePackValueKind.Single, token.Kind);

		// Test integer values.
		token = 3;
		Assert.Equal(3f, (float)token);
		token = -3;
		Assert.Equal(-3f, (float)token);

		// Test boundary values
		token = float.MinValue;
		Assert.Equal(float.MinValue, (float)token);

		token = float.MaxValue;
		Assert.Equal(float.MaxValue, (float)token);

		token = float.Epsilon;
		Assert.Equal(float.Epsilon, (float)token);

		token = float.NaN;
		Assert.True(float.IsNaN((float)token));

		token = float.PositiveInfinity;
		Assert.Equal(float.PositiveInfinity, (float)token);

		token = float.NegativeInfinity;
		Assert.Equal(float.NegativeInfinity, (float)token);
	}

	[Fact]
	public void DoubleConversion()
	{
		// Test normal value
		MessagePackValue token = 3.14159265358979;
		Assert.Equal(3.14159265358979, (double)token);
		Assert.Equal(MessagePackValueKind.Double, token.Kind);

		// Test float value
		token = 3f;
		Assert.Equal(3.0, (double)token);

		// Test integer values.
		token = 3;
		Assert.Equal(3.0, (double)token);
		token = -3;
		Assert.Equal(-3.0, (double)token);

		// Test boundary values
		token = double.MinValue;
		Assert.Equal(double.MinValue, (double)token);

		token = double.MaxValue;
		Assert.Equal(double.MaxValue, (double)token);

		token = double.Epsilon;
		Assert.Equal(double.Epsilon, (double)token);

		token = double.NaN;
		Assert.True(double.IsNaN((double)token));

		token = double.PositiveInfinity;
		Assert.Equal(double.PositiveInfinity, (double)token);

		token = double.NegativeInfinity;
		Assert.Equal(double.NegativeInfinity, (double)token);
	}

	[Fact]
	public void FloatConversion_Overflow()
	{
		// Test overflow
		// A double value larger than float.MaxValue
		MessagePackValue token = (double)float.MaxValue * 2;
		Assert.True(float.IsPositiveInfinity((float)token));

		// A double value smaller than float.MinValue
		token = (double)float.MinValue * 2;
		Assert.True(float.IsNegativeInfinity((float)token));
	}

	[Fact]
	public void DoubleConversion_Overflow()
	{
		// There's no value larger than double in MessagePackToken
		// So we can only test invalid type conversions
		MessagePackValue token = true;
		Assert.Throws<InvalidCastException>(() => (double)token);
	}

	[Fact]
	public void StringConversion()
	{
		// Test normal value
		MessagePackValue token = "hi";
		Assert.Equal("hi", (string?)token);
		Assert.Equal(MessagePackValueKind.String, token.Kind);

		// Test null value
		token = (string?)null;
		Assert.Null((string?)token);
		Assert.Equal(MessagePackValueKind.Nil, token.Kind);

		token = 5;
		Assert.Throws<InvalidCastException>(() => (string?)token);
	}

	[Fact]
	public void BinaryConversion()
	{
		// Test normal value
		MessagePackValue token = new byte[] { 1, 2, 3 };
		Assert.Equal(new byte[] { 1, 2, 3 }, (ReadOnlyMemory<byte>)token);
		Assert.Equal(MessagePackValueKind.Binary, token.Kind);

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (ReadOnlyMemory<byte>)token);
	}

	[Fact]
	public void ArrayConversion()
	{
		MessagePackValue token = new MessagePackValue[] { "Hi", "Bye" };
		Assert.Equal(MessagePackValueKind.Array, token.Kind);
		ReadOnlyMemory<MessagePackValue> array = (ReadOnlyMemory<MessagePackValue>)token;
		Assert.Equal(2, array.Length);

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (ReadOnlyMemory<MessagePackValue>)token);
	}

	[Fact]
	public void MapConversion()
	{
		MessagePackValue token = new Dictionary<MessagePackValue, MessagePackValue>
		{
			[1] = "hello",
			[2] = "bye",
		};
		Assert.Equal(MessagePackValueKind.Map, token.Kind);
		Dictionary<MessagePackValue, MessagePackValue> dict = (Dictionary<MessagePackValue, MessagePackValue>)token;
		Assert.Equal(2, dict.Count);

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (ReadOnlyMemory<MessagePackValue>)token);
	}

	[Fact]
	public void MapConversion_Frozen()
	{
		MessagePackValue token = new Dictionary<MessagePackValue, MessagePackValue>
		{
			[1] = "hello",
			[2] = "bye",
		}.ToFrozenDictionary();
		Assert.Equal(MessagePackValueKind.Map, token.Kind);
		FrozenDictionary<MessagePackValue, MessagePackValue> dict = (FrozenDictionary<MessagePackValue, MessagePackValue>)token;
		Assert.Equal(2, dict.Count);

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (ReadOnlyMemory<MessagePackValue>)token);
	}

	[Fact]
	public void ExtensionConversion()
	{
		MessagePackValue token = new Extension(-5, (byte[])[1, 2, 3]);
		Assert.Equal(MessagePackValueKind.Extension, token.Kind);
		Extension ext = (Extension)token;
		Assert.Equal<byte>([1, 2, 3], ext.Data.ToArray());

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (Extension)token);
	}

	[Fact]
	public void DateTimeConversion()
	{
		MessagePackValue token = new DateTime(2025, 01, 12, 1, 1, 1, DateTimeKind.Utc);
		Assert.Equal(MessagePackValueKind.DateTime, token.Kind);
		DateTime value = (DateTime)token;
		Assert.Equal(2025, value.Year);

		// Test bad conversion.
		token = 5;
		Assert.Throws<InvalidCastException>(() => (DateTime)token);
	}

	[Fact]
	public void ToString_Tests()
	{
		Assert.Equal("null", new MessagePackValue(null).ToString());
		Assert.Equal("-50000", new MessagePackValue(-50000).ToString());
		Assert.Equal("50000", new MessagePackValue(50000).ToString());
		Assert.Equal("true", new MessagePackValue(true).ToString());
		Assert.Equal("false", new MessagePackValue(false).ToString());
		Assert.Equal("1.23", new MessagePackValue(1.23f).ToString());
		Assert.Equal("1.23", new MessagePackValue(1.23).ToString());
		Assert.Equal("test", new MessagePackValue("test").ToString());
		Assert.Equal("byte[3]", new MessagePackValue([1, 3, 5]).ToString());
		Assert.Equal("array[2]", new MessagePackValue(new MessagePackValue[] { 2, 4 }).ToString());
		Assert.Equal("map[1]", new MessagePackValue(new Dictionary<MessagePackValue, MessagePackValue> { [3] = true }).ToString());
		Assert.Equal("extension type -3", new MessagePackValue(new Extension(-3, new byte[] { 55 })).ToString());
		Assert.Equal("01/12/2025 01:01:01", new MessagePackValue(new DateTime(2025, 01, 12, 1, 1, 1, DateTimeKind.Utc)).ToString());
	}

	[Fact]
	public void Equality()
	{
		MessagePackValue left = 5, right = "hi";
		Assert.NotEqual(left, right);
	}
}
