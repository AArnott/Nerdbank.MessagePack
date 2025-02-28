// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// BSD 3-Clause License
// Copyright(c) 2024, serde.msgpack
using System.Buffers.Binary;
using System.ComponentModel;
using System.Text;

namespace Serde.MsgPack;

internal sealed partial class MsgPackWriter(ScratchBuffer outBuffer) : ISerializer
{
	private readonly ScratchBuffer @out = outBuffer;

	void ISerializer.SerializeBool(bool b)
	{
		this.@out.Add(b ? (byte)0xc3 : (byte)0xc2);
	}

	void ISerializer.SerializeByte(byte b) => this.SerializeU64(b);

	void ISerializer.SerializeChar(char c) => this.SerializeU64(c);

	ISerializeCollection ISerializer.SerializeCollection(ISerdeInfo typeInfo, int? length)
	{
		if (length is null)
		{
			throw new InvalidOperationException("Cannot serialize a collection with an unknown length.");
		}

		if (typeInfo.Kind == InfoKind.Enumerable)
		{
			if (length <= 15)
			{
				this.@out.Add((byte)(0x90 | length));
			}
			else if (length <= 0xffff)
			{
				this.@out.Add(0xdc);
				this.WriteBigEndian((ushort)length);
			}
			else
			{
				this.@out.Add(0xdd);
				this.WriteBigEndian((uint)length);
			}
		}
		else if (typeInfo.Kind == InfoKind.Dictionary)
		{
			if (length <= 15)
			{
				this.@out.Add((byte)(0x80 | length));
			}
			else if (length <= 0xffff)
			{
				this.@out.Add(0xde);
				this.WriteBigEndian((ushort)length);
			}
			else
			{
				this.@out.Add(0xdf);
				this.WriteBigEndian((uint)length);
			}
		}
		else
		{
			throw new InvalidOperationException("Expected a collection, found: " + typeInfo.Kind);
		}

		return this;
	}

	void ISerializer.SerializeDecimal(decimal d)
	{
		throw new NotImplementedException();
	}

	void ISerializer.SerializeDouble(double d)
	{
		this.@out.Add(0xcb);
		this.WriteBigEndian(d);
	}

	void ISerializer.SerializeEnumValue<T, U>(ISerdeInfo typeInfo, int index, T value, U serialize)
	{
		// Serialize the index of the enum member
		this.SerializeI64(index);
	}

	void ISerializer.SerializeFloat(float f)
	{
		this.@out.Add(0xca);
		this.WriteBigEndian(f);
	}

	void ISerializer.SerializeI16(short i16) => this.SerializeI64(i16);

	void ISerializer.SerializeI32(int i32) => this.SerializeI64(i32);

	void ISerializer.SerializeI64(long i64) => this.SerializeI64(i64);

	private void SerializeI64(long i64)
	{
		if (i64 >= 0)
		{
			this.SerializeU64((ulong)i64);
		}
		else if (i64 >= -32)
		{
			this.@out.Add((byte)(0xe0 | (i64 + 32)));
		}
		else if (i64 >= -128)
		{
			this.@out.Add(0xd0);
			this.@out.Add((byte)i64);
		}
		else if (i64 >= -32768)
		{
			this.@out.Add(0xd1);
			this.WriteBigEndian((short)i64);
		}
		else if (i64 >= -2147483648)
		{
			this.@out.Add(0xd2);
			this.WriteBigEndian((int)i64);
		}
		else
		{
			this.@out.Add(0xd3);
			this.WriteBigEndian(i64);
		}
	}

	void ISerializer.SerializeNull()
	{
		this.@out.Add(0xc0);
	}

	void ISerializer.SerializeSByte(sbyte b) => this.SerializeI64(b);

	void ISerializer.SerializeString(string s)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		if (bytes.Length <= 31)
		{
			this.@out.Add((byte)(0xa0 | bytes.Length));
		}
		else if (bytes.Length <= 0xff)
		{
			this.@out.Add(0xd9);
			this.@out.Add((byte)bytes.Length);
		}
		else if (bytes.Length <= 0xffff)
		{
			this.@out.Add(0xda);
			this.WriteBigEndian((ushort)bytes.Length);
		}
		else
		{
			this.@out.Add(0xdb);
			this.WriteBigEndian((uint)bytes.Length);
		}

