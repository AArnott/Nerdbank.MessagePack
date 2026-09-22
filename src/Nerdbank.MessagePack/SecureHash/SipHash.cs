// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//// Originally from: https://github.com/paya-cz/siphash
//// Author:          Pavel Werl
//// License:         Public Domain
//// SipHash website: https://131002.net/siphash/

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Nerdbank.MessagePack.SecureHash;

/// <summary>
/// Implements the <see href="https://en.wikipedia.org/wiki/SipHash">SipHash pseudo-random function</see>.
/// </summary>
/// <remarks>
/// This class is immutable and thread-safe.
/// </remarks>
internal class SipHash
{
	/// <summary>
	/// A shareable implementation initialized with a random key.
	/// </summary>
	internal static readonly SipHash Default = new();

	/// <summary>
	/// Part of the initial 256-bit internal state.
	/// </summary>
	private readonly ulong initialState0;

	/// <summary>
	/// Part of the initial 256-bit internal state.
	/// </summary>
	private readonly ulong initialState1;

	/// <summary>Initializes a new instance of the <see cref="SipHash"/> class using a random key.</summary>
	public SipHash()
	{
		using var rng = RandomNumberGenerator.Create();
#if NET
		Span<byte> key = stackalloc byte[16];
		rng.GetBytes(key);
#else
		byte[] buffer = ArrayPool<byte>.Shared.Rent(16);
		rng.GetBytes(buffer, 0, 16);
		Span<byte> key = buffer;
#endif

		this.initialState0 = 0x736f6d6570736575UL ^ BinaryPrimitives.ReadUInt64LittleEndian(key);
		this.initialState1 = 0x646f72616e646f6dUL ^ BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(sizeof(ulong)));

#if !NET
		ArrayPool<byte>.Shared.Return(buffer);
#endif
	}

	/// <summary>Initializes a new instance of the <see cref="SipHash"/> class using the specified 128-bit key.</summary>
	/// <param name="key">Key for the SipHash pseudo-random function. Must be exactly 16 bytes long.</param>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is not exactly 16 bytes long (128 bits).</exception>
	public SipHash(ReadOnlySpan<byte> key)
	{
		if (key.Length != 16)
		{
			throw new ArgumentException("SipHash key must be exactly 128-bit long (16 bytes).", nameof(key));
		}

		this.initialState0 = 0x736f6d6570736575UL ^ BinaryPrimitives.ReadUInt64LittleEndian(key);
		this.initialState1 = 0x646f72616e646f6dUL ^ BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(sizeof(ulong)));
	}

	/// <summary>
	/// Gets the 128-bit SipHash key used to construct this instance.
	/// </summary>
	/// <param name="key">The 16-byte buffer that receives the key originally provided to the constructor.</param>
	public void GetKey(Span<byte> key)
	{
		if (key.Length != 16)
		{
			throw new ArgumentException("SipHash key must be exactly 128-bit long (16 bytes).", nameof(key));
		}

		BinaryPrimitives.WriteUInt64LittleEndian(key, this.initialState0 ^ 0x736f6d6570736575UL);
		BinaryPrimitives.WriteUInt64LittleEndian(key.Slice(sizeof(ulong)), this.initialState1 ^ 0x646f72616e646f6dUL);
	}

	/// <summary>Computes 64-bit SipHash tag for the specified message.</summary>
	/// <param name="data">The byte array for which to compute a SipHash tag.</param>
	/// <returns>Returns 64-bit (8 bytes) SipHash tag.</returns>
	public long Compute(scoped ReadOnlySpan<byte> data)
	{
		IncrementalHasher hasher = this.CreateIncrementalHasher();
		hasher.Append(data);
		return hasher.FinalizeHash();
	}

	/// <summary>Computes 64-bit SipHash tag for the specified sequence.</summary>
	/// <param name="data">The byte sequence for which to compute a SipHash tag.</param>
	/// <returns>Returns 64-bit (8 bytes) SipHash tag.</returns>
	public long Compute(scoped in ReadOnlySequence<byte> data)
	{
		if (data.IsSingleSegment)
		{
			return this.Compute(data.First.Span);
		}

		IncrementalHasher hasher = this.CreateIncrementalHasher();
		foreach (ReadOnlyMemory<byte> segment in data)
		{
			hasher.Append(segment.Span);
		}

		return hasher.FinalizeHash();
	}

	/// <summary>
	/// Creates a stateful hasher that can accept data in multiple segments.
	/// </summary>
	/// <returns>An incremental SipHash hasher.</returns>
	internal IncrementalHasher CreateIncrementalHasher() => new(this.initialState0, this.initialState1);

	private static void ProcessMessageBlock(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3, ulong block)
	{
		unchecked
		{
			v3 ^= block;
			SipRound(ref v0, ref v1, ref v2, ref v3);
			SipRound(ref v0, ref v1, ref v2, ref v3);
			v0 ^= block;
		}
	}

	private static void ProcessFinalBlock(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3, ulong block)
	{
		unchecked
		{
			v3 ^= block;
			SipRound(ref v0, ref v1, ref v2, ref v3);
			SipRound(ref v0, ref v1, ref v2, ref v3);
			v0 ^= block;
			v2 ^= 0xff;
		}
	}

	private static void FinalizeCore(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
	{
		unchecked
		{
			SipRound(ref v0, ref v1, ref v2, ref v3);
			SipRound(ref v0, ref v1, ref v2, ref v3);
			SipRound(ref v0, ref v1, ref v2, ref v3);
			SipRound(ref v0, ref v1, ref v2, ref v3);
		}
	}

	private static void SipRound(ref ulong v0, ref ulong v1, ref ulong v2, ref ulong v3)
	{
		unchecked
		{
			v0 += v1;
			v2 += v3;
			v1 = (v1 << 13) | (v1 >> 51);
			v3 = (v3 << 16) | (v3 >> 48);
			v1 ^= v0;
			v3 ^= v2;
			v0 = (v0 << 32) | (v0 >> 32);
			v2 += v1;
			v0 += v3;
			v1 = (v1 << 17) | (v1 >> 47);
			v3 = (v3 << 21) | (v3 >> 43);
			v1 ^= v2;
			v3 ^= v0;
			v2 = (v2 << 32) | (v2 >> 32);
		}
	}

	/// <summary>
	/// Incrementally computes a SipHash value over multiple segments.
	/// </summary>
	internal struct IncrementalHasher
	{
		private ulong v0;
		private ulong v1;
		private ulong v2;
		private ulong v3;
		private ulong tailBlock;
		private int tailLength;
		private ulong totalLength;

		/// <summary>
		/// Initializes a new instance of the <see cref="IncrementalHasher"/> struct.
		/// </summary>
		/// <param name="initialState0">The first half of the keyed initial state.</param>
		/// <param name="initialState1">The second half of the keyed initial state.</param>
		internal IncrementalHasher(ulong initialState0, ulong initialState1)
		{
			this.v0 = initialState0;
			this.v1 = initialState1;
			this.v2 = 0x1F160A001E161714UL ^ initialState0;
			this.v3 = 0x100A160317100A1EUL ^ initialState1;
			this.tailBlock = 0;
			this.tailLength = 0;
			this.totalLength = 0;
		}

		/// <summary>
		/// Appends data to the hash computation.
		/// </summary>
		/// <param name="data">The data to hash.</param>
		internal void Append(ReadOnlySpan<byte> data)
		{
			this.totalLength += (ulong)data.Length;

			if (this.tailLength > 0)
			{
				int bytesNeeded = sizeof(ulong) - this.tailLength;
				int bytesToCopy = Math.Min(bytesNeeded, data.Length);
				for (int i = 0; i < bytesToCopy; i++)
				{
					this.tailBlock |= (ulong)data[i] << ((this.tailLength + i) * 8);
				}

				this.tailLength += bytesToCopy;
				data = data[bytesToCopy..];

				if (this.tailLength < sizeof(ulong))
				{
					return;
				}

				if (this.tailLength == sizeof(ulong))
				{
					ProcessMessageBlock(ref this.v0, ref this.v1, ref this.v2, ref this.v3, this.tailBlock);
					this.tailBlock = 0;
					this.tailLength = 0;
				}
			}

			int finalBlockPosition = data.Length & ~7;
			for (int blockPosition = 0; blockPosition < finalBlockPosition; blockPosition += sizeof(ulong))
			{
				ProcessMessageBlock(ref this.v0, ref this.v1, ref this.v2, ref this.v3, BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(blockPosition)));
			}

			ReadOnlySpan<byte> tail = data[finalBlockPosition..];
			for (int i = 0; i < tail.Length; i++)
			{
				this.tailBlock |= (ulong)tail[i] << (i * 8);
			}

			this.tailLength = tail.Length;
		}

		/// <summary>
		/// Computes the hash for all data appended so far.
		/// </summary>
		/// <returns>The 64-bit SipHash tag.</returns>
		internal long FinalizeHash()
		{
			unchecked
			{
				ulong v0 = this.v0;
				ulong v1 = this.v1;
				ulong v2 = this.v2;
				ulong v3 = this.v3;
				ulong finalBlock = this.tailBlock | ((this.totalLength & 0xFFUL) << 56);

				ProcessFinalBlock(ref v0, ref v1, ref v2, ref v3, finalBlock);
				FinalizeCore(ref v0, ref v1, ref v2, ref v3);
				return (long)(v0 ^ v1 ^ v2 ^ v3);
			}
		}
	}
}
