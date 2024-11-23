// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
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
	/// <param name="spanConstructor">The constructor for the enumerable type.</param>
	/// <param name="converter">Receives the hardware-accelerated converter if one is available.</param>
	/// <returns>A value indicating whether a converter is available.</returns>
	internal static bool TryGetConverter<TEnumerable, TElement>(
		SpanConstructor<TElement, TEnumerable> spanConstructor,
		[NotNullWhen(true)] out MessagePackConverter<TEnumerable>? converter)
	{
		if (CanGetSpan<TEnumerable, TElement>(out bool assignableFromArray))
		{
			object? spanConstructorToUse = assignableFromArray ? null : spanConstructor;

			if (typeof(TElement) == typeof(bool))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, bool>((SpanConstructor<bool, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(sbyte))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, sbyte>((SpanConstructor<sbyte, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(short))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, short>((SpanConstructor<short, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(int))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, int>((SpanConstructor<int, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(long))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, long>((SpanConstructor<long, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(ushort))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, ushort>((SpanConstructor<ushort, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(uint))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, uint>((SpanConstructor<uint, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(ulong))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, ulong>((SpanConstructor<ulong, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(float))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, float>((SpanConstructor<float, TEnumerable>?)spanConstructorToUse);
				return true;
			}
			else if (typeof(TElement) == typeof(double))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, double>((SpanConstructor<double, TEnumerable>?)spanConstructorToUse);
				return true;
			}
		}

		converter = null;
		return false;
	}

	private static bool CanGetSpan<TEnumerable, TElement>(out bool assignableFromArray)
	{
		Type enumerableType = typeof(TEnumerable);
		assignableFromArray = typeof(TElement[]).IsAssignableTo(enumerableType);
		return
			enumerableType == typeof(TElement[]) ||
			enumerableType == typeof(List<TElement>) ||
			enumerableType == typeof(ReadOnlyMemory<TElement>) ||
			enumerableType == typeof(Memory<TElement>);
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
				return Write(ref output, ref Unsafe.As<T, ushort>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(uint))
			{
				return Write(ref output, ref Unsafe.As<T, uint>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(ulong))
			{
				return Write(ref output, ref Unsafe.As<T, ulong>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(sbyte))
			{
				return Write(ref output, ref Unsafe.As<T, sbyte>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(short))
			{
				return Write(ref output, ref Unsafe.As<T, short>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(int))
			{
				return Write(ref output, ref Unsafe.As<T, int>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(long))
			{
				return Write(ref output, ref Unsafe.As<T, long>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(float))
			{
				return Write(ref output, ref Unsafe.As<T, float>(ref Unsafe.AsRef(in values)), inputLength);
			}
			else if (typeof(T) == typeof(double))
			{
				return Write(ref output, ref Unsafe.As<T, double>(ref Unsafe.AsRef(in values)), inputLength);
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

		private static nuint Write(ref byte output, ref short input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref int input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref long input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref ushort input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref uint input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref ulong input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref float input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

		private static nuint Write(ref byte output, ref double input, nuint inputLength) => BitConverter.IsLittleEndian ? WriteLittleEndian(ref output, ref input, inputLength) : WriteBigEndian(ref output, ref input, inputLength);

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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<sbyte>.Count))
			{
				do
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);
					var additionalBytes = Vector.GreaterThan(new Vector<sbyte>(unchecked((sbyte)MessagePackCode.MinNegativeFixInt)), loaded);
					if (additionalBytes == Vector<sbyte>.Zero)
					{
						loaded.As<sbyte, byte>().StoreUnsafe(ref output, outputOffset);
						inputOffset += unchecked((nuint)Vector<sbyte>.Count);
						outputOffset += unchecked((nuint)Vector<sbyte>.Count);
						continue;
					}

					for (int i = 0; i < Vector<sbyte>.Count; i++)
					{
						if (additionalBytes.GetElement(i) != 0)
						{
							Unsafe.Add(ref output, outputOffset++) = MessagePackCode.Int8;
						}

						Unsafe.Add(ref output, outputOffset++) = unchecked((byte)Unsafe.Add(ref input, inputOffset++));
					}
				}
				while (inputOffset + unchecked((nuint)Vector<sbyte>.Count) <= inputLength);
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<short>.Count))
			{
				for (; inputOffset + unchecked((nuint)Vector<short>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<short>.Count))
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<short>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<short>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<short>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<short>(byte.MaxValue));

					// 0 -> Int16, 3byte
					// 1 -> Int8, 2byte
					// 2 -> None, 1byte
					// 3 -> UInt8, 2byte
					// 4 -> UInt16, 3byte
					Vector<short> kinds = -(gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue);
					if (kinds == new Vector<short>(2))
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(short))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement(i);
						}

						continue;
					}

					Vector<short> shuffled;
					if (AdvSimd.IsSupported && Vector<short>.Count == Vector128<short>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						shuffled = Vector.ShiftLeft(loaded, 8) | Vector.ShiftRightLogical(loaded, 8);
					}

					for (int i = 0; i < Vector<short>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 3U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset++) = shuffled.As<short, byte>().GetElement((i * 2) + 1);
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 3U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<short>.Count))
			{
				for (; inputOffset + unchecked((nuint)Vector<short>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<short>.Count))
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<short>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<short>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<short>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<short>(byte.MaxValue));

					// 0 -> Int16, 3byte
					// 1 -> Int8, 2byte
					// 2 -> None, 1byte
					// 3 -> UInt8, 2byte
					// 4 -> UInt16, 3byte
					Vector<short> kinds = -(gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue);
					if (kinds == new Vector<short>(2))
					{
						for (int i = sizeof(short) - 1; i < Vector<byte>.Count; i += sizeof(short))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<short>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 3U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement((i * 2) + 1);
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 3U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<int>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<int>.Count) <= inputLength; inputOffset += (nuint)Vector<int>.Count)
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than short.MinValue value requires 5 byte.
					var gteInt16MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<int>(short.MinValue));

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<int>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<int>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<int>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<int>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<int>(ushort.MaxValue));

					// 0 -> Int32,  5byte
					// 1 -> Int16,  3byte
					// 2 -> Int8,   2byte
					// 3 -> None,   1byte
					// 4 -> UInt8,  2byte
					// 5 -> UInt16, 3byte
					// 6 -> UInt32, 5byte
					Vector<int> kinds = -(gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue);
					if (kinds == new Vector<int>(3))
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(int))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<int, byte>().GetElement(i);
						}

						continue;
					}

					Vector<int> shuffled;
					if (AdvSimd.IsSupported && Vector<int>.Count == Vector128<int>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<ushort> left = (loaded << 16).As<int, ushort>();
						Vector<ushort> right = (loaded >>> 16).As<int, ushort>();
						shuffled = (left << 8 | (left >>> 8) | right << 8 | (right >>> 8)).As<ushort, int>();
					}

					for (int i = 0; i < Vector<int>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 5U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<int, short>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = shuffled.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 1U;
								break;
							case 4:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 5:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<int, ushort>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 5U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<int>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<int>.Count) <= inputLength; inputOffset += (nuint)Vector<int>.Count)
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than short.MinValue value requires 5 byte.
					var gteInt16MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<int>(short.MinValue));

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<int>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<int>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<int>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<int>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<int>(ushort.MaxValue));

					// 0 -> Int32,  5byte
					// 1 -> Int16,  3byte
					// 2 -> Int8,   2byte
					// 3 -> None,   1byte
					// 4 -> UInt8,  2byte
					// 5 -> UInt16, 3byte
					// 6 -> UInt32, 5byte
					Vector<int> kinds = -(gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue);
					if (kinds == new Vector<int>(3))
					{
						for (int i = sizeof(int) - 1; i < Vector<byte>.Count; i += sizeof(int))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<int, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<int>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 5U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<int, short>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = loaded.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 1U;
								break;
							case 4:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 5:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<int, ushort>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 5U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<long>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<long>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<long>.Count))
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than int.MinValue value requires 9 byte.
					var gteInt32MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(int.MinValue));

					// Less than short.MinValue value requires 5 byte.
					var gteInt16MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(short.MinValue));

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<long>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<long>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<long>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<long>(ushort.MaxValue));

					// GreaterThan uint.MaxValue value requires 5 byte.
					var gtUInt32MaxValue = Vector.GreaterThan(loaded, new Vector<long>(uint.MaxValue));

					// 0 -> Int64,  9byte
					// 1 -> Int32,  5byte
					// 2 -> Int16,  3byte
					// 3 -> Int8,   2byte
					// 4 -> None,   1byte
					// 5 -> UInt8,  2byte
					// 6 -> UInt16, 3byte
					// 7 -> UInt32, 5byte
					// 8 -> UInt64, 9byte
					Vector<long> kinds = -(gteInt32MinValue + gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue + gtUInt32MaxValue);
					if (kinds == new Vector<long>(4L))
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(long))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<long, byte>().GetElement(i);
						}

						continue;
					}

					// Reorder Big-Endian
					Vector<long> shuffled;
					if (AdvSimd.IsSupported && Vector<long>.Count == Vector128<long>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<uint> left = (loaded << 32).As<long, uint>();
						Vector<uint> right = (loaded >>> 32).As<long, uint>();
						Vector<ushort> left_left = (left << 16).As<uint, ushort>();
						Vector<ushort> left_right = (left >>> 16).As<uint, ushort>();
						Vector<ushort> right_left = (right << 16).As<uint, ushort>();
						Vector<ushort> right_right = (right >>> 16).As<uint, ushort>();
						shuffled = (left_left << 8 | (left_left >>> 8) | left_right << 8 | (left_right >>> 8) | right_left << 8 | (right_left >>> 8) | right_right << 8 | (right_right >>> 8)).As<ushort, long>();
					}

					for (int i = 0; i < Vector<long>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 9U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, int>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, short>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 4:
								Unsafe.Add(ref output, outputOffset) = shuffled.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 1U;
								break;
							case 5:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 6:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, ushort>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 7:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, uint>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 9U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<long>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<long>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<long>.Count))
				{
					var loaded = Vector.LoadUnsafe(ref input, inputOffset);

					// Less than int.MinValue value requires 9 byte.
					var gteInt32MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(int.MinValue));

					// Less than short.MinValue value requires 5 byte.
					var gteInt16MinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(short.MinValue));

					// Less than sbyte.MinValue value requires 3 byte.
					var gteSByteMinValue = Vector.GreaterThanOrEqual(loaded, new Vector<long>(sbyte.MinValue));

					// Less than MinFixNegativeInt value requires 2 byte.
					var gteMinFixNegativeInt = Vector.GreaterThanOrEqual(loaded, new Vector<long>(MessagePackRange.MinFixNegativeInt));

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<long>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<long>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<long>(ushort.MaxValue));

					// GreaterThan uint.MaxValue value requires 5 byte.
					var gtUInt32MaxValue = Vector.GreaterThan(loaded, new Vector<long>(uint.MaxValue));

					// 0 -> Int64,  9byte
					// 1 -> Int32,  5byte
					// 2 -> Int16,  3byte
					// 3 -> Int8,   2byte
					// 4 -> None,   1byte
					// 5 -> UInt8,  2byte
					// 6 -> UInt16, 3byte
					// 7 -> UInt32, 5byte
					// 8 -> UInt64, 9byte
					Vector<long> kinds = -(gteInt32MinValue + gteInt16MinValue + gteSByteMinValue + gteMinFixNegativeInt + gtMaxFixPositiveInt + gtByteMaxValue + gtUInt16MaxValue + gtUInt32MaxValue);
					if (kinds == new Vector<long>(4L))
					{
						for (int i = sizeof(long) - 1; i < Vector<byte>.Count; i += sizeof(long))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<long, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<long>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 9U;
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, int>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, short>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 4:
								Unsafe.Add(ref output, outputOffset) = loaded.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 1U;
								break;
							case 5:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 6:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, ushort>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 7:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, uint>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 9U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<short>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<short>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<short>.Count))
				{
					Vector<short> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<ushort, short>();

					// LessThan 0 means ushort max range and requires 3 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<short>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<short>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<short>(byte.MaxValue));

					// -1 -> UInt16, 3byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// Vector<short>.AllBitsSet means -1.
					Vector<short> kinds = Vector<short>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue;
					if (kinds == Vector<short>.Zero)
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(short))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement(i);
						}

						continue;
					}

					Vector<short> shuffled;
					if (AdvSimd.IsSupported && Vector<short>.Count == Vector128<short>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						shuffled = loaded << 8 | (loaded >>> 8);
					}

					for (int i = 0; i < Vector<short>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = shuffled.As<short, byte>().GetElement((i * 2) + 1);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 3U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<short>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<short>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<short>.Count))
				{
					Vector<short> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<ushort, short>();

					// LessThan 0 means ushort max range and requires 3 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<short>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<short>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<short>(byte.MaxValue));

					// -1 -> UInt16, 3byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// Vector<short>.AllBitsSet means -1.
					Vector<short> kinds = Vector<short>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue;
					if (kinds == Vector<short>.Zero)
					{
						for (int i = sizeof(short) - 1; i < Vector<byte>.Count; i += sizeof(short))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<short>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = loaded.As<short, byte>().GetElement((i * 2) + 1);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<short, byte>().GetElement((i * 2) + 1);
								outputOffset += 2U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 3U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<uint>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<uint>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<uint>.Count))
				{
					Vector<int> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<uint, int>();

					// LessThan 0 means ushort max range and requires 5 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<int>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<int>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<int>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<int>(ushort.MaxValue));

					// -1 -> UInt32, 5byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// +3 -> UInt32, 5byte
					Vector<int> kinds = Vector<int>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue;
					if (kinds == Vector<int>.Zero)
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(int))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<int, byte>().GetElement(i);
						}

						continue;
					}

					// Reorder Big-Endian
					Vector<int> shuffled;
					if (AdvSimd.IsSupported && Vector<int>.Count == Vector128<int>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<ushort> left = (loaded << 16).As<int, ushort>();
						Vector<ushort> right = (loaded >>> 16).As<int, ushort>();
						shuffled = (left << 8 | (left >>> 8) | right << 8 | (right >>> 8)).As<ushort, int>();
					}

					for (int i = 0; i < Vector<uint>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = shuffled.As<int, byte>().GetElement((i * 4) + 3);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<int, ushort>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 5U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<uint>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<uint>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<uint>.Count))
				{
					Vector<int> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<uint, int>();

					// LessThan 0 means ushort max range and requires 5 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<int>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<int>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<int>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<int>(ushort.MaxValue));

					// -1 -> UInt32, 5byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// +3 -> UInt32, 5byte
					Vector<int> kinds = Vector<int>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue;
					if (kinds == Vector<int>.Zero)
					{
						for (int i = sizeof(int) - 1; i < Vector<byte>.Count; i += sizeof(int))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<int, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<uint>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = loaded.As<int, byte>().GetElement((i * 4) + 3);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<int, byte>().GetElement((i * 4) + 3);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<int, ushort>().GetElement((i * 2) + 1));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 5U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<ulong>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<ulong>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<ulong>.Count))
				{
					Vector<long> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<ulong, long>();

					// LessThan 0 means ushort max range and requires 5 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<long>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<long>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<long>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<long>(ushort.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt32MaxValue = Vector.GreaterThan(loaded, new Vector<long>(uint.MaxValue));

					// -1 -> UInt64, 9byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// +3 -> UInt32, 5byte
					// +4 -> UInt64, 9byte
					Vector<long> kinds = Vector<long>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue - gtUInt32MaxValue;
					if (kinds == Vector<long>.Zero)
					{
						for (int i = 0; i < Vector<byte>.Count; i += sizeof(long))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<long, byte>().GetElement(i);
						}

						continue;
					}

					// Reorder Big-Endian
					Vector<long> shuffled;
					if (AdvSimd.IsSupported && Vector<long>.Count == Vector128<long>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<uint> left = (loaded << 32).As<long, uint>();
						Vector<uint> right = (loaded >>> 32).As<long, uint>();
						Vector<ushort> left_left = (left << 16).As<uint, ushort>();
						Vector<ushort> left_right = (left >>> 16).As<uint, ushort>();
						Vector<ushort> right_left = (right << 16).As<uint, ushort>();
						Vector<ushort> right_right = (right >>> 16).As<uint, ushort>();
						shuffled = (left_left << 8 | (left_left >>> 8) | left_right << 8 | (left_right >>> 8) | right_left << 8 | (right_left >>> 8) | right_right << 8 | (right_right >>> 8)).As<ushort, long>();
					}

					for (int i = 0; i < Vector<long>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = shuffled.As<long, byte>().GetElement((i * 8) + 7);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = shuffled.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, ushort>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.As<long, uint>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
								outputOffset += 9U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<ulong>.Count) << 1)
			{
				for (; inputOffset + unchecked((nuint)Vector<ulong>.Count) <= inputLength; inputOffset += unchecked((nuint)Vector<ulong>.Count))
				{
					Vector<long> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<ulong, long>();

					// LessThan 0 means ushort max range and requires 5 byte.
					var gte0 = Vector.GreaterThanOrEqual(loaded, Vector<long>.Zero);

					// GreaterThan MaxFixPositiveInt value requires 2 byte.
					var gtMaxFixPositiveInt = Vector.GreaterThan(loaded, new Vector<long>(MessagePackRange.MaxFixPositiveInt));

					// GreaterThan byte.MaxValue value requires 3 byte.
					var gtByteMaxValue = Vector.GreaterThan(loaded, new Vector<long>(byte.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt16MaxValue = Vector.GreaterThan(loaded, new Vector<long>(ushort.MaxValue));

					// GreaterThan ushort.MaxValue value requires 5 byte.
					var gtUInt32MaxValue = Vector.GreaterThan(loaded, new Vector<long>(uint.MaxValue));

					// -1 -> UInt64, 9byte
					// +0 -> None, 1byte
					// +1 -> UInt8, 2byte
					// +2 -> UInt16, 3byte
					// +3 -> UInt32, 5byte
					// +4 -> UInt64, 9byte
					Vector<long> kinds = Vector<long>.AllBitsSet - gte0 - gtMaxFixPositiveInt - gtByteMaxValue - gtUInt16MaxValue - gtUInt32MaxValue;
					if (kinds == Vector<long>.Zero)
					{
						for (int i = sizeof(long) - 1; i < Vector<byte>.Count; i += sizeof(long))
						{
							Unsafe.Add(ref output, outputOffset++) = loaded.As<long, byte>().GetElement(i);
						}

						continue;
					}

					for (int i = 0; i < Vector<long>.Count; i++)
					{
						switch (kinds.GetElement(i))
						{
							case 0:
								Unsafe.Add(ref output, outputOffset++) = loaded.As<long, byte>().GetElement((i * 8) + 7);
								break;
							case 1:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt8;
								Unsafe.Add(ref output, outputOffset + 1U) = loaded.As<long, byte>().GetElement((i * 8) + 7);
								outputOffset += 2U;
								break;
							case 2:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt16;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, ushort>().GetElement((i * 4) + 3));
								outputOffset += 3U;
								break;
							case 3:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.As<long, uint>().GetElement((i * 2) + 1));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), loaded.GetElement(i));
								outputOffset += 9U;
								break;
						}
					}
				}
			}

			for (; inputOffset < inputLength; inputOffset++)
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
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && length >= unchecked((nuint)(Vector<uint>.Count << 1)))
			{
				for (; inputOffset + unchecked((nuint)Vector<uint>.Count) <= length; inputOffset += unchecked((nuint)Vector<uint>.Count))
				{
					Vector<uint> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<float, uint>();
					Vector<uint> shuffled;
					if (AdvSimd.IsSupported && Vector<uint>.Count == Vector128<uint>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<ushort> left = (loaded << 16).As<uint, ushort>();
						Vector<ushort> right = (loaded >>> 16).As<uint, ushort>();
						shuffled = (left << 8 | (left >>> 8) | right << 8 | (right >>> 8)).As<ushort, uint>();
					}

					for (int i = 0; i < Vector<uint>.Count; i++)
					{
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
						outputOffset += 5U;
					}
				}
			}

			for (; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<float, uint>(Unsafe.Add(ref input, inputOffset))));
				outputOffset += 5U;
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref float input, nuint length)
		{
			nuint inputOffset = 0, outputOffset = 0;
			for (; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), Unsafe.Add(ref input, inputOffset));
				outputOffset += 5U;
			}

			return outputOffset;
		}

		private static nuint WriteLittleEndian(ref byte output, ref double input, nuint length)
		{
			nuint inputOffset = 0, outputOffset = 0;
			if (Vector.IsHardwareAccelerated && length >= unchecked((nuint)(Vector<ulong>.Count << 1)))
			{
				for (; inputOffset + unchecked((nuint)Vector<ulong>.Count) <= length; inputOffset += unchecked((nuint)Vector<ulong>.Count))
				{
					Vector<ulong> loaded = Vector.LoadUnsafe(ref input, inputOffset).As<double, ulong>();
					Vector<ulong> shuffled;
					if (AdvSimd.IsSupported && Vector<uint>.Count == Vector128<ulong>.Count)
					{
						shuffled = AdvSimd.ReverseElement8(loaded.AsVector128()).AsVector();
					}
					else
					{
						Vector<uint> left = (loaded << 32).As<ulong, uint>();
						Vector<uint> right = (loaded >>> 32).As<ulong, uint>();
						Vector<ushort> left_left = (left << 16).As<uint, ushort>();
						Vector<ushort> left_right = (left >>> 16).As<uint, ushort>();
						Vector<ushort> right_left = (right << 16).As<uint, ushort>();
						Vector<ushort> right_right = (right >>> 16).As<uint, ushort>();
						shuffled = (left_left << 8 | (left_left >>> 8) | left_right << 8 | (left_right >>> 8) | right_left << 8 | (right_left >>> 8) | right_right << 8 | (right_right >>> 8)).As<ushort, ulong>();
					}

					for (int i = 0; i < Vector<ulong>.Count; i++)
					{
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), shuffled.GetElement(i));
						outputOffset += 9U;
					}
				}
			}

			for (; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<double, ulong>(Unsafe.Add(ref input, inputOffset))));
				outputOffset += 9U;
			}

			return outputOffset;
		}

		private static nuint WriteBigEndian(ref byte output, ref double input, nuint length)
		{
			nuint inputOffset = 0, outputOffset = 0;
			for (; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), Unsafe.Add(ref input, inputOffset));
				outputOffset += 9U;
			}

			return outputOffset;
		}
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

		private readonly SpanConstructor<TElement, TEnumerable>? ctor;

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimitiveArrayConverter{TEnumerable, TElement}"/> class
		/// with an accelerated encoder and decoder.
		/// </summary>
		/// <param name="ctor">The constructor to pass the span of values to, if an array isn't sufficient.</param>
		internal PrimitiveArrayConverter(SpanConstructor<TElement, TEnumerable>? ctor)
		{
			this.ctor = ctor;
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
				return this.ctor is null ? (TEnumerable)(object)Array.Empty<TElement>() : this.ctor(default);
			}

			TElement[] elements = this.ctor is null ? new TElement[count] : ArrayPool<TElement>.Shared.Rent(count);
			try
			{
				Span<byte> temp = stackalloc byte[sizeof(long) + 1];
				int tempLength = 0;
				Span<TElement> remainingElements = elements.AsSpan(0, count);
				ReadOnlySequence<byte> sequence = reader.ReadRaw(count);
				foreach (ReadOnlyMemory<byte> segment in sequence)
				{
					if (typeof(TElement) == typeof(bool))
					{
						if (!MessagePackPrimitiveSpanUtility.Read(ref Unsafe.As<TElement, bool>(ref MemoryMarshal.GetReference(remainingElements)), in MemoryMarshal.GetReference(segment.Span), segment.Length))
						{
							throw new MessagePackSerializationException("Not all elements were boolean msgpack values.");
						}

						remainingElements = remainingElements[segment.Length..];
						continue;
					}

					ReadOnlySpan<byte> segmentSpan = segment.Span;
					if (tempLength > 0)
					{
						switch (unchecked((sbyte)temp[0]))
						{
							case unchecked((sbyte)MessagePackCode.UInt8):
								remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<byte, TElement>(ref MemoryMarshal.GetReference(segmentSpan), 0);
								remainingElements = remainingElements[1..];
								segmentSpan = segmentSpan[1..];
								tempLength = 0;
								break;
							case unchecked((sbyte)MessagePackCode.Int8):
								remainingElements[0] = MessagePackPrimitiveSpanUtility.Interpret<sbyte, TElement>(ref MemoryMarshal.GetReference(segmentSpan), 0);
								remainingElements = remainingElements[1..];
								segmentSpan = segmentSpan[1..];
								tempLength = 0;
								break;
							case unchecked((sbyte)MessagePackCode.UInt16):
								{
									int copyLength = sizeof(ushort) + 1 - tempLength;
									if (copyLength > segmentSpan.Length)
									{
										segmentSpan.CopyTo(temp[tempLength..]);
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
										tempLength += copyLength;
										continue;
									}

									segmentSpan[..copyLength].CopyTo(temp[tempLength..]);
									segmentSpan = segmentSpan[copyLength..];
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
							restSpan[..tempLength].CopyTo(temp);
							break;
						default:
							throw new MessagePackSerializationException("Not all elements were numeric msgpack values.");
					}

					remainingElements = remainingElements[writtenLength..];
				}

				return this.ctor is null ? (TEnumerable)(object)elements : this.ctor(elements.AsSpan(0, count));
			}
			finally
			{
				if (this.ctor is not null)
				{
					ArrayPool<TElement>.Shared.Return(elements);
				}
			}
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
			Assumes.False(Unsafe.IsNullRef(ref reference));
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