		foreach (byte b in bytes)
		{
			this.@out.Add(b);
		}
	}

	ISerializeType ISerializer.SerializeType(ISerdeInfo typeInfo)
	{
		// Check that, if the members are marked with [Key], they are in order.
		// We do not support out-of-order keys.
		for (int i = 0; i < typeInfo.FieldCount; i++)
		{
			IList<System.Reflection.CustomAttributeData> attrs = typeInfo.GetFieldAttributes(i);
			foreach (System.Reflection.CustomAttributeData attr in attrs)
			{
				if (attr.AttributeType.FullName == "MessagePack.KeyAttribute")
				{
					if (attr.ConstructorArguments is [{ Value: int index }] && index != i)
					{
						throw new InvalidOperationException($"Found member {typeInfo.GetFieldStringName(i)} declared at index {i} but marked with [Key({index})]. Key indices must match declaration order.");
					}
				}
			}
		}

		// Write as an array, with the keys left implicit in the order
		if (typeInfo.FieldCount <= 15)
		{
			this.@out.Add((byte)(0x90 | typeInfo.FieldCount));
		}
		else if (typeInfo.FieldCount <= 0xffff)
		{
			this.@out.Add(0xdc);
			this.WriteBigEndian((ushort)typeInfo.FieldCount);
		}
		else
		{
			this.@out.Add(0xdd);
			this.WriteBigEndian((uint)typeInfo.FieldCount);
		}

		return this;
	}

	void ISerializer.SerializeU16(ushort u16) => this.SerializeU64(u16);

	void ISerializer.SerializeU32(uint u32) => this.SerializeU64(u32);

	void ISerializer.SerializeU64(ulong u64) => this.SerializeU64(u64);

	private void SerializeU64(ulong u64)
	{
		if (u64 <= 0x7f)
		{
			this.@out.Add((byte)u64);
		}
		else if (u64 <= 0xff)
		{
			this.@out.Add(0xcc);
			this.@out.Add((byte)u64);
		}
		else if (u64 <= 0xffff)
		{
			this.@out.Add(0xcd);
			this.WriteBigEndian((ushort)u64);
		}
		else if (u64 <= 0xffffffff)
		{
			this.@out.Add(0xce);
			this.WriteBigEndian((uint)u64);
		}
		else
		{
			this.@out.Add(0xcf);
			this.WriteBigEndian(u64);
		}
	}

	private void WriteBigEndian(ushort value)
	{
		this.@out.Add((byte)(value >> 8));
		this.@out.Add((byte)value);
	}

	private void WriteBigEndian(uint value)
	{
		this.@out.Add((byte)(value >> 24));
		this.@out.Add((byte)(value >> 16));
		this.@out.Add((byte)(value >> 8));
		this.@out.Add((byte)value);
	}

	private void WriteBigEndian(ulong value)
	{
		this.@out.Add((byte)(value >> 56));
		this.@out.Add((byte)(value >> 48));
		this.@out.Add((byte)(value >> 40));
		this.@out.Add((byte)(value >> 32));
		this.@out.Add((byte)(value >> 24));
		this.@out.Add((byte)(value >> 16));
		this.@out.Add((byte)(value >> 8));
		this.@out.Add((byte)value);
	}

	private void WriteBigEndian(short value) => this.WriteBigEndian((ushort)value);

	private void WriteBigEndian(int value) => this.WriteBigEndian((uint)value);

	private void WriteBigEndian(long value) => this.WriteBigEndian((ulong)value);

	private void WriteBigEndian(float f)
	{
		Span<byte> bytes = stackalloc byte[4];
		BinaryPrimitives.WriteSingleBigEndian(bytes, f);
		this.@out.AddRange(bytes);
	}

	private void WriteBigEndian(double d)
	{
		Span<byte> bytes = stackalloc byte[8];
		BinaryPrimitives.WriteDoubleBigEndian(bytes, d);
		this.@out.AddRange(bytes);
	}
}
