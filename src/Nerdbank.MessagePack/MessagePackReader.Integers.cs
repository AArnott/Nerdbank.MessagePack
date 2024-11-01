// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Simplify type syntax
#pragma warning disable SA1601 // Partial elements should be documented

namespace Nerdbank.MessagePack;

public ref partial struct MessagePackReader
{
	/// <summary>
	/// Reads an <see cref="Byte"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public Byte ReadByte()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (Byte)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((Byte)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((Byte)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((Byte)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((Byte)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((Byte)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((Byte)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((Byte)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((Byte)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((Byte)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt16"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public UInt16 ReadUInt16()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (UInt16)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((UInt16)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((UInt16)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((UInt16)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((UInt16)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((UInt16)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((UInt16)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((UInt16)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((UInt16)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((UInt16)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt32"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public UInt32 ReadUInt32()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (UInt32)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((UInt32)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((UInt32)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((UInt32)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((UInt32)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((UInt32)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((UInt32)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((UInt32)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((UInt32)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((UInt32)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt64"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public UInt64 ReadUInt64()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (UInt64)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((UInt64)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((UInt64)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((UInt64)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((UInt64)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((UInt64)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((UInt64)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((UInt64)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((UInt64)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((UInt64)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="SByte"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public SByte ReadSByte()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (SByte)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((SByte)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((SByte)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((SByte)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((SByte)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((SByte)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((SByte)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((SByte)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((SByte)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((SByte)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="Int16"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public Int16 ReadInt16()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (Int16)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((Int16)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((Int16)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((Int16)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((Int16)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((Int16)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((Int16)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((Int16)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((Int16)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((Int16)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="Int32"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public Int32 ReadInt32()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (Int32)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((Int32)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((Int32)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((Int32)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((Int32)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((Int32)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((Int32)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((Int32)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((Int32)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((Int32)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Reads an <see cref="Int64"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <returns>The value.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public Int64 ReadInt64()
	{
		ThrowInsufficientBufferUnless(this.reader.TryRead(out byte code));

		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x):
				return (Int64)code;
			case byte x when MessagePackCode.IsNegativeFixInt(x):
				return checked((Int64)unchecked((sbyte)code));
			case MessagePackCode.UInt8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out byte byteResult));
				return checked((Int64)byteResult);
			case MessagePackCode.Int8:
				ThrowInsufficientBufferUnless(this.reader.TryRead(out sbyte sbyteResult));
				return checked((Int64)sbyteResult);
			case MessagePackCode.UInt16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ushort ushortResult));
				return checked((Int64)ushortResult);
			case MessagePackCode.Int16:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out short shortResult));
				return checked((Int64)shortResult);
			case MessagePackCode.UInt32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out uint uintResult));
				return checked((Int64)uintResult);
			case MessagePackCode.Int32:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out int intResult));
				return checked((Int64)intResult);
			case MessagePackCode.UInt64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out ulong ulongResult));
				return checked((Int64)ulongResult);
			case MessagePackCode.Int64:
				ThrowInsufficientBufferUnless(this.reader.TryReadBigEndian(out long longResult));
				return checked((Int64)longResult);
			default:
				throw ThrowInvalidCode(code);
		}
	}
}
