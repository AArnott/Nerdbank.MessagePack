// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Hardware-accelerated converters for arrays of various primitive types.
/// </summary>
internal static class HardwareAccelerated
{
	private enum SpanConstructorKind
	{
		Array,
		List,
		ReadOnlyMemory,
		Memory,
	}

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
			converter = null;
			return false;
		}

		if (typeof(TElement) == typeof(bool))
		{
			converter = new BoolArrayConverter<TEnumerable>(spanConstructorKind);
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

		converter = null;
		return false;
	}

	private static ref TElement GetReferenceAndLength<TEnumerable, TElement>(SpanConstructorKind spanConstructorKind, TEnumerable enumerable, out int length)
		where TElement : unmanaged
	{
		switch (spanConstructorKind)
		{
			case SpanConstructorKind.Array:
				TElement[] array = Unsafe.As<TEnumerable, TElement[]>(ref enumerable);
				length = array.Length;
				return ref MemoryMarshal.GetArrayDataReference(array);
			case SpanConstructorKind.List:
				List<TElement> list = Unsafe.As<TEnumerable, List<TElement>>(ref enumerable);
				length = list.Count;
				return ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(list));
			case SpanConstructorKind.ReadOnlyMemory:
				ReadOnlyMemory<TElement> rom = Unsafe.As<TEnumerable, ReadOnlyMemory<TElement>>(ref enumerable);
				length = rom.Length;
				return ref MemoryMarshal.GetReference(rom.Span);
			default:
				Memory<TElement> mem = Unsafe.As<TEnumerable, Memory<TElement>>(ref enumerable);
				length = mem.Length;
				return ref MemoryMarshal.GetReference(mem.Span);
		}
	}

	private static TEnumerable GetEmptyEnumerable<TEnumerable, TElement>(SpanConstructorKind spanConstructorKind)
		where TElement : unmanaged
	{
		switch (spanConstructorKind)
		{
			case SpanConstructorKind.Array:
				return (TEnumerable)(object)Array.Empty<TElement>();
			case SpanConstructorKind.List:
				return (TEnumerable)(object)new List<TElement>();
			case SpanConstructorKind.ReadOnlyMemory:
				ReadOnlyMemory<TElement> rom = ReadOnlyMemory<TElement>.Empty;
				return Unsafe.As<ReadOnlyMemory<TElement>, TEnumerable>(ref rom)!;
			default:
				Memory<TElement> mem = Memory<TElement>.Empty;
				return Unsafe.As<Memory<TElement>, TEnumerable>(ref mem)!;
		}
	}

	/// <summary>
	/// Read/Write methods with Hardware Intrinsics for primitive msgpack codes.
	/// </summary>
	private static class MessagePackPrimitiveSpanUtility
	{
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

		private static nuint Write(ref byte output, ref short input, nuint inputLength)
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
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref int input, nuint inputLength)
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
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)) : unchecked((ushort)temp));
								outputOffset += 3U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((short)temp)) : unchecked((short)temp));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref long input, nuint inputLength)
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
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)) : unchecked((ushort)temp));
								outputOffset += 3U;
								break;
							case <= uint.MaxValue:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((uint)temp)) : unchecked((uint)temp));
								outputOffset += 5U;
								break;
							default:
								Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
								Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((short)temp)) : unchecked((short)temp));
						outputOffset += 3U;
						break;
					case >= int.MinValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((int)temp)) : unchecked((int)temp));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.Int64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref ushort input, nuint inputLength)
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 3U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref uint input, nuint inputLength)
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)) : unchecked((ushort)temp));
						outputOffset += 3U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 5U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref ulong input, nuint inputLength)
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
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((ushort)temp)) : unchecked((ushort)temp));
						outputOffset += 3U;
						break;
					case <= uint.MaxValue:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt32;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(unchecked((uint)temp)) : unchecked((uint)temp));
						outputOffset += 5U;
						break;
					default:
						Unsafe.Add(ref output, outputOffset) = MessagePackCode.UInt64;
						Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
						outputOffset += 9U;
						break;
				}
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref float input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float32;
				uint temp = Unsafe.BitCast<float, uint>(Unsafe.Add(ref input, inputOffset));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
				outputOffset += 5U;
			}

			return outputOffset;
		}

		private static nuint Write(ref byte output, ref double input, nuint length)
		{
			nuint outputOffset = 0;
			for (nuint inputOffset = 0; inputOffset < length; inputOffset++)
			{
				Unsafe.Add(ref output, outputOffset) = MessagePackCode.Float64;
				ulong temp = Unsafe.BitCast<double, ulong>(Unsafe.Add(ref input, inputOffset));
				Unsafe.WriteUnaligned(ref Unsafe.Add(ref output, outputOffset + 1U), BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(temp) : temp);
				outputOffset += 9U;
			}

			return outputOffset;
		}
	}

	private sealed class BoolArrayConverter<TEnumerable>(SpanConstructorKind spanConstructorKind) : MessagePackConverter<TEnumerable>
	{
		private readonly SpanConstructorKind spanConstructorKind = spanConstructorKind;

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
				return GetEmptyEnumerable<TEnumerable, bool>(this.spanConstructorKind);
			}

			TEnumerable enumerable;
			Span<bool> span;
			switch (this.spanConstructorKind)
			{
				case SpanConstructorKind.Array:
					{
						var array = new bool[count];
						enumerable = (TEnumerable)(object)array;
						span = new(array);
					}

					break;
				case SpanConstructorKind.List:
					{
						var list = new List<bool>(count);
						CollectionsMarshal.SetCount(list, count);
						enumerable = (TEnumerable)(object)list;
						span = CollectionsMarshal.AsSpan(list);
					}

					break;
				case SpanConstructorKind.ReadOnlyMemory:
					{
						var array = new bool[count];
						Unsafe.SkipInit(out enumerable);
						Unsafe.As<TEnumerable, ReadOnlyMemory<bool>>(ref enumerable) = new ReadOnlyMemory<bool>(array);
						span = new(array);
					}

					break;
				default:
					{
						var array = new bool[count];
						Unsafe.SkipInit(out enumerable);
						Unsafe.As<TEnumerable, Memory<bool>>(ref enumerable) = new Memory<bool>(array);
						span = new(array);
					}

					break;
			}

			ReadOnlySequence<byte> sequence = reader.ReadRaw(count);
			foreach (ReadOnlyMemory<byte> segment in sequence)
			{
				if (!MessagePackPrimitiveSpanUtility.Read(ref MemoryMarshal.GetReference(span), in MemoryMarshal.GetReference(segment.Span), segment.Length))
				{
					throw new MessagePackSerializationException("Not all elements were boolean msgpack values.");
				}

				span = span[segment.Length..];
			}

			return enumerable;
		}

		public override void Write(ref MessagePackWriter writer, in TEnumerable? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			context.DepthStep();
			ref bool reference = ref GetReferenceAndLength<TEnumerable, bool>(this.spanConstructorKind, value, out int length);
			if (Unsafe.IsNullRef(ref reference))
			{
				writer.WriteArrayHeader(0);
				return;
			}

			writer.WriteArrayHeader(length);
			if (length <= 0)
			{
				return;
			}

			Span<byte> buffer = writer.GetSpan(length);
			nuint writtenBytes = MessagePackPrimitiveSpanUtility.Write(ref MemoryMarshal.GetReference(buffer), in reference, unchecked((nuint)length));
			writer.Advance(unchecked((int)writtenBytes));
			reference = ref Unsafe.Add(ref reference, length);
		}

		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject()
				{
					["type"] = "boolean",
				},
			};
	}

	/// <summary>
	/// A hardware-accelerated converter for an array of <see cref="bool"/> values.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of the enumerable to be converted.</typeparam>
	/// <typeparam name="TElement">The type of element to be converted.</typeparam>
	private sealed class PrimitiveArrayConverter<TEnumerable, TElement>(SpanConstructorKind spanConstructorKind) : MessagePackConverter<TEnumerable>
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

		private readonly SpanConstructorKind spanConstructorKind = spanConstructorKind;

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
				return GetEmptyEnumerable<TEnumerable, TElement>(this.spanConstructorKind);
			}

			TEnumerable enumerable;
			Span<TElement> span;
			switch (this.spanConstructorKind)
			{
				case SpanConstructorKind.Array:
					{
						var array = new TElement[count];
						enumerable = (TEnumerable)(object)array;
						span = new(array);
					}

					break;
				case SpanConstructorKind.List:
					{
						var list = new List<TElement>(count);
						CollectionsMarshal.SetCount(list, count);
						enumerable = (TEnumerable)(object)list;
						span = CollectionsMarshal.AsSpan(list);
					}

					break;
				case SpanConstructorKind.ReadOnlyMemory:
					{
						var array = new TElement[count];
						Unsafe.SkipInit(out enumerable);
						Unsafe.As<TEnumerable, ReadOnlyMemory<TElement>>(ref enumerable) = new ReadOnlyMemory<TElement>(array);
						span = new(array);
					}

					break;
				default:
					{
						var array = new TElement[count];
						Unsafe.SkipInit(out enumerable);
						Unsafe.As<TEnumerable, Memory<TElement>>(ref enumerable) = new Memory<TElement>(array);
						span = new(array);
					}

					break;
			}

			for (int i = 0; i < span.Length; i++)
			{
				if (typeof(TElement) == typeof(ushort))
				{
					Unsafe.As<TElement, ushort>(ref span[i]) = reader.ReadUInt16();
				}
				else if (typeof(TElement) == typeof(uint))
				{
					Unsafe.As<TElement, uint>(ref span[i]) = reader.ReadUInt32();
				}
				else if (typeof(TElement) == typeof(ulong))
				{
					Unsafe.As<TElement, ulong>(ref span[i]) = reader.ReadUInt64();
				}
				else if (typeof(TElement) == typeof(sbyte))
				{
					Unsafe.As<TElement, sbyte>(ref span[i]) = reader.ReadSByte();
				}
				else if (typeof(TElement) == typeof(short))
				{
					Unsafe.As<TElement, short>(ref span[i]) = reader.ReadInt16();
				}
				else if (typeof(TElement) == typeof(int))
				{
					Unsafe.As<TElement, int>(ref span[i]) = reader.ReadInt32();
				}
				else if (typeof(TElement) == typeof(long))
				{
					Unsafe.As<TElement, long>(ref span[i]) = reader.ReadInt64();
				}
				else if (typeof(TElement) == typeof(float))
				{
					Unsafe.As<TElement, float>(ref span[i]) = reader.ReadSingle();
				}
				else
				{
					Unsafe.As<TElement, double>(ref span[i]) = reader.ReadDouble();
				}
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
			ref TElement reference = ref GetReferenceAndLength<TEnumerable, TElement>(this.spanConstructorKind, value, out int length);
			writer.WriteArrayHeader(length);
			if (length == 0 || Unsafe.IsNullRef(ref reference))
			{
				return;
			}

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

		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = context.GetJsonSchema(((IEnumerableTypeShape<TEnumerable, TElement>)typeShape).ElementType),
			};
	}
}
