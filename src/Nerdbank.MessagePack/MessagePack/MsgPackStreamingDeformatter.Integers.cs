// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

#pragma warning disable SA1121 // Simplify type syntax
#pragma warning disable SA1601 // Partial elements should be documented

namespace Nerdbank.PolySerializer.MessagePack;

partial record MsgPackStreamingDeformatter
{
	/// <summary>
	/// Reads an <see cref="Byte"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out Byte value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref Byte value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt16"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out UInt16 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref UInt16 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt32"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out UInt32 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref UInt32 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="UInt64"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out UInt64 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref UInt64 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="SByte"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out SByte value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref SByte value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="Int16"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out Int16 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref Int16 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="Int32"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out Int32 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref Int32 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Reads an <see cref="Int64"/> value from:
	/// Some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// Some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>,
	/// or any of the other MsgPack integer types.
	/// </summary>
	/// <param name="value">Receives the decoded value.</param>
	/// <returns>The success or error code.</returns>
	/// <exception cref="OverflowException">Thrown when the value exceeds what can be stored in the returned type.</exception>
	public override DecodeResult TryRead(ref Reader reader, out Int64 value)
	{
		DecodeResult readResult = MessagePackPrimitives.TryRead(reader.UnreadSpan, out value, out int tokenSize);
		if (readResult == DecodeResult.Success)
		{
			this.Advance(ref reader, tokenSize);
			return DecodeResult.Success;
		}

		return SlowPath(ref reader, this, readResult, ref value, ref tokenSize);

		static DecodeResult SlowPath(ref Reader reader, MsgPackStreamingDeformatter self, DecodeResult readResult, ref Int64 value, ref int tokenSize)
		{
			switch (readResult)
			{
				case DecodeResult.Success:
					self.Advance(ref reader, tokenSize);
					return DecodeResult.Success;
				case DecodeResult.TokenMismatch:
					return DecodeResult.TokenMismatch;
				case DecodeResult.EmptyBuffer:
				case DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (reader.SequenceReader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryRead(buffer, out value, out tokenSize);
						return SlowPath(ref reader, self, readResult, ref value, ref tokenSize);
					}
					else
					{
						return self.InsufficientBytes(reader);
					}

				default:
					return ThrowUnreachable();
			}
		}
	}
}
