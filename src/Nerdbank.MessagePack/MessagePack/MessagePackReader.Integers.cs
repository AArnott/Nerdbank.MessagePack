// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Simplify type syntax
#pragma warning disable SA1601 // Partial elements should be documented

using Nerdbank.PolySerializer.Converters;

namespace Nerdbank.PolySerializer.MessagePack;

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
		switch (this.streamingReader.TryRead(out Byte value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out UInt16 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out UInt32 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out UInt64 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out SByte value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out Int16 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out Int32 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
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
		switch (this.streamingReader.TryRead(out Int64 value))
		{
			case DecodeResult.Success:
				return value;
			case DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case DecodeResult.EmptyBuffer:
			case DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}
}
