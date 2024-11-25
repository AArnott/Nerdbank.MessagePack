// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Microsoft;
using DecodeResult = Nerdbank.MessagePack.MessagePackPrimitives.DecodeResult;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Hardware-accelerated converters for arrays of various primitive types.
/// </summary>
internal static class HardwareAccelerated
{
	/// <summary>
	/// Creates a hardware-accelerated converter if one is available for the given enumerable and element type.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of element.</typeparam>
	/// <param name="converter">Receives the hardware-accelerated converter if one is available.</param>
	/// <returns>A value indicating whether a converter is available.</returns>
	internal static bool TryGetConverter<TEnumerable, TElement>([NotNullWhen(true)] out MessagePackConverter<TEnumerable>? converter)
	{
		Type enumerableType = typeof(TEnumerable);
		SpanConstructorKind spanConstructorKind;
		if (enumerableType == typeof(TElement[]))
		{
			spanConstructorKind = SpanConstructorKind.Array;
		}
		else if (enumerableType == typeof(List<TElement>))
		{
			spanConstructorKind = SpanConstructorKind.List;
		}
		else if (enumerableType == typeof(ReadOnlyMemory<TElement>))
		{
			spanConstructorKind = SpanConstructorKind.ReadOnlyMemory;
		}
		else if (enumerableType == typeof(Memory<TElement>))
		{
			spanConstructorKind = SpanConstructorKind.Memory;
		}
		else
		{
			goto FAIL;
		}

		if (typeof(TElement) == typeof(bool))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, bool>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(sbyte))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, sbyte>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(short))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, short>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(int))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, int>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(long))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, long>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(ushort))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, ushort>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(uint))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, uint>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(ulong))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, ulong>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(float))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, float>(spanConstructorKind);
			return true;
		}
		else if (typeof(TElement) == typeof(double))
		{
			converter = new PrimitiveArrayConverter<TEnumerable, double>(spanConstructorKind);
			return true;
		}

