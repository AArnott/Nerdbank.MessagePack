// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads
#pragma warning disable RS0027 // optional parameter on a method with overloads

using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

public partial record MessagePackSerializer
{
	private static readonly StreamPipeWriterOptions PipeWriterOptions = new(MemoryPool<byte>.Shared, leaveOpen: true);
	private static readonly StreamPipeReaderOptions PipeReaderOptions = new(MemoryPool<byte>.Shared, leaveOpen: true);

	/// <summary>
	/// A thread-local, recyclable array that may be used for short bursts of code.
	/// </summary>
	[ThreadStatic]
	private static byte[]? scratchArray;

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	/// <returns>A byte array containing the serialized msgpack.</returns>
	public byte[] Serialize<T>(in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		// Although the static array is thread-local, we still want to null it out while using it
		// to avoid any potential issues with re-entrancy due to a converter that makes a (bad) top-level call to the serializer.
		(byte[] array, scratchArray) = (scratchArray ?? new byte[65536], null);
		try
		{
			MessagePackWriter writer = new(SequencePool<byte>.Shared, array);
			this.Serialize(ref writer, value, shape, cancellationToken);
			return writer.FlushAndGetArray();
		}
		finally
		{
			scratchArray = array;
		}
	}

	/// <inheritdoc cref="SerializeObject(ref MessagePackWriter, object, ITypeShape, CancellationToken)"/>
	/// <returns>A byte array containing the serialized msgpack.</returns>
	public byte[] SerializeObject(object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		// Although the static array is thread-local, we still want to null it out while using it
		// to avoid any potential issues with re-entrancy due to a converter that makes a (bad) top-level call to the serializer.
		(byte[] array, scratchArray) = (scratchArray ?? new byte[65536], null);
		try
		{
			MessagePackWriter writer = new(SequencePool<byte>.Shared, array);
			this.SerializeObject(ref writer, value, shape, cancellationToken);
			return writer.FlushAndGetArray();
		}
		finally
		{
			scratchArray = array;
		}
	}

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	public void Serialize<T>(IBufferWriter<byte> writer, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.Serialize(ref msgpackWriter, value, shape, cancellationToken);
		msgpackWriter.Flush();
	}

	/// <inheritdoc cref="SerializeObject(ref MessagePackWriter, object, ITypeShape, CancellationToken)"/>
	public void SerializeObject(IBufferWriter<byte> writer, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		MessagePackWriter msgpackWriter = new(writer);
		this.SerializeObject(ref msgpackWriter, value, shape, cancellationToken);
		msgpackWriter.Flush();
	}

