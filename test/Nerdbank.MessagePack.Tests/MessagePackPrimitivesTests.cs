// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class MessagePackPrimitivesTests
{
	[Theory]
	[InlineData(-32, 0xe0)]
	[InlineData(-1, 0xff)]
	[InlineData(0, 0x00)]
	[InlineData(127, 0x7f)]
	public void TryWriteInt32FixInt(int value, int expectedCode)
	{
		Span<byte> encoded = stackalloc byte[1];
		Assert.True(MessagePackPrimitives.TryWrite(encoded, value, out int bytesWritten));
		Assert.Equal((byte)expectedCode, encoded[0]);
		Assert.Equal(1, bytesWritten);
	}

	[Theory]
	[InlineData(0x00, 0)]
	[InlineData(0x7f, 127)]
	[InlineData(0xe0, -32)]
	[InlineData(0xff, -1)]
	public void TryReadInt32FixInt(int code, int expected)
	{
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryRead([(byte)code], out int value, out int tokenSize));
		Assert.Equal(expected, value);
		Assert.Equal(1, tokenSize);
	}

	[Theory]
	[InlineData(MessagePackCode.UInt32, 0x7f, 0xff, 0xff, 0xff, int.MaxValue)]
	[InlineData(MessagePackCode.Int32, 0x80, 0x00, 0x00, 0x00, int.MinValue)]
	public void TryReadInt32Payload(int code, int byte1, int byte2, int byte3, int byte4, int expected)
	{
		byte[] encoded = [(byte)code, (byte)byte1, (byte)byte2, (byte)byte3, (byte)byte4];
		Assert.Equal(MessagePackPrimitives.DecodeResult.Success, MessagePackPrimitives.TryRead(encoded, out int value, out int tokenSize));
		Assert.Equal(expected, value);
		Assert.Equal(5, tokenSize);
	}

	[Fact]
	public void TryReadInt32InsufficientPayload()
	{
		byte[] encoded = [MessagePackCode.Int32, 0x00, 0x00, 0x00];
		Assert.Equal(MessagePackPrimitives.DecodeResult.InsufficientBuffer, MessagePackPrimitives.TryRead(encoded, out int value, out int tokenSize));
		Assert.Equal(0, value);
		Assert.Equal(5, tokenSize);
	}

	[Fact]
	public void TryReadInt32UnsignedOverflow()
	{
		byte[] encoded = [MessagePackCode.UInt32, 0x80, 0x00, 0x00, 0x00];
		Assert.Throws<OverflowException>(() => MessagePackPrimitives.TryRead(encoded, out int _, out int _));
	}
}
