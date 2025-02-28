// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Serde.IO;

namespace Serde.MsgPack;

internal sealed partial class MsgPackReader<TReader> : IDeserializer
	where TReader : IBufReader
{
	private TReader reader;

	public MsgPackReader(TReader reader)
	{
		this.reader = reader;
		// start with a filled buffer
		this.reader.FillBuffer(1);
	}

	void IDisposable.Dispose()
	{ }

	[DoesNotReturn]
	private static void ThrowEof()
	{
		throw new Exception("Unexpected end of stream");
	}

	T IDeserializer.ReadAny<T>(IDeserializeVisitor<T> v)
	{
		throw new NotImplementedException();
	}

	bool IDeserializer.ReadBool() => this.ReadBool();

	private bool ReadBool()
	{
		byte b = this.EatByteOrThrow();
		if (b == 0xc2)
		{
			return false;
		}

		if (b == 0xc3)
		{
			return true;
		}

		throw new Exception($"Expected boolean, got 0x{b:x}");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private ReadOnlySpan<byte> RefillNoEof(int fillCount)
	{
		if (!this.reader.FillBuffer(fillCount))
		{
			ThrowEof();
		}

		return this.reader.Span;
	}

	private byte PeekByteOrThrow()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length == 0)
		{
			span = this.RefillNoEof(1);
		}

		return span[0];
	}

	private byte EatByteOrThrow()
	{
		byte result = this.PeekByteOrThrow();
		this.reader.Advance(1);
		return result;
	}

	/// <summary>
	/// Eats at least one byte from the buffer.
	/// </summary>
	bool TryReadByte(out byte result)
	{
		return this.TryReadByte(this.EatByteOrThrow(), out result);
	}

	bool TryReadByte(byte b, out byte result)
	{
		if (b <= 0x7f)
		{
			result = b;
			return true;
		}

		if (b == 0xcc)
		{
			result = this.EatByteOrThrow();
			return true;
		}

		result = b;
		return false;
	}

	byte IDeserializer.ReadByte() => this.ReadByte();

	private byte ReadByte()
	{
		if (!this.TryReadByte(out byte b))
		{
			throw new Exception($"Expected byte 0xcc, got 0x{b:x}");
		}

		return b;
	}

	char IDeserializer.ReadChar()
	{
		// char is encoded as either a 1-2 byte integer
		return (char)this.ReadU16();
	}

	IDeserializeCollection IDeserializer.ReadCollection(ISerdeInfo typeInfo)
	{
		byte b = this.EatByteOrThrow();
		if (typeInfo.Kind == InfoKind.Enumerable)
		{
			int length;
			if (b <= 0x9f)
			{
				length = b & 0xf;
			}
			else if (b == 0xdc)
			{
				length = this.ReadBigEndianU16();
			}
			else if (b == 0xdd)
			{
				length = (int)this.ReadBigEndianU32();
			}
			else
			{
				throw new Exception($"Expected array, got 0x{b:x}");
			}

			return new DeserializeCollection(this, false, length);
		}
		else if (typeInfo.Kind == InfoKind.Dictionary)
		{
			int length;
			if (b <= 0x8f)
			{
				length = b & 0xf;
			}
			else if (b == 0xde)
			{
				length = this.ReadBigEndianU16();
			}
			else if (b == 0xdf)
			{
				length = (int)this.ReadBigEndianU32();
			}
			else
			{
				throw new Exception($"Expected dictionary, got 0x{b:x}");
			}

			return new DeserializeCollection(this, true, length * 2);
		}
		else
		{
			throw new Exception("Expected collection");
		}
	}

	public decimal ReadDecimal()
	{
		throw new NotImplementedException();
	}

	private double ReadDouble()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < 9)
		{
			span = this.RefillNoEof(0);
		}

		byte b = span[0];
		if (b != 0xcb)
		{
			throw new Exception($"Expected 64-bit double, got 0x{b:x}");
		}

		double result = BinaryPrimitives.ReadDoubleBigEndian(span[1..]);
		this.reader.Advance(9);
		return result;
	}

	double IDeserializer.ReadDouble() => this.ReadDouble();

	private float ReadFloat()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < 5)
		{
			span = this.RefillNoEof(5);
		}

		byte b = span[0];
		if (b != 0xca)
		{
			throw new Exception($"Expected 32-bit float, got 0x{b:x}");
		}

		float result = BinaryPrimitives.ReadSingleBigEndian(span[1..]);
		this.reader.Advance(5);
		return result;
	}

	float IDeserializer.ReadFloat() => this.ReadFloat();

	private bool TryReadSbyte(out sbyte s)
	{
		byte first = this.EatByteOrThrow();
		if (first <= 0x7f || first >= 0xe0)
		{
			s = (sbyte)first;
			return true;
		}

		if (first == 0xd0)
		{
			s = (sbyte)this.EatByteOrThrow();
			return true;
		}

		s = (sbyte)first;
		return false;
	}

	private bool TryReadI16(out short i16)
	{
		if (this.TryReadSbyte(out sbyte sb))
		{
			i16 = sb;
			return true;
		}

		if (this.TryReadByte((byte)sb, out byte b))
		{
			i16 = b;
			return true;
		}

		if (b == 0xd1)
		{
			i16 = (short)this.ReadBigEndianU16();
			return true;
		}

		i16 = b;
		return false;
	}

	public short ReadI16()
	{
		if (!this.TryReadI16(out short i16))
		{
			throw new Exception("Expected 16-bit integer");
		}

		return i16;
	}

	private bool TryReadI32(out int i32)
	{
		if (this.TryReadI16(out short i16))
		{
			i32 = i16;
			return true;
		}

		if (i16 == 0xcd)
		{
			i32 = this.ReadBigEndianU16();
			return true;
		}

		if (i16 == 0xd2)
		{
			i32 = (int)this.ReadBigEndianU32();
			return true;
		}

		i32 = i16;
		return false;
	}

	private int ReadI32()
	{
		if (!this.TryReadI32(out int i32))
		{
			throw new Exception("Expected 32-bit integer");
		}

		return i32;
	}

	int IDeserializer.ReadI32() => this.ReadI32();

	private bool TryReadI64(out long i64)
	{
		if (this.TryReadI32(out int i32))
		{
			i64 = i32;
			return true;
		}

		if (i32 == 0xce)
		{
			i64 = this.ReadBigEndianU32();
			return true;
		}

		if (i32 == 0xd3)
		{
			i64 = (long)this.ReadBigEndianU64();
			return true;
		}

		i64 = i32;
		return false;
	}

	private long ReadI64()
	{
		if (!this.TryReadI64(out long i64))
		{
			throw new Exception("Expected 64-bit integer");
		}

		return i64;
	}

	long IDeserializer.ReadI64() => this.ReadI64();

	T? IDeserializer.ReadNullableRef<T, TProxy>(TProxy proxy)
		where T : class
	{
		byte b = this.PeekByteOrThrow();
		if (b == 0xc0)
		{
			this.reader.Advance(1);
			return null;
		}

		return proxy.Deserialize(this);
	}

	public sbyte ReadSByte()
	{
		if (!this.TryReadSbyte(out sbyte sb))
		{
			throw new Exception("Expected signed byte");
		}

		return sb;
	}

	string IDeserializer.ReadString() => this.ReadString();

	private string ReadString()
	{
		// strings are encoded in UTF8 as byte arrays
		byte b = this.EatByteOrThrow();
		int length;
		if (b <= 0xbf)
		{
			length = b & 0x1f;
		}
		else if (b == 0xd9)
		{
			// 8-bit length
			length = this.EatByteOrThrow();
		}
		else if (b == 0xda)
		{
			// 16-bit length
			length = this.ReadBigEndianU16();
		}
		else if (b == 0xdb)
		{
			// 32-bit length
			length = (int)this.ReadBigEndianU32();
		}
		else
		{
			throw new Exception($"Expected string, got 0x{b:x}");
		}

		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < length)
		{
			span = this.RefillNoEof(length);
		}

		string str = Encoding.UTF8.GetString(span[..length]);
		this.reader.Advance(length);
		return str;
	}

	IDeserializeType IDeserializer.ReadType(ISerdeInfo typeInfo)
	{
		// Types are just an array of fields
		if (typeInfo.Kind == InfoKind.CustomType)
		{
			int fieldCount = typeInfo.FieldCount;
			byte b = this.EatByteOrThrow();
			int length;
			if (fieldCount <= 0xff)
			{
				if (b > 0x9f)
				{
					throw new Exception($"Expected array, got 0x{b:x}");
				}

				length = b & 0xf;
			}
			else if (fieldCount <= 0xffff)
			{
				if (b != 0xdc)
				{
					throw new Exception($"Expected 16-bit array, got 0x{b:x}");
				}

				length = this.ReadBigEndianU16();
			}
			else
			{
				if (b != 0xdd)
				{
					throw new Exception($"Expected 32-bit array, got 0x{b:x}");
				}

				length = (int)this.ReadBigEndianU32();
			}

			if (length != fieldCount)
			{
				throw new Exception($"Expected array of length {fieldCount}, got {length}");
			}

			return new DeserializeType(this);
		}
		else if (typeInfo.Kind == InfoKind.Enum)
		{
			return new DeserializeType(this);
		}

		throw new Exception("Expected custom type or enum");
	}

	private ushort ReadU16()
	{
		if (this.TryReadU16(out ushort u16))
		{
			return u16;
		}

		throw new Exception("Expected integer");
	}

	ushort IDeserializer.ReadU16() => this.ReadU16();

	private bool TryReadU16(out ushort u16)
	{
		if (this.TryReadByte(out byte b))
		{
			u16 = b;
			return true;
		}

		if (b == 0xcd)
		{
			u16 = this.ReadBigEndianU16();
			return true;
		}

		u16 = b;
		return false;
	}

	private bool TryReadU32(out uint u32)
	{
		if (this.TryReadU16(out ushort u16))
		{
			u32 = u16;
			return true;
		}

		// u16 contains the first unexpected byte
		if (u16 == 0xce)
		{
			u32 = this.ReadBigEndianU32();
			return true;
		}

		u32 = u16;
		return false;
	}

	private bool TryReadU64(out ulong u64)
	{
		if (this.TryReadU32(out uint u32))
		{
			u64 = u32;
			return true;
		}

		// u32 contains the first unexpected byte
		if (u32 == 0xcf)
		{
			u64 = this.ReadBigEndianU64();
			return true;
		}

		u64 = u32;
		return false;
	}

	public uint ReadU32()
	{
		if (!this.TryReadU32(out uint u32))
		{
			throw new Exception($"Expected integer, got 0x{u32:x}");
		}

		return u32;
	}

	public ulong ReadU64()
	{
		if (!this.TryReadU64(out ulong u64))
		{
			throw new Exception($"Expected integer, got 0x{u64:x}");
		}

		return u64;
	}

	private ushort ReadBigEndianU16()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < 2)
		{
			span = this.RefillNoEof(2);
		}

		ushort result = BinaryPrimitives.ReadUInt16BigEndian(span);
		this.reader.Advance(2);
		return result;
	}

	private uint ReadBigEndianU32()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < 4)
		{
			span = this.RefillNoEof(4);
		}

		uint result = BinaryPrimitives.ReadUInt32BigEndian(span);
		this.reader.Advance(4);
		return result;
	}

	private ulong ReadBigEndianU64()
	{
		ReadOnlySpan<byte> span = this.reader.Span;
		if (span.Length < 8)
		{
			span = this.RefillNoEof(8);
		}

		ulong result = BinaryPrimitives.ReadUInt64BigEndian(span);
		this.reader.Advance(8);
		return result;
	}
}