	/// <inheritdoc cref="Serialize{T}(ref MessagePackWriter, in T, ITypeShape{T}, CancellationToken)" />
	/// <param name="stream">The stream to write to.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public void Serialize<T>(Stream stream, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		Requires.NotNull(stream);
		this.Serialize(new StreamBufferWriter(stream), value, shape, cancellationToken);
	}

	/// <inheritdoc cref="SerializeObject(ref MessagePackWriter, object, ITypeShape, CancellationToken)"/>
	/// <param name="stream">The stream to write to.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public void SerializeObject(Stream stream, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		Requires.NotNull(stream);
		this.SerializeObject(new StreamBufferWriter(stream), value, shape, cancellationToken);
	}

	/// <summary>
	/// Serializes a value to a <see cref="Stream"/>.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='value']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public async ValueTask SerializeAsync<T>(Stream stream, T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		// Fast path for MemoryStream.
		if (stream is MemoryStream)
		{
			this.Serialize(stream, value, shape, cancellationToken);
			return;
		}

		PipeWriter pipeWriter = PipeWriter.Create(stream, PipeWriterOptions);
		await this.SerializeAsync(pipeWriter, value, shape, cancellationToken).ConfigureAwait(false);
		await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
		await pipeWriter.CompleteAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Serializes a value to a <see cref="Stream"/>.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='value']"/></param>
	/// <param name="shape"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public async ValueTask SerializeObjectAsync(Stream stream, object? value, ITypeShape shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		// Fast path for MemoryStream.
		if (stream is MemoryStream)
		{
			this.SerializeObject(stream, value, shape, cancellationToken);
			return;
		}

		PipeWriter pipeWriter = PipeWriter.Create(stream, PipeWriterOptions);
		await this.SerializeObjectAsync(pipeWriter, value, shape, cancellationToken).ConfigureAwait(false);
		await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
		await pipeWriter.CompleteAsync().ConfigureAwait(false);
	}

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ITypeShape{T}, CancellationToken)"/>
	public T? Deserialize<T>(ReadOnlyMemory<byte> buffer, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize(ref reader, shape, cancellationToken);
	}

	/// <inheritdoc cref="DeserializeObject(ref MessagePackReader, ITypeShape, CancellationToken)"/>
	public object? DeserializeObject(ReadOnlyMemory<byte> buffer, ITypeShape shape, CancellationToken cancellationToken = default)
	{
		MessagePackReader reader = new(buffer);
		return this.DeserializeObject(ref reader, shape, cancellationToken);
	}

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public T? Deserialize<T>(scoped in ReadOnlySequence<byte> buffer, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		MessagePackReader reader = new(buffer);
		return this.Deserialize(ref reader, shape, cancellationToken);
	}

	/// <inheritdoc cref="DeserializeObject(ref MessagePackReader, ITypeShape, CancellationToken)"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public object? DeserializeObject(scoped in ReadOnlySequence<byte> buffer, ITypeShape shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		MessagePackReader reader = new(buffer);
		return this.DeserializeObject(ref reader, shape, cancellationToken);
	}

	/// <inheritdoc cref="Deserialize{T}(ref MessagePackReader, ITypeShape{T}, CancellationToken)" />
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <remarks>
	/// <para>
	/// The implementation of this method currently is to buffer the entire remaining content of the <paramref name="stream"/> into memory before deserializing.
	/// This is for simplicity and perf reasons.
	/// Callers should only provide streams that are known to be small enough to fit in memory and contain only the msgpack content intended to deserialize in this call.
	/// </para>
	/// <para>
	/// If you need to deserialize only a portion of the <paramref name="stream"/>, convert it to a <see cref="PipeReader" /> first
	/// (you may use <see cref="PipeReader.Create(Stream, StreamPipeReaderOptions)" />)
	/// use a deserialize overload that takes a <see cref="PipeReader" />,
	/// and <em>only</em> interact with the stream using the <see cref="PipeReader" /> thereafter.
	/// Or to keep deserialization synchronous, read the stream into a memory buffer first,
	/// create a <see cref="MessagePackReader" /> based on that memory buffer,
	/// and use the deserialize overload that accepts <see cref="MessagePackReader" />.
	/// </para>
	/// </remarks>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public T? Deserialize<T>(Stream stream, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		Requires.NotNull(stream);

		// Fast path for MemoryStream.
		if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> buffer))
		{
			// Account for the stream's current position
			int offset = buffer.Offset + (int)ms.Position;
			int count = buffer.Count - (int)ms.Position;
			return this.Deserialize(buffer.AsMemory(offset, count), shape, cancellationToken);
		}
		else
		{
			// We don't have a streaming msgpack reader, so buffer it all into memory instead and read from there.
			using SequencePool<byte>.Rental rental = SequencePool<byte>.Shared.Rent();
			int bytesLastRead;
			do
			{
				Span<byte> span = rental.Value.GetSpan(0);
				bytesLastRead = stream.Read(span);
				rental.Value.Advance(bytesLastRead);
			}
			while (bytesLastRead > 0);

			return this.Deserialize(rental.Value, shape, cancellationToken);
		}
	}

	/// <inheritdoc cref="DeserializeObject(ref MessagePackReader, ITypeShape, CancellationToken)"/>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" path="/remarks" />
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public object? DeserializeObject(Stream stream, ITypeShape shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		Requires.NotNull(stream);

		// Fast path for MemoryStream.
		if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> buffer))
		{
			// Account for the stream's current position
			int offset = buffer.Offset + (int)ms.Position;
			int count = buffer.Count - (int)ms.Position;
			return this.DeserializeObject(buffer.AsMemory(offset, count), shape, cancellationToken);
		}
		else
		{
			// We don't have a streaming msgpack reader, so buffer it all into memory instead and read from there.
			using SequencePool<byte>.Rental rental = SequencePool<byte>.Shared.Rent();
			int bytesLastRead;
			do
			{
				Span<byte> span = rental.Value.GetSpan(0);
				bytesLastRead = stream.Read(span);
				rental.Value.Advance(bytesLastRead);
			}
			while (bytesLastRead > 0);

			return this.DeserializeObject(rental.Value, shape, cancellationToken);
		}
	}

	/// <summary>
	/// Deserializes a value from a <see cref="Stream"/>.
	/// </summary>
	/// <typeparam name="T"><inheritdoc cref="SerializeAsync{T}(PipeWriter, T, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than the one top-level msgpack structure this method deserializes, the stream may be positioned beyond the end of the deserialized structure after deserialization due to buffering.</param>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <remarks>
	/// If you need to deserialize only a portion of the <paramref name="stream"/>, convert it to a <see cref="PipeReader" /> first
	/// (you may use <see cref="PipeReader.Create(Stream, StreamPipeReaderOptions)" />)
	/// use a deserialize overload that takes a <see cref="PipeReader" />,
	/// and <em>only</em> interact with the stream using the <see cref="PipeReader" /> thereafter.
	/// </remarks>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public async ValueTask<T?> DeserializeAsync<T>(Stream stream, ITypeShape<T> shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		// Fast path for MemoryStream.
		if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> buffer))
		{
			// Account for the stream's current position
			int offset = buffer.Offset + (int)ms.Position;
			int count = buffer.Count - (int)ms.Position;
			return this.Deserialize(buffer.AsMemory(offset, count), shape, cancellationToken);
		}

		PipeReader pipeReader = PipeReader.Create(stream, PipeReaderOptions);
		T? result = await this.DeserializeAsync(pipeReader, shape, cancellationToken).ConfigureAwait(false);
		await pipeReader.CompleteAsync().ConfigureAwait(false);
		return result;
	}

	/// <summary>
	/// Deserializes a value from a <see cref="Stream"/>.
	/// </summary>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <param name="shape"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="DeserializeAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public async ValueTask<object?> DeserializeObjectAsync(Stream stream, ITypeShape shape, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		// Fast path for MemoryStream.
		if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> buffer))
		{
			// Account for the stream's current position
			int offset = buffer.Offset + (int)ms.Position;
			int count = buffer.Count - (int)ms.Position;
			return this.DeserializeObject(buffer.AsMemory(offset, count), shape, cancellationToken);
		}

		PipeReader pipeReader = PipeReader.Create(stream, PipeReaderOptions);
		object? result = await this.DeserializeObjectAsync(pipeReader, shape, cancellationToken).ConfigureAwait(false);
		await pipeReader.CompleteAsync().ConfigureAwait(false);
		return result;
	}

	/// <summary><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/summary"/></summary>
	/// <typeparam name="T"><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <param name="shape"><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/returns"/></returns>
	/// <remarks><inheritdoc cref="DeserializeEnumerableAsync{T}(PipeReader, ITypeShape{T}, CancellationToken)" path="/remarks"/></remarks>
	public async IAsyncEnumerable<T?> DeserializeEnumerableAsync<T>(Stream stream, ITypeShape<T> shape, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		PipeReader pipeReader = PipeReader.Create(stream, PipeReaderOptions);
		await foreach (T? result in this.DeserializeEnumerableAsync<T>(pipeReader, shape, cancellationToken).ConfigureAwait(false))
		{
			yield return result;
		}

		await pipeReader.CompleteAsync().ConfigureAwait(false);
	}

	/// <summary><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/summary"/></summary>
	/// <typeparam name="T"><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/typeparam[@name='T']"/></typeparam>
	/// <typeparam name="TElement"><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/typeparam[@name='TElement']"/></typeparam>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <param name="shape"><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/param[@name='shape']"/></param>
	/// <param name="options"><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/param[@name='options']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	/// <returns><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/returns"/></returns>
	/// <remarks><inheritdoc cref="DeserializePathEnumerableAsync{T, TElement}(PipeReader, ITypeShape{T}, StreamingEnumerationOptions{T, TElement}, CancellationToken)" path="/remarks"/></remarks>
	public async IAsyncEnumerable<TElement?> DeserializePathEnumerableAsync<T, TElement>(Stream stream, ITypeShape<T> shape, StreamingEnumerationOptions<T, TElement> options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		PipeReader pipeReader = PipeReader.Create(stream, PipeReaderOptions);
		await foreach (TElement? result in this.DeserializePathEnumerableAsync(pipeReader, shape, options, cancellationToken).ConfigureAwait(false))
		{
			yield return result;
		}

		await pipeReader.CompleteAsync().ConfigureAwait(false);
	}

	/// <inheritdoc cref="DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public TElement? DeserializePath<T, TElement>(ReadOnlyMemory<byte> buffer, ITypeShape<T> shape, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		MessagePackReader reader = new(buffer);
		return this.DeserializePath(ref reader, shape, options, cancellationToken);
	}

	/// <inheritdoc cref="DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)"/>
	/// <param name="buffer">The msgpack to deserialize from.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public TElement? DeserializePath<T, TElement>(scoped in ReadOnlySequence<byte> buffer, ITypeShape<T> shape, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		MessagePackReader reader = new(buffer);
		return this.DeserializePath(ref reader, shape, options, cancellationToken);
	}

	/// <inheritdoc cref="DeserializePath{T, TElement}(ref MessagePackReader, ITypeShape{T}, in DeserializePathOptions{T, TElement}, CancellationToken)"/>
	/// <param name="stream">The stream to deserialize from. If this stream contains more than one top-level msgpack structure, it may be positioned beyond its end after deserialization due to buffering.</param>
	/// <inheritdoc cref="Deserialize{T}(Stream, ITypeShape{T}, CancellationToken)" path="/remarks" />
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	public TElement? DeserializePath<T, TElement>(Stream stream, ITypeShape<T> shape, in DeserializePathOptions<T, TElement> options, CancellationToken cancellationToken = default)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
	{
		Requires.NotNull(stream);

		// Fast path for MemoryStream.
		if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> buffer))
		{
			// Account for the stream's current position
			int offset = buffer.Offset + (int)ms.Position;
			int count = buffer.Count - (int)ms.Position;
			return this.DeserializePath(buffer.AsMemory(offset, count), shape, options, cancellationToken);
		}
		else
		{
			// We don't have a streaming msgpack reader, so buffer it all into memory instead and read from there.
			using SequencePool<byte>.Rental rental = SequencePool<byte>.Shared.Rent();
			int bytesLastRead;
			do
			{
				Span<byte> span = rental.Value.GetSpan(0);
				bytesLastRead = stream.Read(span);
				rental.Value.Advance(bytesLastRead);
			}
			while (bytesLastRead > 0);

			return this.DeserializePath(rental.Value, shape, options, cancellationToken);
		}
	}
}
