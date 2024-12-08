// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Contains a bunch of converters for arrays of primitives.
/// </summary>
/// <remarks>
/// These aren't strictly necessary, but because we can predict their max encoded representation and embed the
/// direct reader/writer calls, we can avoid the overhead of many tiny calls to
/// <see cref="MessagePackWriter.GetSpan(int)"/> and <see cref="MessagePackWriter.Advance(int)"/>,
/// which speeds things up considerably.
/// </remarks>
internal static partial class ArraysOfPrimitivesConverters
{
	/// <summary>
	/// An abstract base class for converting arrays of primitive types.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of element.</typeparam>
	/// <param name="getEnumerable">The function that produces an <see cref="IEnumerable{T}"/> for a given <typeparamref name="TEnumerable"/>.</param>
	/// <param name="spanConstructor">The constructor for the enumerable type.</param>
	private abstract class PrimitiveArrayConverter<TEnumerable, TElement>(
		Func<TEnumerable, IEnumerable<TElement>> getEnumerable,
		SpanConstructor<TElement, TEnumerable>? spanConstructor) : MessagePackConverter<TEnumerable>
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

		/// <inheritdoc/>
		public override void Write(ref MessagePackWriter writer, in TEnumerable? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			int totalBytesWritten = 0;
			if (TryGetSpan(value, out ReadOnlySpan<TElement> values))
			{
				writer.WriteArrayHeader(values.Length);
				Span<byte> span = writer.GetSpan(values.Length * MsgPackBufferLengthFactor);
				for (int i = 0; i < values.Length; i++)
				{
					Assumes.True(this.TryWrite(span[totalBytesWritten..], values[i], out int justWritten));
					totalBytesWritten += justWritten;
				}
			}
			else
			{
				IEnumerable<TElement> enumerable = getEnumerable(value);
				if (PolyfillExtensions.TryGetNonEnumeratedCount(enumerable, out int count))
				{
					writer.WriteArrayHeader(count);
					Span<byte> span = writer.GetSpan(count * MsgPackBufferLengthFactor);
					foreach (TElement element in enumerable)
					{
						Assumes.True(this.TryWrite(span[totalBytesWritten..], element, out int justWritten));
						totalBytesWritten += justWritten;
					}
				}
				else
				{
					TElement[] array = enumerable.ToArray();
					writer.WriteArrayHeader(array.Length);
					Span<byte> span = writer.GetSpan(count * MsgPackBufferLengthFactor);
					for (int i = 0; i < array.Length; i++)
					{
						Assumes.True(this.TryWrite(span[totalBytesWritten..], array[i], out int justWritten));
						totalBytesWritten += justWritten;
					}
				}
			}

			writer.Advance(totalBytesWritten);
		}

		/// <inheritdoc/>
		public override TEnumerable? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return default;
			}

			int count = reader.ReadArrayHeader();
			if (count == 0)
			{
				return spanConstructor is null ? (TEnumerable)(object)Array.Empty<TElement>() : spanConstructor(default);
			}

			TElement[] elements = spanConstructor is null ? new TElement[count] : ArrayPool<TElement>.Shared.Rent(count);
			try
			{
				// PERF: When the memory is contiguous, we could use MessagePackPrimitives and advance an offset manually
				// to avoid calling some machinery within MessagePackReader to speed things up.
				for (int i = 0; i < count; i++)
				{
					elements[i] = this.Read(ref reader);
				}

				return spanConstructor is null ? (TEnumerable)(object)elements : spanConstructor(elements.AsSpan(0, count));
			}
			finally
			{
				if (spanConstructor is not null)
				{
					ArrayPool<TElement>.Shared.Return(elements);
				}
			}
		}

		/// <summary>
		/// When overridden by a derived class, invokes the appropriate <see cref="MessagePackPrimitives"/> <c>TryWrite</c> overload.
		/// </summary>
		/// <param name="destination">The buffer to write the msgpack-encoded value to.</param>
		/// <param name="value">The value to encode.</param>
		/// <param name="bytesWritten">Receives the number of actual bytes written to <paramref name="destination"/>, or that would have been written had there been sufficient space.</param>
		/// <returns>A value indicating whether <paramref name="destination"/> was large enough to write out the value.</returns>>
		protected abstract bool TryWrite(Span<byte> destination, TElement value, out int bytesWritten);

		/// <summary>
		/// Decodes a msgpack value.
		/// </summary>
		/// <param name="reader">The reader to use.</param>
		/// <returns>The decoded value.</returns>
		protected abstract TElement Read(ref MessagePackReader reader);

		private static bool TryGetSpan(TEnumerable enumerable, out ReadOnlySpan<TElement> span)
		{
			switch (enumerable)
			{
				case TElement[] array:
					span = array;
					return true;
#if NET
				case List<TElement> list:
					span = CollectionsMarshal.AsSpan(list);
					return true;
#endif
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
	}
}
