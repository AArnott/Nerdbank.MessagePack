// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1402 // multiple types
#pragma warning disable SA1403 // multiple namespaces

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft;
using PolyType.Utilities;

namespace ShapeShift
{
	/// <content>
	/// Utility methods to help make up for cross-targeting support.
	/// </content>
	internal static partial class PolyfillExtensions
	{
		internal static bool TryGetNonEnumeratedCount<TSource>(this IEnumerable<TSource> source, out int count)
		{
#if NET
			return Enumerable.TryGetNonEnumeratedCount(source, out count);
#else
			Requires.NotNull(source);

			switch (source)
			{
				case ICollection<TSource> collection:
					count = collection.Count;
					return true;
				case System.Collections.ICollection collection:
					count = collection.Count;
					return true;
				default:
					count = 0;
					return false;
			}
#endif
		}

		internal static object GetOrAddOrThrow(this MultiProviderTypeCache cache, Type type, ITypeShapeProvider provider)
			=> cache.GetOrAdd(type, provider) ?? throw ThrowMissingTypeShape(type, provider);

		internal static ITypeShape GetShapeOrThrow(this ITypeShapeProvider provider, Type type)
			=> provider.GetShape(type) ?? throw ThrowMissingTypeShape(type, provider);

		private static Exception ThrowMissingTypeShape(Type type, ITypeShapeProvider provider)
			=> new ArgumentException($"The {provider.GetType().FullName} provider had no type shape for {type.FullName}.", nameof(provider));
	}

#if !NET
	/// <content>
	/// Polyfills specifically for .NET Standard targeting.
	/// </content>
	internal static partial class PolyfillExtensions
	{
		internal static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> value)
		{
			if (value.IsEmpty)
			{
				return 0;
			}

			fixed (char* pValue = value)
			{
				return encoding.GetByteCount(pValue, value.Length);
			}
		}

		internal static unsafe int GetChars(this Encoding encoding, ReadOnlySpan<byte> source, Span<char> destination)
		{
			if (source.IsEmpty)
			{
				return 0;
			}

			fixed (byte* pSource = source)
			{
				fixed (char* pDestination = destination)
				{
					return encoding.GetChars(pSource, source.Length, pDestination, destination.Length);
				}
			}
		}

		internal static unsafe int GetChars(this Encoding encoding, ReadOnlySequence<byte> source, Span<char> destination)
		{
			if (source.IsSingleSegment)
			{
				return GetChars(encoding, source.First.Span, destination);
			}

			Decoder decoder = encoding.GetDecoder();
			int charsWritten = 0;
			bool completed = true;
			foreach (ReadOnlyMemory<byte> sourceSegment in source)
			{
				if (sourceSegment.IsEmpty)
				{
					continue;
				}

				fixed (byte* pSource = sourceSegment.Span)
				{
					fixed (char* pDestination = destination)
					{
						decoder.Convert(pSource, sourceSegment.Length, pDestination, destination.Length, false, out _, out int charsUsed, out completed);
						charsWritten += charsUsed;
						destination = destination[charsUsed..];
					}
				}
			}

			if (!completed)
			{
				fixed (char* pDest = destination)
				{
					decoder.Convert(null, 0, pDest, destination.Length, flush: true, out _, out int charsUsed, out _);
					charsWritten += charsUsed;
				}
			}

			return charsWritten;
		}

		internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> source, Span<byte> destination)
		{
			if (source.IsEmpty)
			{
				return 0;
			}

			fixed (char* pSource = source)
			{
				fixed (byte* pDestination = destination)
				{
					return encoding.GetBytes(pSource, source.Length, pDestination, destination.Length);
				}
			}
		}

		internal static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
		{
			if (bytes.IsEmpty)
			{
				return string.Empty;
			}

			fixed (byte* pBytes = bytes)
			{
				return encoding.GetString(pBytes, bytes.Length);
			}
		}

		internal static unsafe string GetString(this Encoding encoding, in ReadOnlySequence<byte> bytes)
		{
			if (bytes.IsSingleSegment)
			{
				return encoding.GetString(bytes.First.Span);
			}
			else
			{
				byte[] buffer = ArrayPool<byte>.Shared.Rent(checked((int)bytes.Length));
				try
				{
					bytes.CopyTo(buffer);
					return encoding.GetString(buffer);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
		}

		/// <summary>
		/// Reads from the stream into a memory buffer.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="buffer">The buffer to read directly into.</param>
		/// <returns>The number of bytes actually read.</returns>
		internal static int Read(this Stream stream, Span<byte> buffer)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				int bytesRead = stream.Read(array, 0, buffer.Length);
				new Span<byte>(array, 0, bytesRead).CopyTo(buffer);
				return bytesRead;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
			}
		}

		/// <summary>
		/// Reads from the stream into a memory buffer.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="buffer">The buffer to read directly into.</param>
		/// <param name="cancellationToken">A cancellation token.</param>
		/// <returns>The number of bytes actually read.</returns>
		/// <devremarks>
		/// This method shamelessly copied from the .NET Core 2.1 Stream class: https://github.com/dotnet/coreclr/blob/a113b1c803783c9d64f1f0e946ff9a853e3bc140/src/System.Private.CoreLib/shared/System/IO/Stream.cs#L366-L391.
		/// </devremarks>
		internal static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
			{
				return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
			}
			else
			{
				byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
				return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

				async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
				{
					try
					{
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks -- it's actually from our parent context.
						int result = await readTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
						new Span<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
						return result;
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(localBuffer);
					}
				}
			}
		}

		internal static bool TryWriteBytes(this Guid value, Span<byte> destination)
		{
			if (destination.Length < 16)
			{
				return false;
			}

			if (BitConverter.IsLittleEndian)
			{
				MemoryMarshal.TryWrite(destination, ref value);
			}
			else
			{
				// slower path for BigEndian
				Span<GuidBits> guidSpan = stackalloc GuidBits[1];
				guidSpan[0] = value;
				GuidBits endianSwitched = new(MemoryMarshal.AsBytes(guidSpan), false);
				MemoryMarshal.TryWrite(destination, ref endianSwitched);
			}

			return true;
		}

		internal static Guid CreateGuid(ReadOnlySpan<byte> bytes) => new GuidBits(bytes, bigEndian: false);

		internal static bool IsAssignableTo(this Type left, Type right) => right.IsAssignableFrom(left);

		internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (!dictionary.ContainsKey(key))
			{
				dictionary.Add(key, value);
				return true;
			}

			return false;
		}

		internal static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T value)
		{
			if (stack.Count > 0)
			{
				value = stack.Pop();
				return true;
			}

			value = default;
			return false;
		}

		internal static Exception ThrowNotSupportedOnNETFramework() => throw new PlatformNotSupportedException("This functionality is only supported on .NET.");

		private readonly struct GuidBits
		{
			private readonly int a;
			private readonly short b;
			private readonly short c;
#pragma warning disable CS0169 // The field is never used
			private readonly byte d;
			private readonly byte e;
			private readonly byte f;
			private readonly byte g;
			private readonly byte h;
			private readonly byte i;
			private readonly byte j;
			private readonly byte k;
#pragma warning restore CS0169 // The field is never used

			internal GuidBits(ReadOnlySpan<byte> b, bool bigEndian = false)
			{
				if (b.Length != 16)
				{
					throw new ArgumentException();
				}

				this = MemoryMarshal.Read<GuidBits>(b);

				if (bigEndian == BitConverter.IsLittleEndian)
				{
					this.a = BinaryPrimitives.ReverseEndianness(this.a);
					this.b = BinaryPrimitives.ReverseEndianness(this.b);
					this.c = BinaryPrimitives.ReverseEndianness(this.c);
				}
			}

			public static implicit operator Guid(GuidBits value) => Unsafe.As<GuidBits, Guid>(ref value);

			public static implicit operator GuidBits(Guid value) => Unsafe.As<Guid, GuidBits>(ref value);
		}
	}
#endif
}

#if !NET

namespace System.Diagnostics
{
	internal class UnreachableException : Exception
	{
		internal UnreachableException()
			: base("This code path should be unreachable.")
		{
		}
	}
}

#endif
