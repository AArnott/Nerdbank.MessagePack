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

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Hardware-accelerated converters for arrays of various primitive types.
/// </summary>
internal static class HardwareAccelerated
{
	/// <inheritdoc cref="MessagePackPrimitiveSpanUtility.Read(ref bool, in byte, nuint)"/>
	internal delegate bool AcceleratedRead<T>(ref T output, in byte msgpack, nuint inputLength);

	/// <inheritdoc cref="MessagePackPrimitiveSpanUtility.Write(ref byte, in bool, nuint)" />
	internal delegate nuint AcceleratedWrite<T>(ref byte output, in T values, nuint inputLength);

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
				converter = new PrimitiveArrayConverter<TEnumerable, bool>(
					(SpanConstructor<bool, TEnumerable>?)spanConstructorToUse,
					MessagePackPrimitiveSpanUtility.Read,
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(sbyte))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, sbyte>(
					(SpanConstructor<sbyte, TEnumerable>?)spanConstructorToUse,
					new SByteConverter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(short))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, short>(
					(SpanConstructor<short, TEnumerable>?)spanConstructorToUse,
					new Int16Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(int))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, int>(
					(SpanConstructor<int, TEnumerable>?)spanConstructorToUse,
					new Int32Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(long))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, long>(
					(SpanConstructor<long, TEnumerable>?)spanConstructorToUse,
					new Int64Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(ushort))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, ushort>(
					(SpanConstructor<ushort, TEnumerable>?)spanConstructorToUse,
					new UInt16Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(uint))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, uint>(
					(SpanConstructor<uint, TEnumerable>?)spanConstructorToUse,
					new UInt32Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(ulong))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, ulong>(
					(SpanConstructor<ulong, TEnumerable>?)spanConstructorToUse,
					new UInt64Converter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(float))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, float>(
					(SpanConstructor<float, TEnumerable>?)spanConstructorToUse,
					new SingleConverter(),
					MessagePackPrimitiveSpanUtility.Write);
				return true;
			}
			else if (typeof(TElement) == typeof(double))
			{
				converter = new PrimitiveArrayConverter<TEnumerable, double>(
					(SpanConstructor<double, TEnumerable>?)spanConstructorToUse,
					new DoubleConverter(),
					MessagePackPrimitiveSpanUtility.Write);
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

	private static bool TryGetSpan<TEnumerable, TElement>(TEnumerable enumerable, out ReadOnlySpan<TElement> span)
	{
		switch (enumerable)
		{
			case TElement[] array:
				span = array;
				return true;
			case List<TElement> list:
				span = CollectionsMarshal.AsSpan(list);
				return true;
			case ReadOnlyMemory<TElement> rom:
				span = rom.Span;
				return true;
			case Memory<TElement> mem:
				span = mem.Span;
				return true;
		}

		span = default;
		return false;
	}

	/// <summary>
	/// Read/Write methods with Hardware Intrinsics for primitive msgpack codes.
	/// </summary>
	private static class MessagePackPrimitiveSpanUtility
	{
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
		internal static bool Read(ref bool output, in byte msgpack, nuint inputLength)
		{
			ref byte input = ref Unsafe.AsRef(in msgpack);
			nuint offset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<byte>.Count))
			{
				for (; offset + unchecked((nuint)Vector<byte>.Count) <= inputLength; offset += unchecked((nuint)Vector<byte>.Count))
				{
					var loaded = Vector.LoadUnsafe(ref input, offset);
					var trues = Vector.Equals(loaded, new Vector<byte>(MessagePackCode.True));
					if (Vector.BitwiseOr(trues, Vector.Equals(loaded, new Vector<byte>(MessagePackCode.False))) != Vector<byte>.AllBitsSet)
					{
						return false;
					}

					// false is (byte)0. true is (byte)1 ~ (byte)255.
					trues.StoreUnsafe(ref Unsafe.As<bool, byte>(ref output), offset);
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
		internal static nuint Write(ref byte output, in bool values, nuint inputLength)
		{
			ref bool input = ref Unsafe.AsRef(in values);
			nuint offset = 0;
			if (Vector.IsHardwareAccelerated && inputLength >= unchecked((nuint)Vector<sbyte>.Count))
			{
				for (; offset + unchecked((nuint)Vector<sbyte>.Count) <= inputLength; offset += unchecked((nuint)Vector<sbyte>.Count))
				{
					Vector<sbyte> results = Vector.Equals(Vector.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref input), offset), Vector<sbyte>.Zero) + new Vector<sbyte>(unchecked((sbyte)MessagePackCode.True));
					results.StoreUnsafe(ref Unsafe.As<byte, sbyte>(ref output), offset);
				}

				{
					offset = inputLength - unchecked((nuint)Vector<sbyte>.Count);
					Vector<sbyte> results = Vector.Equals(Vector.LoadUnsafe(ref Unsafe.As<bool, sbyte>(ref input), offset), Vector<sbyte>.Zero) + new Vector<sbyte>(unchecked((sbyte)MessagePackCode.True));
					results.StoreUnsafe(ref Unsafe.As<byte, sbyte>(ref output), offset);
				}
			}
			else
			{
				for (; offset < inputLength; offset++)
				{
					Unsafe.Add(ref output, offset) = Unsafe.Add(ref input, offset) ? MessagePackCode.True : MessagePackCode.False;
				}
			}

			return inputLength;
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in sbyte values, nuint inputLength)
		{
			ref sbyte input = ref Unsafe.AsRef(in values);
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

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in short values, nuint inputLength)
		{
			ref short input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in int values, nuint inputLength)
		{
			ref int input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in long values, nuint inputLength)
		{
			ref long input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in ushort values, nuint inputLength)
		{
			ref ushort input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in uint values, nuint inputLength)
		{
			ref uint input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in ulong values, nuint inputLength)
		{
			ref ulong input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in float values, nuint inputLength)
		{
			ref float input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
		}

		/// <inheritdoc cref="Write(ref byte, in bool, nuint)"/>
		internal static nuint Write(ref byte output, in double values, nuint inputLength)
		{
			ref double input = ref Unsafe.AsRef(in values);
			return BitConverter.IsLittleEndian
				? WriteLittleEndian(ref output, ref input, inputLength)
				: WriteBigEndian(ref output, ref input, inputLength);
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
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)input);
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
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)input);
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
								Unsafe.Add(ref output, outputOffset++) = unchecked((byte)(sbyte)input);
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
		private static readonly int MsgPackBufferLengthFactor = typeof(TElement) == typeof(bool) ? 1 : (Unsafe.SizeOf<TElement>() + 1);

		private readonly SpanConstructor<TElement, TEnumerable>? ctor;
		private readonly AcceleratedRead<TElement>? acceleratedRead;
		private readonly MessagePackConverter<TElement>? unacceleratedReader;
		private readonly AcceleratedWrite<TElement> write;

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimitiveArrayConverter{TEnumerable, TElement}"/> class
		/// with an accelerated encoder and decoder.
		/// </summary>
		/// <param name="ctor">The constructor to pass the span of values to, if an array isn't sufficient.</param>
		/// <param name="read">The hardware-accelerated delegate used to decode values from msgpack.</param>
		/// <param name="write">The hardware-accelerated delegate used to encode values to msgpack.</param>
		internal PrimitiveArrayConverter(
			SpanConstructor<TElement, TEnumerable>? ctor,
			AcceleratedRead<TElement>? read,
			AcceleratedWrite<TElement> write)
		{
			this.ctor = ctor;
			this.acceleratedRead = read;
			this.write = write;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PrimitiveArrayConverter{TEnumerable, TElement}"/> class
		/// with an accelerated encoder and an ordinary decoder.
		/// </summary>
		/// <param name="ctor">The constructor to pass the span of values to, if an array isn't sufficient.</param>
		/// <param name="unacceleratedReader">The converter used to decode values from msgpack.</param>
		/// <param name="write">The hardware-accelerated delegate used to encode values to msgpack.</param>
		internal PrimitiveArrayConverter(
			SpanConstructor<TElement, TEnumerable>? ctor,
			MessagePackConverter<TElement>? unacceleratedReader,
			AcceleratedWrite<TElement> write)
		{
			this.ctor = ctor;
			this.unacceleratedReader = unacceleratedReader;
			this.write = write;
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
				if (this.acceleratedRead is not null)
				{
					Span<TElement> remainingElements = elements.AsSpan(0, count);
					ReadOnlySequence<byte> sequence = reader.ReadRaw(count);
					foreach (ReadOnlyMemory<byte> segment in sequence)
					{
						if (!this.acceleratedRead(ref remainingElements[0], in segment.Span[0], unchecked((nuint)segment.Length)))
						{
							throw new MessagePackSerializationException("Not all elements were boolean msgpack values.");
						}

						remainingElements = remainingElements[segment.Length..];
					}
				}
				else
				{
					for (int i = 0; i < count; i++)
					{
						elements[i] = this.unacceleratedReader!.Read(ref reader, context);
					}
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

			Assumes.True(TryGetSpan(value, out ReadOnlySpan<TElement> span));
			writer.WriteArrayHeader(span.Length);
			if (span.Length > 0)
			{
				Span<byte> buffer = writer.GetSpan(span.Length * MsgPackBufferLengthFactor);
				nuint writtenBytes = this.write(ref buffer[0], in span[0], unchecked((nuint)span.Length));
				writer.Advance(checked((int)writtenBytes));
			}
		}
	}
}
