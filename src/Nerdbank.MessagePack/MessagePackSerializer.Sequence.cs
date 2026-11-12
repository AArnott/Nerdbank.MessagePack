// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads
#pragma warning disable RS0027 // optional parameter on a method with overloads

using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <content>
/// Asynchronous serialization of sequences.
/// </content>
public partial record MessagePackSerializer
{
	/// <summary>
	/// Serializes an asynchronous sequence as concatenated top-level MessagePack values.
	/// </summary>
	/// <typeparam name="T">The type of value to serialize.</typeparam>
	/// <param name="writer">
	/// The writer to use.
	/// This method commits bytes to <paramref name="writer"/> and flushes it when the unflushed buffer exceeds <see cref="SerializationContext.UnflushedBytesThreshold"/>, but it does not complete the writer.
	/// </param>
	/// <param name="values">The values to serialize. Each value, including <see langword="null"/>, becomes one top-level MessagePack value.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the asynchronous serialization.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/>, <paramref name="values"/>, or <paramref name="shape"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown when serialization of a value fails.</exception>
	/// <remarks>
	/// <para>
	/// The bytes written are the inverse of <see cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)"/>:
	/// an empty sequence writes nothing, and each subsequent value is a complete MessagePack structure with no array or map envelope around the sequence.
	/// A consumer can therefore read the result with <see cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)"/>.
	/// </para>
	/// <para>
	/// Values are written as they are produced. Bytes stay in a private buffer until <see cref="SerializationContext.UnflushedBytesThreshold"/> is exceeded, and that count is kept by this method, so a destination that does not report <see cref="PipeWriter.UnflushedBytes"/> still flushes and applies backpressure.
	/// Call <see cref="PipeWriter.FlushAsync(CancellationToken)"/> or <see cref="PipeWriter.CompleteAsync(Exception?)"/> after this method to publish any trailing bytes.
	/// </para>
	/// <para>
	/// Cancellation is observed before each value is written, including when the source yields another value after the token is canceled, and before a pending chunk is copied to <paramref name="writer"/>.
	/// If this method fails before that copy, the pending bytes are dropped.
	/// If the copy or the destination flush then fails, <paramref name="writer"/> may already hold that chunk and must be abandoned, because a <see cref="PipeWriter"/> cannot roll those bytes back.
	/// Bytes that were flushed successfully remain.
	/// </para>
	/// <para>
	/// MessagePack arrays store their length before any element, so an asynchronous sequence of unknown length cannot be written as one array without buffering it.
	/// Use <see cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)"/> when the length is known.
	/// <see cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)"/> of an <see cref="IEnumerable{T}"/> or <see cref="IAsyncEnumerable{T}"/> still buffers the values and writes one array.
	/// </para>
	/// </remarks>
	/// <inheritdoc cref="ThrowIfPreservingReferencesDuringEnumeration" path="/exception"/>
	public ValueTask SerializeEnumerableAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, ITypeShape<T> shape, CancellationToken cancellationToken = default)
		=> this.SerializeEnumerableCoreAsync(writer, values, shape, this.StartingContext, cancellationToken);

	/// <summary>
	/// Serializes an asynchronous sequence as concatenated top-level MessagePack values, using a caller-supplied starting context.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="writer"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='writer']"/></param>
	/// <param name="values"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='values']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="startingContext"><inheritdoc cref="SerializeObject(ref MessagePackWriter, object?, ITypeShape, SerializationContext)" path="/param[@name='startingContext']"/></param>
	/// <returns><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <inheritdoc cref="SerializeObject(ref MessagePackWriter, object?, ITypeShape, SerializationContext)" path="/exception"/>
	/// <inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/remarks"/>
	/// <inheritdoc cref="ThrowIfPreservingReferencesDuringEnumeration" path="/exception"/>
	[OverloadResolutionPriority(-1)] // prefer the CancellationToken overload when the caller passes `default`.
	public ValueTask SerializeEnumerableAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, ITypeShape<T> shape, SerializationContext startingContext)
	{
		startingContext.ThrowIfInitialized(nameof(startingContext));
		return this.SerializeEnumerableCoreAsync(writer, values, shape, startingContext, startingContext.CancellationToken);
	}

	/// <summary>
	/// Serializes an asynchronous sequence to a <see cref="Stream"/> as concatenated top-level MessagePack values.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="stream">The stream to write to. It is flushed but not disposed or closed.</param>
	/// <param name="values"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='values']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/>, <paramref name="values"/>, or <paramref name="shape"/> is <see langword="null"/>.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown when serialization of a value fails.</exception>
	/// <remarks>
	/// This overload adapts <paramref name="stream"/> to a <see cref="PipeWriter"/> and then calls <see cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)"/>.
	/// </remarks>
	/// <inheritdoc cref="ThrowIfPreservingReferencesDuringEnumeration" path="/exception"/>
	public async ValueTask SerializeEnumerableAsync<T>(Stream stream, IAsyncEnumerable<T?> values, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(stream);
		Requires.NotNull(values);
		Requires.NotNull(shape);

		PipeWriter pipeWriter = PipeWriter.Create(stream, PipeWriterOptions);
		try
		{
			await this.SerializeEnumerableAsync(pipeWriter, values, shape, cancellationToken).ConfigureAwait(false);
			await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// Completing with the exception releases buffers without flushing partial output.
			await pipeWriter.CompleteAsync(ex).ConfigureAwait(false);
			throw;
		}

		await pipeWriter.CompleteAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Serializes an asynchronous sequence as one MessagePack array.
	/// </summary>
	/// <typeparam name="T">The type of element to serialize.</typeparam>
	/// <param name="writer">
	/// The writer to use.
	/// This method commits bytes to <paramref name="writer"/> and flushes it when the unflushed buffer exceeds <see cref="SerializationContext.UnflushedBytesThreshold"/>, but it does not complete the writer.
	/// </param>
	/// <param name="values">The elements to serialize. The sequence must produce exactly <paramref name="count"/> elements.</param>
	/// <param name="count">The number of elements in <paramref name="values"/>. This value is written into the array header before any element.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the asynchronous serialization.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/>, <paramref name="values"/>, or <paramref name="shape"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled.</exception>
	/// <exception cref="MessagePackSerializationException">
	/// Thrown when serialization of an element fails, or when <paramref name="values"/> does not produce exactly <paramref name="count"/> elements.
	/// Elements already flushed before the failure remain on <paramref name="writer"/>.
	/// Bytes not yet copied to <paramref name="writer"/> are dropped.
	/// If a chunk was copied and flushing it failed, <paramref name="writer"/> must be abandoned because those bytes cannot be rolled back.
	/// </exception>
	/// <remarks>
	/// <para>
	/// A MessagePack array header contains the element count, so the length has to be known before the first element is written.
	/// This method writes that header and then each element as <paramref name="values"/> produces it, flushing when <see cref="SerializationContext.UnflushedBytesThreshold"/> is exceeded.
	/// The threshold is tracked by this method, including when <paramref name="writer"/> does not report <see cref="PipeWriter.UnflushedBytes"/>.
	/// Cancellation is observed before each element is written and before a pending chunk is copied to <paramref name="writer"/>.
	/// Bytes not yet copied are dropped when this method fails.
	/// If a chunk was copied and flushing it failed, <paramref name="writer"/> must be abandoned because those bytes cannot be rolled back.
	/// The result is one array and can be read with <see cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)"/> of <typeparamref name="T"/>[] or with <see cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)"/>.
	/// </para>
	/// <para>
	/// When the length is not known, <see cref="SerializeEnumerableAsync{T}(PipeWriter, IAsyncEnumerable{T}, ITypeShape{T}, CancellationToken)"/> writes each value as its own top-level structure.
	/// <see cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)"/> of an <see cref="IEnumerable{T}"/> or <see cref="IAsyncEnumerable{T}"/> still buffers every element and then writes one array.
	/// </para>
	/// <para>
	/// Reference preservation applies to the array as one object graph, using the same rules as <see cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)"/>.
	/// </para>
	/// </remarks>
	public ValueTask SerializeArrayAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, int count, ITypeShape<T> shape, CancellationToken cancellationToken = default)
		=> this.SerializeArrayCoreAsync(writer, values, count, shape, this.StartingContext, cancellationToken);

	/// <summary>
	/// Serializes an asynchronous sequence as one MessagePack array, using a caller-supplied starting context.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="writer"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='writer']"/></param>
	/// <param name="values"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='values']"/></param>
	/// <param name="count"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='count']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="startingContext"><inheritdoc cref="SerializeObject(ref MessagePackWriter, object?, ITypeShape, SerializationContext)" path="/param[@name='startingContext']"/></param>
	/// <returns><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <inheritdoc cref="SerializeObject(ref MessagePackWriter, object?, ITypeShape, SerializationContext)" path="/exception"/>
	/// <inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/remarks"/>
	[OverloadResolutionPriority(-1)] // prefer the CancellationToken overload when the caller passes `default`.
	public ValueTask SerializeArrayAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, int count, ITypeShape<T> shape, SerializationContext startingContext)
	{
		startingContext.ThrowIfInitialized(nameof(startingContext));
		return this.SerializeArrayCoreAsync(writer, values, count, shape, startingContext, startingContext.CancellationToken);
	}

	/// <summary>
	/// Serializes an asynchronous sequence to a <see cref="Stream"/> as one MessagePack array.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="stream">The stream to write to. It is flushed but not disposed or closed.</param>
	/// <param name="values"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='values']"/></param>
	/// <param name="count"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='count']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/>, <paramref name="values"/>, or <paramref name="shape"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <paramref name="cancellationToken"/> is canceled.</exception>
	/// <exception cref="MessagePackSerializationException">Thrown when serialization of an element fails, or when <paramref name="values"/> does not produce exactly <paramref name="count"/> elements.</exception>
	/// <remarks>
	/// This overload adapts <paramref name="stream"/> to a <see cref="PipeWriter"/> and then calls <see cref="SerializeArrayAsync{T}(PipeWriter, IAsyncEnumerable{T}, int, ITypeShape{T}, CancellationToken)"/>.
	/// </remarks>
	public async ValueTask SerializeArrayAsync<T>(Stream stream, IAsyncEnumerable<T?> values, int count, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(stream);
		Requires.NotNull(values);
		Requires.NotNull(shape);

		PipeWriter pipeWriter = PipeWriter.Create(stream, PipeWriterOptions);
		try
		{
			await this.SerializeArrayAsync(pipeWriter, values, count, shape, cancellationToken).ConfigureAwait(false);
			await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// Completing with the exception releases buffers without flushing partial output.
			await pipeWriter.CompleteAsync(ex).ConfigureAwait(false);
			throw;
		}

		await pipeWriter.CompleteAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Serializes an asynchronous sequence as concatenated top-level MessagePack values.
	/// </summary>
	/// <typeparam name="T">The type of value to serialize.</typeparam>
	/// <param name="writer">The writer to use.</param>
	/// <param name="values">The values to serialize.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="startingContext">The context to base the job on.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the asynchronous serialization.</returns>
	/// <exception cref="NotSupportedException">Thrown when <see cref="PreserveReferences"/> is not <see cref="ReferencePreservationMode.Off"/>.</exception>
	private async ValueTask SerializeEnumerableCoreAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, ITypeShape<T> shape, SerializationContext startingContext, CancellationToken cancellationToken)
	{
		Requires.NotNull(writer);
		Requires.NotNull(values);
		Requires.NotNull(shape);

		// Each value is an independent top-level structure. Reference ids would not round-trip through DeserializeEnumerableAsync.
		this.ThrowIfPreservingReferencesDuringEnumeration();

		// Stage bytes privately so a failure cannot leave a partial value pending on the caller's writer,
		// and so the flush threshold works when that writer cannot report an unflushed byte count.
		StagingPipeWriter staging = new(writer);
		try
		{
			try
			{
				using DisposableSerializationContext context = this.CreateSerializationContextCore(shape.Provider, startingContext, cancellationToken);
				MessagePackConverter<T> converter = this.ConverterCache.GetOrAddConverterValue(shape);
				MessagePackAsyncWriter asyncWriter = new(staging);
				int index = 0;
				await foreach (T? value in values.WithCancellation(context.Value.CancellationToken).ConfigureAwait(false))
				{
					// WithCancellation only asks the source to stop. Converters such as a null array skip the token, so check it here.
					context.Value.CancellationToken.ThrowIfCancellationRequested();

					try
					{
						// Pass a fresh copy so each top-level value starts at the root depth while sharing context state such as string interning.
						await converter.WriteAsync(asyncWriter, value, context.Value).ConfigureAwait(false);
					}
					catch (Exception ex) when (ShouldWrapSerializationException(ex, context.Value.CancellationToken))
					{
						throw new MessagePackSerializationException(MessagePackConverter.CreateFailWritingValueAtIndex(typeof(T), index), ex);
					}

					index++;

					// WriteAsync only observes bytes already committed to its writer. Small values stay in the private buffer until committed.
					asyncWriter.Flush();
					await asyncWriter.FlushIfAppropriateAsync(context.Value).ConfigureAwait(false);
				}

				asyncWriter.Flush();
				cancellationToken.ThrowIfCancellationRequested();
				staging.CommitTail();
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
			{
				throw new MessagePackSerializationException("An error occurred during serialization.", ex);
			}
		}
		finally
		{
			staging.Discard();
		}
	}

	/// <summary>
	/// Serializes an asynchronous sequence as one MessagePack array.
	/// </summary>
	/// <typeparam name="T">The type of element to serialize.</typeparam>
	/// <param name="writer">The writer to use.</param>
	/// <param name="values">The elements to serialize.</param>
	/// <param name="count">The number of elements <paramref name="values"/> must produce.</param>
	/// <param name="shape">The shape of <typeparamref name="T"/>.</param>
	/// <param name="startingContext">The context to base the job on.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that tracks the asynchronous serialization.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
	private async ValueTask SerializeArrayCoreAsync<T>(PipeWriter writer, IAsyncEnumerable<T?> values, int count, ITypeShape<T> shape, SerializationContext startingContext, CancellationToken cancellationToken)
	{
		Requires.NotNull(writer);
		Requires.NotNull(values);
		Requires.NotNull(shape);
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count), count, "The array length must be greater than or equal to zero.");
		}

		// Stage bytes privately so a failure cannot leave a partial array pending on the caller's writer,
		// and so the flush threshold works when that writer cannot report an unflushed byte count.
		StagingPipeWriter staging = new(writer);
		try
		{
			try
			{
				using DisposableSerializationContext context = this.CreateSerializationContextCore(shape.Provider, startingContext, cancellationToken);
				MessagePackConverter<T> converter = this.ConverterCache.GetOrAddConverterValue(shape);
				MessagePackAsyncWriter asyncWriter = new(staging);

				// A reference-preserving reader assigns the first id to the array object. Reserve it so element ids match.
				object? containerToken = context.Value.ReferenceEqualityTracker?.BeginWritingContainer();
				try
				{
					// One array is one object graph, so every element shares this depth and any reference-preservation state.
					SerializationContext elementContext = context.Value;
					elementContext.DepthStep();
					asyncWriter.WriteArrayHeader(count);

					int written = 0;
					await foreach (T? value in values.WithCancellation(elementContext.CancellationToken).ConfigureAwait(false))
					{
						// WithCancellation only asks the source to stop. Converters such as a null array skip the token, so check it here.
						elementContext.CancellationToken.ThrowIfCancellationRequested();

						if (written == count)
						{
							throw new InvalidOperationException($"The sequence produced more elements than the declared array length of {count}.");
						}

						try
						{
							await converter.WriteAsync(asyncWriter, value, elementContext).ConfigureAwait(false);
						}
						catch (Exception ex) when (ShouldWrapSerializationException(ex, elementContext.CancellationToken))
						{
							throw new MessagePackSerializationException(MessagePackConverter.CreateFailWritingValueAtIndex(typeof(T), written), ex);
						}

						written++;

						// WriteAsync only observes bytes already committed to its writer. Small values stay in the private buffer until committed.
						asyncWriter.Flush();
						await asyncWriter.FlushIfAppropriateAsync(elementContext).ConfigureAwait(false);
					}

					if (written != count)
					{
						throw new InvalidOperationException($"The sequence produced {written} elements, fewer than the declared array length of {count}.");
					}

					asyncWriter.Flush();
				}
				finally
				{
					if (containerToken is not null)
					{
						context.Value.ReferenceEqualityTracker!.CompleteWritingContainer(containerToken);
					}
				}

				cancellationToken.ThrowIfCancellationRequested();
				staging.CommitTail();
			}
			catch (Exception ex) when (ShouldWrapSerializationException(ex, cancellationToken))
			{
				throw new MessagePackSerializationException("An error occurred during serialization.", ex);
			}
		}
		finally
		{
			staging.Discard();
		}
	}

	/// <summary>
	/// Buffers bytes until <see cref="FlushAsync(CancellationToken)"/> and reports that buffered length as <see cref="UnflushedBytes"/>.
	/// </summary>
	/// <param name="destination">The writer that receives bytes when they are flushed or when <see cref="CommitTail"/> is called.</param>
	private sealed class StagingPipeWriter(PipeWriter destination) : PipeWriter
	{
		private readonly Sequence<byte> staging = new();

		/// <inheritdoc/>
		public override bool CanGetUnflushedBytes => true;

		/// <inheritdoc/>
		public override long UnflushedBytes => this.staging.Length;

		/// <inheritdoc/>
		public override void Advance(int bytes) => this.staging.Advance(bytes);

		/// <inheritdoc/>
		public override Memory<byte> GetMemory(int sizeHint = 0) => this.staging.GetMemory(sizeHint);

		/// <inheritdoc/>
		public override Span<byte> GetSpan(int sizeHint = 0) => this.staging.GetSpan(sizeHint);

		/// <inheritdoc/>
		public override void CancelPendingFlush() => destination.CancelPendingFlush();

		/// <inheritdoc/>
		public override void Complete(Exception? exception = null) => this.Discard();

		/// <inheritdoc/>
		public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
		{
			// Leave the bytes staged when the caller is already canceled so Discard can drop them.
			// Once they are copied, a failed destination flush cannot take them back.
			cancellationToken.ThrowIfCancellationRequested();
			this.CopyToDestination();
			return destination.FlushAsync(cancellationToken);
		}

		/// <summary>
		/// Copies buffered bytes to the destination without flushing it.
		/// </summary>
		public void CommitTail() => this.CopyToDestination();

		/// <summary>
		/// Drops buffered bytes without writing them to the destination.
		/// </summary>
		public void Discard() => this.staging.Reset();

		private void CopyToDestination()
		{
			ReadOnlySequence<byte> pending = this.staging;
			if (pending.IsEmpty)
			{
				return;
			}

			foreach (ReadOnlyMemory<byte> segment in pending)
			{
				destination.Write(segment.Span);
			}

			this.staging.Reset();
		}
	}
}
