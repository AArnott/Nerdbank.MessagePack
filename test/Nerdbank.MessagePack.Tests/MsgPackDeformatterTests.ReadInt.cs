﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Use built-in type alias

using System.Numerics;

public partial class MsgPackDeformatterTests
{
	private const sbyte MinNegativeFixInt = unchecked((sbyte)MessagePackCode.MinNegativeFixInt);
	private const sbyte MaxNegativeFixInt = unchecked((sbyte)MessagePackCode.MaxNegativeFixInt);

	private readonly IReadOnlyList<(BigInteger Value, ReadOnlySequence<byte> Encoded)> integersOfInterest = new List<(BigInteger Value, ReadOnlySequence<byte> Encoded)>
	{
		// * FixInt
		// ** non-boundary
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 3))),

		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -3))),

		// ** Boundary conditions
		/* MaxFixInt */
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, MessagePackCode.MaxFixInt))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, checked((Byte)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, checked((UInt16)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, checked((UInt32)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, checked((UInt64)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, checked((SByte)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, checked((Int16)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, checked((Int32)MessagePackCode.MaxFixInt)))),
		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, checked((Int64)MessagePackCode.MaxFixInt)))),
		/* MinFixInt */
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, MessagePackCode.MinFixInt))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, checked((Byte)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, checked((UInt16)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, checked((UInt32)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, checked((UInt64)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, checked((SByte)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, checked((Int16)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, checked((Int32)MessagePackCode.MinFixInt)))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, checked((Int64)MessagePackCode.MinFixInt)))),
		/* MinNegativeFixInt */
		(MinNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, MinNegativeFixInt))),
		(MinNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, MinNegativeFixInt))),
		(MinNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MinNegativeFixInt))),
		(MinNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, MinNegativeFixInt))),
		/* MaxNegativeFixInt */
		(MaxNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, MaxNegativeFixInt))),
		(MaxNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, MaxNegativeFixInt))),
		(MaxNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MaxNegativeFixInt))),
		(MaxNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, MaxNegativeFixInt))),

		(MessagePackCode.MaxFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MessagePackCode.MaxFixInt))),
		(MessagePackCode.MinFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MessagePackCode.MinFixInt))),
		(MaxNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MaxNegativeFixInt))),
		(MinNegativeFixInt, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, MinNegativeFixInt))),

		// * Encoded as each type of at least 8 bits
		// ** Small positive value
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 3))),
		(3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 3))),

		// ** Small negative value
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, -3))),
		(-3, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -3))),

		// ** Max values
		/* Positive */
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 255))),
		(0x0ff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 255))),
		(0x0ffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 65535))),
		(0x0ffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 65535))),
		(0x0ffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 65535))),
		(0x0ffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 65535))),
		(0x0ffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 65535))),
		(0x0ffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 4294967295))),
		(0x0ffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 4294967295))),
		(0x0ffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 4294967295))),
		(0x0ffffffffffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 18446744073709551615))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteByte(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 127))),
		(0x7f, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 127))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt16(ref w, 32767))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 32767))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 32767))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, 32767))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 32767))),
		(0x7fff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 32767))),
		(0x7fffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt32(ref w, 2147483647))),
		(0x7fffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 2147483647))),
		(0x7fffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, 2147483647))),
		(0x7fffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 2147483647))),
		(0x7fffffffffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteUInt64(ref w, 9223372036854775807))),
		(0x7fffffffffffffff, Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, 9223372036854775807))),
		/* Negative */
		(unchecked((SByte)0x80), Encode((ref Writer w) => MsgPackFormatter.Default.WriteSByte(ref w, -128))),
		(unchecked((SByte)0x80), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, -128))),
		(unchecked((SByte)0x80), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, -128))),
		(unchecked((SByte)0x80), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -128))),
		(unchecked((Int16)0x8000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt16(ref w, -32768))),
		(unchecked((Int16)0x8000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, -32768))),
		(unchecked((Int16)0x8000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -32768))),
		(unchecked((Int32)0x80000000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt32(ref w, -2147483648))),
		(unchecked((Int32)0x80000000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -2147483648))),
		(unchecked((Int64)0x8000000000000000), Encode((ref Writer w) => MsgPackFormatter.Default.WriteInt64(ref w, -9223372036854775808))),
	};

	[Fact]
	public void ReadByte_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= Byte.MaxValue && value >= Byte.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadByte(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadByte(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadByte_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadByte(ref reader);
		});
	}

	[Fact]
	public void ReadUInt16_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= UInt16.MaxValue && value >= UInt16.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadUInt16(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadUInt16(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadUInt16_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadUInt16(ref reader);
		});
	}

	[Fact]
	public void ReadUInt32_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= UInt32.MaxValue && value >= UInt32.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadUInt32(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadUInt32(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadUInt32_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadUInt32(ref reader);
		});
	}

	[Fact]
	public void ReadUInt64_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= UInt64.MaxValue && value >= UInt64.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadUInt64(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadUInt64(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadUInt64_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadUInt64(ref reader);
		});
	}

	[Fact]
	public void ReadSByte_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= SByte.MaxValue && value >= SByte.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadSByte(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadSByte(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadSByte_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadSByte(ref reader);
		});
	}

	[Fact]
	public void ReadInt16_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= Int16.MaxValue && value >= Int16.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadInt16(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadInt16(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadInt16_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadInt16(ref reader);
		});
	}

	[Fact]
	public void ReadInt32_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= Int32.MaxValue && value >= Int32.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadInt32(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadInt32(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadInt32_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadInt32(ref reader);
		});
	}

	[Fact]
	public void ReadInt64_ReadVariousLengthsAndMagnitudes()
	{
		foreach ((BigInteger value, ReadOnlySequence<byte> encoded) in this.integersOfInterest)
		{
			this.logger.WriteLine("Decoding 0x{0:x} from {1}", value, MessagePackCode.ToFormatName(encoded.First.Span[0]));
			if (value <= Int64.MaxValue && value >= Int64.MinValue)
			{
				Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
				Assert.Equal(value, MsgPackDeformatter.Default.ReadInt64(ref reader));
			}
			else
			{
#if !ENABLE_IL2CPP
				Assert.Throws<OverflowException>(delegate
				{
					Reader reader = new Reader(encoded, MsgPackDeformatter.Default);
					MsgPackDeformatter.Default.ReadInt64(ref reader);
				});
#endif
			}
		}
	}

	[Fact]
	public void ReadInt64_ThrowsOnUnexpectedCode()
	{
		Assert.Throws<SerializationException>(delegate
		{
			Reader reader = new Reader(StringEncodedAsFixStr, MsgPackDeformatter.Default);
			MsgPackDeformatter.Default.ReadInt64(ref reader);
		});
	}
}
