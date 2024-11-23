// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
	/// A hardware-accelerated converter for an array of <see cref="bool"/> values.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of the enumerable to be converted.</typeparam>
	/// <typeparam name="TElement">The type of element to be converted.</typeparam>
	internal class PrimitiveArrayConverter<TEnumerable, TElement> : MessagePackConverter<TEnumerable>
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