FAIL:
		converter = null;
		return false;
	}

	private static ref TElement TryGetReferenceAndLength<TEnumerable, TElement>(TEnumerable enumerable, out int length)
	{
		switch (enumerable)
		{
			case TElement[] array:
				length = array.Length;
				return ref MemoryMarshal.GetArrayDataReference(array);
			case List<TElement> list:
				length = list.Count;
				return ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(list));
			case ReadOnlyMemory<TElement> rom:
				length = rom.Length;
				return ref MemoryMarshal.GetReference(rom.Span);
			case Memory<TElement> mem:
				length = mem.Length;
				return ref MemoryMarshal.GetReference(mem.Span);
		}

		Unsafe.SkipInit(out length);
		return ref Unsafe.NullRef<TElement>();
	}

	/// <summary>
	/// Read/Write methods with Hardware Intrinsics for primitive msgpack codes.
	/// </summary>
	private static class MessagePackPrimitiveSpanUtility
	{
		internal static unsafe T1 Interpret<T0, T1>(ref byte input, int offset)
			where T0 : unmanaged
			where T1 : unmanaged
		{
			T0 value = sizeof(T0) == 1 ? Unsafe.BitCast<byte, T0>(Unsafe.Add(ref input, offset)) : Unsafe.ReadUnaligned<T0>(ref Unsafe.Add(ref input, offset));
			if (BitConverter.IsLittleEndian)
			{
				if (typeof(T0) == typeof(short) || typeof(T0) == typeof(ushort))
				{
					value = Unsafe.BitCast<short, T0>(BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<T0, short>(value)));
				}
				else if (typeof(T0) == typeof(int) || typeof(T0) == typeof(uint) || typeof(T0) == typeof(float))
				{
					value = Unsafe.BitCast<int, T0>(BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<T0, int>(value)));
				}
				else if (typeof(T0) == typeof(long) || typeof(T0) == typeof(ulong) || typeof(T0) == typeof(double))
				{
					value = Unsafe.BitCast<long, T0>(BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<T0, long>(value)));
				}
			}

			if (typeof(T0) == typeof(T1))
			{
				return Unsafe.BitCast<T0, T1>(value);
			}

			if (typeof(T1) == typeof(sbyte))
			{
				if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<sbyte, T1>((sbyte)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(short))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<short, T1>(Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<short, T1>((short)Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<short, T1>((short)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<short, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<short, T1>((short)Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<short, T1>((short)Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<short, T1>((short)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(int))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<int, T1>(Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<int, T1>(Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<int, T1>((int)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<int, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<int, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<int, T1>((int)Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<int, T1>((int)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(long))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<long, T1>(Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<long, T1>((long)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(ushort))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<ushort, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<ushort, T1>((ushort)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(uint))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<uint, T1>((uint)Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<uint, T1>((uint)Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<uint, T1>((uint)Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<uint, T1>((uint)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<uint, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<uint, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<uint, T1>((uint)Unsafe.BitCast<T0, ulong>(value));
				}
			}
			else if (typeof(T1) == typeof(ulong))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<ulong, T1>((ulong)Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<ulong, T1>((ulong)Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<ulong, T1>((ulong)Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<ulong, T1>((ulong)Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<ulong, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<ulong, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<ulong, T1>(Unsafe.BitCast<T0, uint>(value));
				}
			}
			else if (typeof(T1) == typeof(float))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<float, T1>(Unsafe.BitCast<T0, ulong>(value));
				}
				else if (typeof(T0) == typeof(double))
				{
					return Unsafe.BitCast<float, T1>((float)Unsafe.BitCast<T0, double>(value));
				}
			}
			else if (typeof(T1) == typeof(double))
			{
				if (typeof(T0) == typeof(sbyte))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, sbyte>(value));
				}
				else if (typeof(T0) == typeof(short))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, short>(value));
				}
				else if (typeof(T0) == typeof(int))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, int>(value));
				}
				else if (typeof(T0) == typeof(long))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, long>(value));
				}
				else if (typeof(T0) == typeof(byte))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, byte>(value));
				}
				else if (typeof(T0) == typeof(ushort))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, ushort>(value));
				}
				else if (typeof(T0) == typeof(uint))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, uint>(value));
				}
				else if (typeof(T0) == typeof(ulong))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, ulong>(value));
				}
				else if (typeof(T0) == typeof(float))
				{
					return Unsafe.BitCast<double, T1>(Unsafe.BitCast<T0, float>(value));
				}
			}

			throw new MessagePackSerializationException("Invalid numeric value.");
		}

		internal static DecodeResult Read<T>(ref T output, int outputCapacity, in byte msgpack, int msgpackLength, out int writtenLength, out int readLength)
			where T : unmanaged
		{
			if (typeof(T) == typeof(ushort))
			{
				return Read(ref Unsafe.As<T, ushort>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(uint))
			{
				return Read(ref Unsafe.As<T, uint>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(ulong))
			{
				return Read(ref Unsafe.As<T, ulong>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(sbyte))
			{
				return Read(ref Unsafe.As<T, sbyte>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(short))
			{
				return Read(ref Unsafe.As<T, short>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(int))
			{
				return Read(ref Unsafe.As<T, int>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(long))
			{
				return Read(ref Unsafe.As<T, long>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(float))
			{
				return Read(ref Unsafe.As<T, float>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else if (typeof(T) == typeof(double))
			{
				return Read(ref Unsafe.As<T, double>(ref output), outputCapacity, in msgpack, msgpackLength, out writtenLength, out readLength);
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		internal static nuint Write<T>(ref byte output, in T values, nuint inputLength)
			where T : unmanaged
		{
			if (typeof(T) == typeof(bool))
			{
				return Write(ref output, ref Unsafe.As<T, bool>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(ushort))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, ushort>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, ushort>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(uint))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, uint>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, uint>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(ulong))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, ulong>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, ulong>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(sbyte))
			{
				return Write(ref output, ref Unsafe.As<T, sbyte>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(short))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, short>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, short>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(int))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, int>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, int>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(long))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, long>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, long>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(float))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, float>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, float>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else if (typeof(T) == typeof(double))
			{
				if (BitConverter.IsLittleEndian)
				{
					return WriteLittleEndian(ref output, ref Unsafe.As<T, double>(ref Unsafe.AsRef(in values)), inputLength);
				}
				else
				{
					return WriteBigEndian(ref output, ref Unsafe.As<T, double>(ref Unsafe.AsRef(in values)), inputLength);
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Decodes a span of msgpack-encoded primitive values.
		/// </summary>
		/// <param name="output">
		/// The reference to the first element in a span where the decoded values should be written.
		/// The buffer must be at least <paramref name="inputLength"/> elements long.
		/// If the length of this exceeds that of <paramref name="msgpack"/>, this span may remain partially uninitialized.
		/// </param>
		/// <param name="msgpack">
		/// A reference to the first msgpack byte to decode.
		/// The buffer must be at least <paramref name="inputLength"/> bytes long.
		/// </param>
		/// <param name="inputLength">The number of elements to decode.</param>
		/// <returns><see langword="true" /> if the values in <paramref name="msgpack"/> were all valid; otherwise, <see langword="false" />.</returns>
		internal static bool Read(ref bool output, in byte msgpack, int inputLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			int offset = 0;
			if (Vector.IsHardwareAccelerated)
			{
				for (; offset + Vector<byte>.Count <= inputLength; offset += Vector<byte>.Count)
				{
					var loaded = Vector.LoadUnsafe(ref input, unchecked((nuint)offset));
					var trues = Vector.Equals(loaded, new Vector<byte>(MessagePackCode.True));
					if (Vector.BitwiseOr(trues, Vector.Equals(loaded, new Vector<byte>(MessagePackCode.False))) != Vector<byte>.AllBitsSet)
					{
						return false;
					}

					// false is (byte)0. true is (byte)1 ~ (byte)255.
					trues.StoreUnsafe(ref Unsafe.As<bool, byte>(ref output), unchecked((nuint)offset));
				}
			}

			for (; offset < inputLength; offset++)
			{
				byte temp = Unsafe.Add(ref input, offset);
				switch (temp)
				{
					case MessagePackCode.True:
						Unsafe.Add(ref output, offset) = true;
						break;
					case MessagePackCode.False:
						Unsafe.Add(ref output, offset) = false;
						break;
					default:
						return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Encodes a span of primitive values as msgpack.
		/// </summary>
		/// <param name="output">
		/// The location for the first msgpack encoded byte.
		/// The buffer must be at least long enough to encode all the values in <paramref name="values"/>, assuming their maximum size representation including a leading byte that provides the messagepack code.
		/// </param>
		/// <param name="values">
		/// The first element to encode.
		/// All subsequence elements must be contiguous in memory after the first.
		/// This buffer must be at least <paramref name="inputLength"/> elements long.
		/// </param>
		/// <param name="inputLength">The number of elements to encode.</param>
		/// <returns>The number of msgpack bytes written.</returns>
		private static nuint Write(ref byte output, ref bool values, nuint inputLength)
		{
			nuint offset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<sbyte>.Count))
			{
				for (; offset + unchecked((nuint)Vector<sbyte>.Count) <= inputLength; offset += unchecked((nuint)Vector<sbyte>.Count))
				{
					Vector<sbyte> results = Vector.Equals(Vector.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref values), offset), Vector<sbyte>.Zero) + new Vector<sbyte>(unchecked((sbyte)MessagePackCode.True));
					results.StoreUnsafe(ref Unsafe.As<byte, sbyte>(ref output), offset);
				}

				{
					offset = inputLength - unchecked((nuint)Vector<sbyte>.Count);
					Vector<sbyte> results = Vector.Equals(Vector.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref values), offset), Vector<sbyte>.Zero) + new Vector<sbyte>(unchecked((sbyte)MessagePackCode.True));
					results.StoreUnsafe(ref Unsafe.As<byte, sbyte>(ref output), offset);
				}
			}
			else
			{
				for (; offset < inputLength; offset++)
				{
					Unsafe.Add(ref output, offset) = Unsafe.Add(ref values, offset) ? MessagePackCode.True : MessagePackCode.False;
				}
			}

			return inputLength;
		}

		/// <inheritdoc cref="Write(ref byte, ref bool, nuint)"/>
		private static nuint Write(ref byte output, ref sbyte input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				sbyte temp = Unsafe.Add(ref input, inputOffset);
				if (temp < MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, outputOffset++) = MessagePackCode.Int8;
				}

				Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
			}

			return outputOffset;
		}

		private static DecodeResult Read(ref ushort output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = Unsafe.Add(ref input, readLength++);
				if (unchecked((sbyte)code) >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case MessagePackCode.UInt8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, ushort>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case MessagePackCode.UInt16:
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, ushort>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case MessagePackCode.UInt32:
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, ushort>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case MessagePackCode.UInt64:
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, ushort>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case MessagePackCode.Int8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, ushort>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case MessagePackCode.Int16:
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, ushort>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case MessagePackCode.Int32:
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, ushort>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case MessagePackCode.Int64:
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, ushort>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref uint output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = Unsafe.Add(ref input, readLength++);
				if (unchecked((sbyte)code) >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case MessagePackCode.UInt8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, uint>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case MessagePackCode.UInt16:
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, uint>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case MessagePackCode.UInt32:
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, uint>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case MessagePackCode.UInt64:
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, uint>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case MessagePackCode.Int8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, uint>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case MessagePackCode.Int16:
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, uint>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case MessagePackCode.Int32:
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, uint>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case MessagePackCode.Int64:
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, uint>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref ulong output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = Unsafe.Add(ref input, readLength++);
				if (unchecked((sbyte)code) >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case MessagePackCode.UInt8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, ulong>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case MessagePackCode.UInt16:
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, ulong>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case MessagePackCode.UInt32:
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, ulong>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case MessagePackCode.UInt64:
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, ulong>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case MessagePackCode.Int8:
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, ulong>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case MessagePackCode.Int16:
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, ulong>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case MessagePackCode.Int32:
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, ulong>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case MessagePackCode.Int64:
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, ulong>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref sbyte output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, sbyte>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, sbyte>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, sbyte>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, sbyte>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, sbyte>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, sbyte>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, sbyte>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, sbyte>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref short output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, short>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, short>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, short>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, short>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, short>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, short>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, short>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, short>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref int output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, int>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, int>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, int>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, int>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, int>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, int>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, int>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, int>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref long output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, long>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, long>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, long>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, long>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, long>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, long>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, long>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, long>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref float output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.Float32):
						if (readLength + (sizeof(float) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<float, float>(ref input, readLength);
						readLength += sizeof(float);
						break;
					case unchecked((sbyte)MessagePackCode.Float64):
						if (readLength + (sizeof(double) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<double, float>(ref input, readLength);
						readLength += sizeof(double);
						break;
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, float>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, float>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, float>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, float>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, float>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, float>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, float>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, float>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static DecodeResult Read(ref double output, int outputCapacity, in byte msgpack, int inputLength, out int writtenLength, out int readLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			writtenLength = 0;
			readLength = 0;

			for (; readLength < inputLength && writtenLength < outputCapacity;)
			{
				var code = unchecked((sbyte)Unsafe.Add(ref input, readLength++));
				if (code >= MessagePackRange.MinFixNegativeInt)
				{
					Unsafe.Add(ref output, writtenLength++) = code;
					continue;
				}

				switch (code)
				{
					case unchecked((sbyte)MessagePackCode.Float32):
						if (readLength + (sizeof(float) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<float, double>(ref input, readLength);
						readLength += sizeof(float);
						break;
					case unchecked((sbyte)MessagePackCode.Float64):
						if (readLength + (sizeof(double) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<double, double>(ref input, readLength);
						readLength += sizeof(double);
						break;
					case unchecked((sbyte)MessagePackCode.UInt8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<byte, double>(ref input, readLength);
						readLength += sizeof(byte);
						break;
					case unchecked((sbyte)MessagePackCode.UInt16):
						if (readLength + (sizeof(ushort) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ushort, double>(ref input, readLength);
						readLength += sizeof(ushort);
						break;
					case unchecked((sbyte)MessagePackCode.UInt32):
						if (readLength + (sizeof(uint) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<uint, double>(ref input, readLength);
						readLength += sizeof(uint);
						break;
					case unchecked((sbyte)MessagePackCode.UInt64):
						if (readLength + (sizeof(ulong) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<ulong, double>(ref input, readLength);
						readLength += sizeof(ulong);
						break;
					case unchecked((sbyte)MessagePackCode.Int8):
						if (readLength >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<sbyte, double>(ref input, readLength);
						readLength += sizeof(sbyte);
						break;
					case unchecked((sbyte)MessagePackCode.Int16):
						if (readLength + (sizeof(short) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<short, double>(ref input, readLength);
						readLength += sizeof(short);
						break;
					case unchecked((sbyte)MessagePackCode.Int32):
						if (readLength + (sizeof(int) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<int, double>(ref input, readLength);
						readLength += sizeof(int);
						break;
					case unchecked((sbyte)MessagePackCode.Int64):
						if (readLength + (sizeof(long) - 1) >= inputLength)
						{
							--readLength;
							return DecodeResult.InsufficientBuffer;
						}

						Unsafe.Add(ref output, writtenLength++) = Interpret<long, double>(ref input, readLength);
						readLength += sizeof(long);
						break;
					default:
						return DecodeResult.TokenMismatch;
				}
			}

			return DecodeResult.Success;
		}

		private static nuint WriteLittleEndian(ref byte output, ref short input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				short temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
								outputOffset += 3U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref short input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				short temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
								outputOffset += 3U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref int input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				int temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							case <= ushort.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((short)temp)));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
								outputOffset += 5U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					case >= short.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((short)temp)));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref int input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				int temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							case <= ushort.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((short)temp));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
								outputOffset += 5U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					case >= short.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((short)temp));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref long input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				long temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							case <= ushort.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((short)temp)));
								outputOffset += 3U;
								break;
							case <= uint.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((int)temp)));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
								outputOffset += 9U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					case >= short.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((short)temp)));
						outputOffset += 3U;
						break;
					case >= int.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((int)temp)));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref long input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				long temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case >= MessagePackRange.MinFixNegativeInt:
						switch (temp)
						{
							case <= MessagePackRange.MaxFixPositiveInt:
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)temp);
								break;
							case <= byte.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
								outputOffset += 2U;
								break;
							case <= ushort.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((short)temp));
								outputOffset += 3U;
								break;
							case <= uint.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((int)temp));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
								outputOffset += 9U;
								break;
						}

						break;
					case >= sbyte.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)(sbyte)temp);
						outputOffset += 2U;
						break;
					case >= short.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((short)temp));
						outputOffset += 3U;
						break;
					case >= int.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((int)temp));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref ushort input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				ushort temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref ushort input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				ushort temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref uint input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				uint temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					case <= ushort.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref uint input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				uint temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					case <= ushort.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((ushort)temp));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref ulong input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				var temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					case <= ushort.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)));
						outputOffset += 3U;
						break;
					case <= uint.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(unchecked((uint)temp)));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(temp));
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref ulong input, nuint inputLength)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < inputLength; inputOffset++)
			{
				var temp = Unsafe.Add(ref input, inputOffset);
				switch (temp)
				{
					case <= MessagePackRange.MaxFixPositiveInt:
						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)temp);
						break;
					case <= byte.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
						Unsafe.Add(ref output, outputOffset + 1U) = unchecked((byte)temp);
						outputOffset += 2U;
						break;
					case <= ushort.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((ushort)temp));
						outputOffset += 3U;
						break;
					case <= uint.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), unchecked((uint)temp));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), temp);
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref float input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<float, uint>(Unsafe.Add(ref input, inputOffset))));
				outputOffset += 5U;
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref float input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), Unsafe.Add(ref input, inputOffset));
				outputOffset += 5U;
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref double input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<double, ulong>(Unsafe.Add(ref input, inputOffset))));
				outputOffset += 9U;
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref double input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), Unsafe.Add(ref input, inputOffset));
				outputOffset += 9U;
			}

			return outputOffset;
		}
	}

	public enum SpanConstructorKind
	{
		Array,
		List,
		ReadOnlyMemory,
		Memory,
	}

	/// <summary>
	/// A hardware-accelerated converter for an array of <see cref="bool"/> values.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of the enumerable to be converted.</typeparam>
	/// <typeparam name="TElement">The type of element to be converted.</typeparam>
	private class PrimitiveArrayConverter<TEnumerable, TElement> : MessagePackConverter<TEnumerable>
		where TElement : unmanaged
	{
		/// <summary>
		/// The factor by which the input values span length should be multiplied to get the minimum output buffer length.
		/// </summary>
		/// <remarks>
		/// Booleans are unique in that they always take exactly one byte.
		/// All other primitives are assumed to take a max of their own full memory size + a single byte for the msgpack header.
		/// </remarks>
		private static readonly unsafe int MsgPackBufferLengthFactor = typeof(TElement) == typeof(bool) ? 1 : (sizeof(TElement) + 1);

		private static readonly unsafe int ElementMaxSerializableLength = Array.MaxLength / MsgPackBufferLengthFactor;

		private readonly SpanConstructorKind spanConstructorKind;

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimitiveArrayConverter{TEnumerable, TElement}"/> class
		/// with an accelerated encoder and decoder.
		/// </summary>
		internal PrimitiveArrayConverter(SpanConstructorKind spanConstructorKind)
		{
			this.spanConstructorKind = spanConstructorKind;
		}

		/// <inheritdoc/>
		public override TEnumerable? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return default;
			}

			context.DepthStep();
			int count = reader.ReadArrayHeader();
			if (count == 0)
			{
				switch (this.spanConstructorKind)
				{
					case SpanConstructorKind.Array:
						return (TEnumerable)(object)Array.Empty<TElement>();
					case SpanConstructorKind.List:
						return (TEnumerable)(object)new List<TElement>();
					case SpanConstructorKind.ReadOnlyMemory:
						return (TEnumerable)(object)ReadOnlyMemory<TElement>.Empty;
					default:
						return (TEnumerable)(object)Memory<TElement>.Empty;
				}
			}

			Span<byte> temp = stackalloc byte[sizeof(long) + 1];
			int tempLength = 0;
			TEnumerable enumerable;
			Span<TElement> remainingElements;
			switch (this.spanConstructorKind)
			{
				case SpanConstructorKind.Array:
					{
						var array = new TElement[count];
						enumerable = (TEnumerable)(object)array;
						remainingElements = array;
					}

					break;
				case SpanConstructorKind.List:
					var list = new List<TElement>(count);
					enumerable = (TEnumerable)(object)list;
					CollectionsMarshal.SetCount(list, count);
					remainingElements = CollectionsMarshal.AsSpan(list);
					break;
				case SpanConstructorKind.ReadOnlyMemory:
					{
						var array = new TElement[count];
						enumerable = (TEnumerable)(object)new ReadOnlyMemory<TElement>(array);
						remainingElements = array;
					}

					break;
				default:
					{
						var array = new TElement[count];
						enumerable = (TEnumerable)(object)new Memory<TElement>(array);
						remainingElements = array;
					}

					break;
			}

			if (typeof(TElement) == typeof(bool))
			{
				ReadOnlySequence<byte> sequence = reader.ReadRaw(count);
				foreach (ReadOnlyMemory<byte> segment in sequence)
				{
					if (!MessagePackPrimitiveSpanUtility.Read(ref Unsafe.As<TElement, bool>(ref MemoryMarshal.GetReference(remainingElements)), in MemoryMarshal.GetReference(segment.Span), segment.Length))
					{
						throw new MessagePackSerializationException("Not all elements were boolean msgpack values.");
					}

					remainingElements = remainingElements[segment.Length..];
				}
			}
			else
			{
				ReadOnlySequence<byte> sequence = reader.Sequence.Slice(reader.Position);
				long sliceLength = 0;
				foreach (ReadOnlyMemory<byte> segment in sequence)
				{
					ReadOnlySpan<byte> segmentSpan = segment.Span;
					if (tempLength > 0)
					{
						switch (unchecked((sbyte)temp[0]))
						{
							case unchecked((sbyte)MessagePackCode.UInt8):
								remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<byte, TElement>(ref MemoryMarshal.GetReference(segmentSpan), 0);
								remainingElements = remainingElements[1..];
								sliceLength++;
								segmentSpan = segmentSpan[1..];
								tempLength = 0;
								break;
							case unchecked((sbyte)MessagePackCode.Int8):
								remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<sbyte, TElement>(ref MemoryMarshal.GetReference(segmentSpan), 0);
								remainingElements = remainingElements[1..];
								sliceLength++;
								segmentSpan = segmentSpan[1..];
								tempLength = 0;
								break;
							case unchecked((sbyte)MessagePackCode.UInt16):
								{
									int copyLength = sizeof(ushort) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<ushort, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.Int16):
								{
									int copyLength = sizeof(short) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<short, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.Float32):
								{
									int copyLength = sizeof(float) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<float, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.UInt32):
								{
									int copyLength = sizeof(uint) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<uint, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.Int32):
								{
									int copyLength = sizeof(int) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<int, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.Float64):
								{
									int copyLength = sizeof(double) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<double, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.UInt64):
								{
									int copyLength = sizeof(ulong) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<ulong, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							case unchecked((sbyte)MessagePackCode.Int64):
								{
									int copyLength = sizeof(long) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += segmentSpan.Length;
										sliceLength += segmentSpan.Length;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
									sliceLength += copyLength;
									remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<long, TElement>(ref MemoryMarshal.GetReference(temp), 1);
									remainingElements = remainingElements[1..];
									tempLength = 0;
								}

								break;
							default:
								throw new InvalidProgramException();
						}

						if (segmentSpan.IsEmpty)
						{
							continue;
						}
					}

					switch (MessagePackPrimitiveSpanUtility.Read(ref MemoryMarshal.GetReference(remainingElements), remainingElements.Length, in MemoryMarshal.GetReference(segmentSpan), segmentSpan.Length, out var writtenLength, out var readLength))
					{
						case DecodeResult.Success:
							tempLength = 0;
							break;
						case DecodeResult.InsufficientBuffer:
							ReadOnlySpan<byte> restSpan = segmentSpan[readLength..];
							tempLength = restSpan.Length < 9 ? restSpan.Length : 9;
							sliceLength += tempLength;
							restSpan[..tempLength].CopyTo(temp);
							break;
						default:
							throw new MessagePackSerializationException("Not all elements were numeric msgpack values.");
					}

					sliceLength += readLength;
					remainingElements = remainingElements[writtenLength..];
				}

				reader.ReadRaw(sliceLength);
			}

			return enumerable;
		}

		/// <inheritdoc/>
		public override void Write(ref MessagePackWriter writer, in TEnumerable? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			context.DepthStep();
			ref TElement reference = ref TryGetReferenceAndLength<TEnumerable, TElement>(value, out int length);
			if (Unsafe.IsNullRef(ref reference))
			{
				writer.WriteArrayHeader(0);
				return;
			}

			writer.WriteArrayHeader(length);
			while (length > 0)
			{
				int consumedSpanLength = length > ElementMaxSerializableLength ? ElementMaxSerializableLength : length;
				Span<byte> buffer = writer.GetSpan(consumedSpanLength * MsgPackBufferLengthFactor);
				nuint writtenBytes = MessagePackPrimitiveSpanUtility.Write(ref MemoryMarshal.GetReference(buffer), in reference, unchecked((nuint)consumedSpanLength));
				writer.Advance(unchecked((int)writtenBytes));
				reference = ref Unsafe.Add(ref reference, consumedSpanLength);
				length -= consumedSpanLength;
			}
		}
	}
}
