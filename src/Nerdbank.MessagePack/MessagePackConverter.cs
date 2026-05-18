// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic, <see cref="object"/>-based base class for all message pack converters.
/// </summary>
public abstract class MessagePackConverter
{
	/// <summary>
	/// The largest capacity that a collection should be precreated with based on untrusted or streaming data.
	/// </summary>
	private const int MaxUntrustedCollectionPreallocation = 4096;

#if NET
	private static readonly int ArrayMaxLength = Array.MaxLength;
#else
	private static readonly int ArrayMaxLength = int.MaxValue; // an approximation that still guards against int overflow.
#endif

	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="WriteObjectAsync"/> and/or <see cref="ReadObjectAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public abstract bool PreferAsyncSerialization { get; }

	/// <summary>
	/// Gets the data type that this converter can serialize and deserialize.
	/// </summary>
	internal abstract Type DataType { get; }

	/// <summary>
	/// Serializes an instance of an object.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <remarks>
	/// Implementations of this method should not flush the writer.
	/// </remarks>
	public abstract void WriteObject(ref MessagePackWriter writer, object? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of an object.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract object? ReadObject(ref MessagePackReader reader, SerializationContext context);

	/// <inheritdoc cref="WriteObject"/>
	/// <returns>A task that tracks the asynchronous operation.</returns>
	public abstract ValueTask WriteObjectAsync(MessagePackAsyncWriter writer, object? value, SerializationContext context);

	/// <inheritdoc cref="ReadObject"/>
	public abstract ValueTask<object?> ReadObjectAsync(MessagePackAsyncReader reader, SerializationContext context);

	/// <inheritdoc cref="MessagePackConverter{T}.GetJsonSchema(JsonSchemaContext, ITypeShape)"/>
	public abstract JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value of the specified property can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="propertyShape">The shape of the property whose value is to be skipped to.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified property was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks><inheritdoc cref="SkipToIndexValueAsync(MessagePackAsyncReader, object?, SerializationContext)" path="/remarks"/></remarks>
	public abstract ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value of the specified property can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="propertyShape">The shape of the property whose value is to be skipped to.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified property was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks><inheritdoc cref="SkipToIndexValue(ref MessagePackReader, object?, SerializationContext)" path="/remarks"/></remarks>
	public abstract bool SkipToPropertyValue(ref MessagePackReader reader, IPropertyShape propertyShape, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value at the specified index can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="index">The key or index of the value to be retrieved.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified index was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This method is used by <see cref="MessagePackSerializer.DeserializePathEnumerableAsync{T, TElement}(System.IO.Pipelines.PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)"/>
	/// to skip to the starting position of a sequence that should be asynchronously enumerated.
	/// </remarks>
	public abstract ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context);

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value at the specified index can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="index">The key or index of the value to be retrieved.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified index was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This method may be used by <see cref="MessagePackSerializer.DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in MessagePackSerializer.DeserializePathOptions{T, TElement}, CancellationToken)"/>
	/// to skip to the starting position of the particular object to be deserialized.
	/// </remarks>
	public abstract bool SkipToIndexValue(ref MessagePackReader reader, object? index, SerializationContext context);

	/// <summary>
	/// Determines if a thrown exception should be wrapped with contextual information.
	/// </summary>
	/// <param name="ex">The thrown exception.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns><see langword="true" /> if the exception should be wrapped; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// We wrap all exceptions <em>except</em> <see cref="OperationCanceledException"/> if the cancellation token is cancelled.
	/// In other words, the only time we allow any exception to escape is when the operation was cancelled, because
	/// that is the intended behavior of cancellation tokens.
	/// </remarks>
	internal static bool ShouldWrapSerializationException(Exception ex, CancellationToken cancellationToken)
		=> ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested;

	/// <summary>
	/// Creates a type-specific error message for deserialization failures.
	/// </summary>
	/// <param name="objectType">The type of object being deserialized.</param>
	/// <param name="index">The index within an array of the element being processed.</param>
	/// <returns>A formatted error message.</returns>
	internal static string CreateFailReadingValueAtIndex(Type objectType, int index)
		=> $"Failed to deserialize a '{objectType.FullName}' at index {index}.";

	/// <summary>
	/// Creates a type-specific error message for serialization failures.
	/// </summary>
	/// <param name="objectType">The type of object being serialized.</param>
	/// <param name="index">The index within an array of the element being processed.</param>
	/// <returns>A formatted error message.</returns>
	internal static string CreateFailWritingValueAtIndex(Type objectType, int index)
		=> $"Failed to serialize a '{objectType.FullName}' at index {index}.";

	/// <summary>
	/// Just insurance that no external assembly can derive a concrete type from this type, except through the generic <see cref="MessagePackConverter{T}"/>.
	/// </summary>
	internal abstract void DerivationGuard();

	/// <summary>
	/// Resizes an array if necessary to store one more element.
	/// </summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="buffer">
	/// The last buffer received from this method.
	/// For initial calls, passing in <c>[]</c> is recommended.
	/// This may be returned to a shared pool if it is too small for the caller.
	/// Upon completion of this method, this will be set to the buffer that should be used for the next read operation.
	/// </param>
	/// <param name="initializedLength">The number of elements in <paramref name="buffer" /> that have been initialized.</param>
	/// <param name="finalLength">The expected length of the final array when all elements are initialized.</param>
	/// <param name="allowSlack"><see langword="true" /> to allow <paramref name="buffer"/> to be returned <em>larger</em> than <paramref name="finalLength"/>.</param>
	/// <param name="context">The serialization context.</param>
	/// <remarks>
	/// When the returned array is smaller than <paramref name="finalLength"/>,
	/// it will have come from a shared pool and will be returned to it when
	/// the caller returns for a larger array.
	/// </remarks>
	private protected static void Grow<T>([NotNull] ref T[] buffer, int initializedLength, int finalLength, bool allowSlack, in SerializationContext context)
	{
		Debug.Assert(finalLength > initializedLength, "The final length must be greater than the number of initialized elements.");

		if (finalLength == 0)
		{
			Debug.Assert(initializedLength == 0, "Initialized elements when final length is 0?");
			buffer = [];
			return;
		}

		int currentLength = buffer.Length;
		Debug.Assert(currentLength >= initializedLength, "Buffer is shorter than the alleged number of initialized elements.");

		int initializedPlus1 = initializedLength + 1;
		Debug.Assert(initializedPlus1 <= finalLength, "Caller should not have requested more elements than the final length.");

		// Return the buffer to the caller if it is already long enough.
		if (currentLength >= initializedPlus1)
		{
			// The only way the buffers length could meet or exceed requiredLength (which is always >=1)
			// is if it is non-null.
			return;
		}

		// The buffer is too small. We need to return a new buffer that can hold at least one more element.
		T[] newBuffer;
		int nextStepSize = context.IsTrustedData ? finalLength : (int)Math.Max(Math.Min(ArrayMaxLength, (long)currentLength * 2), MaxUntrustedCollectionPreallocation);
		if (nextStepSize < finalLength)
		{
			// Our target next size is smaller than the final length, so we can rent a buffer from the pool.
			newBuffer = ArrayPool<T>.Shared.Rent(nextStepSize);

			// But the pool *may* return a larger buffer, and if it is greater than the final length,
			// just return it and allocate an exact size array so the caller doesn't have to copy the
			// data to a final array.
			if (!allowSlack && newBuffer.Length > finalLength)
			{
				ArrayPool<T>.Shared.Return(newBuffer);
				newBuffer = new T[finalLength];
			}
		}
		else
		{
			// Our target next size is greater than or equal to the final length, so just allocate an array of the final length.
			newBuffer = allowSlack ? ArrayPool<T>.Shared.Rent(finalLength) : new T[finalLength];
		}

		// Copy the data and recycle the old buffer, if any.
		buffer.AsSpan(0, initializedLength).CopyTo(newBuffer);
		if (buffer is not [])
		{
			ArrayPool<T>.Shared.Return(buffer);
		}

		buffer = newBuffer;
	}

	/// <summary>
	/// Gets the initial capacity to allocate before reading elements from a streaming source.
	/// </summary>
	/// <param name="count">The element count declared by the messagepack header.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>A capacity that does not exceed <see cref="MaxUntrustedCollectionPreallocation" />.</returns>
	private protected static int GetCollectionInitialCapacity(int count, in SerializationContext context) => context.IsTrustedData ? count : Math.Min(count, MaxUntrustedCollectionPreallocation);
}
