// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.PolySerializer.Converters;

public ref partial struct StreamingReader
{
	private readonly AsyncReader.GetMoreBytesAsync? getMoreBytesAsync;
	private readonly object? getMoreBytesState;
	private Reader reader;

	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	/// <summary>
	/// Initializes a new instance of the <see cref="StreamingReader"/> struct
	/// that decodes from a complete buffer.
	/// </summary>
	/// <param name="sequence">The buffer to decode msgpack from. This buffer should be complete.</param>
	public StreamingReader(scoped in ReadOnlySequence<byte> sequence, Deformatter deformatter)
		: this(sequence, null, null, deformatter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StreamingReader"/> struct
	/// that decodes from a buffer that may be initially incomplete.
	/// </summary>
	/// <param name="sequence">The buffer we have so far.</param>
	/// <param name="additionalBytesSource">A means to obtain more msgpack bytes when necessary.</param>
	/// <param name="getMoreBytesState">
	/// A value to provide to the <paramref name="getMoreBytesState"/> delegate.
	/// This facilitates reuse of a particular delegate across deserialization operations.
	/// </param>
	public StreamingReader(scoped in ReadOnlySequence<byte> sequence, AsyncReader.GetMoreBytesAsync? additionalBytesSource, object? getMoreBytesState, Deformatter deformatter)
	{
		Requires.NotNull(deformatter);
		this.reader = new Reader(new SequenceReader<byte>(sequence), deformatter);
		this.getMoreBytesAsync = additionalBytesSource;
		this.getMoreBytesState = getMoreBytesState;
		this.eof = additionalBytesSource is null;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StreamingReader"/> struct
	/// that resumes after an <see langword="await" /> operation.
	/// </summary>
	/// <param name="refresh">The data to reinitialize this ref struct.</param>
	public StreamingReader(scoped in AsyncReader.BufferRefresh refresh)
		: this(refresh.Buffer, refresh.GetMoreBytes, refresh.GetMoreBytesState, refresh.Deformatter)
	{
		this.CancellationToken = refresh.CancellationToken;
		this.eof = refresh.EndOfStream;
	}

	private StreamingDeformatter StreamingDeformatter => this.reader.Deformatter.StreamingDeformatter;

	[UnscopedRef]
	public ref Reader Reader => ref this.reader;

	/// <summary>
	/// Gets a token that may cancel deserialization.
	/// </summary>
	public CancellationToken CancellationToken { get; init; }

	internal SequenceReader<byte> SequenceReader => this.reader.SequenceReader;

	/// <summary>
	/// Adds more bytes to the buffer being decoded, if they are available.
	/// </summary>
	/// <param name="minimumLength">The minimum number of bytes to fetch before returning.</param>
	/// <returns>The value to pass to <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.</returns>
	/// <exception cref="EndOfStreamException">Thrown if no more bytes are available.</exception>
	/// <remarks>
	/// This is a destructive operation to this <see cref="MessagePackStreamingReader"/> value.
	/// It must not be used after calling this method.
	/// Instead, the result can use the result of this method to recreate a new <see cref="MessagePackStreamingReader"/> value.
	/// </remarks>
	public ValueTask<AsyncReader.BufferRefresh> FetchMoreBytesAsync(uint minimumLength = 1)
	{
		if (this.getMoreBytesAsync is null || this.eof)
		{
			throw new EndOfStreamException($"Additional bytes are required to complete the operation and no means to get more was provided.");
		}

		this.CancellationToken.ThrowIfCancellationRequested();
		ValueTask<AsyncReader.BufferRefresh> result = HelperAsync(this.getMoreBytesAsync, this.getMoreBytesState, this.reader.SequenceReader.Position, this.reader.SequenceReader.Sequence.End, minimumLength, this.reader.Deformatter, this.CancellationToken);

		// Having made the call to request more bytes, our caller can no longer use this struct because the buffers it had are assumed to have been recycled.
		this.reader = default;
		return result;

		static async ValueTask<AsyncReader.BufferRefresh> HelperAsync(AsyncReader.GetMoreBytesAsync getMoreBytes, object? getMoreBytesState, SequencePosition consumed, SequencePosition examined, uint minimumLength, Deformatter deformatter, CancellationToken cancellationToken)
		{
			ReadResult moreBuffer = await getMoreBytes(getMoreBytesState, consumed, examined, cancellationToken).ConfigureAwait(false);
			while (moreBuffer.Buffer.Length < minimumLength && !(moreBuffer.IsCompleted || moreBuffer.IsCanceled))
			{
				// We haven't got enough bytes. Try again.
				moreBuffer = await getMoreBytes(getMoreBytesState, consumed, moreBuffer.Buffer.End, cancellationToken).ConfigureAwait(false);
			}

			return new AsyncReader.BufferRefresh
			{
				CancellationToken = cancellationToken,
				Buffer = moreBuffer.Buffer,
				GetMoreBytes = getMoreBytes,
				GetMoreBytesState = getMoreBytesState,
				EndOfStream = moreBuffer.IsCompleted,
				Deformatter = deformatter,
			};
		}
	}

	/// <summary>
	/// Gets the information to return from an async method that has been using this reader
	/// so that the caller knows how to resume reading.
	/// </summary>
	/// <returns>The value to pass to <see cref="MessagePackStreamingReader(in BufferRefresh)"/>.</returns>
	public readonly AsyncReader.BufferRefresh GetExchangeInfo() => new()
	{
		CancellationToken = this.CancellationToken,
		Buffer = this.reader.SequenceReader.UnreadSequence,
		GetMoreBytes = this.getMoreBytesAsync,
		GetMoreBytesState = this.getMoreBytesState,
		EndOfStream = this.eof,
		Deformatter = this.reader.Deformatter,
	};

	public DecodeResult TryPeekNextCode(out byte code) => this.StreamingDeformatter.TryPeekNextCode(this.reader, out code);

	public DecodeResult TryPeekNextCode(out TypeCode typeCode) => this.StreamingDeformatter.TryPeekNextCode(this.reader, out typeCode);

	public DecodeResult TryReadRaw(long length, out ReadOnlySequence<byte> rawMsgPack) => this.StreamingDeformatter.TryReadRaw(ref this.reader, length, out rawMsgPack);

	public DecodeResult TryReadArrayHeader(out int length) => this.StreamingDeformatter.TryReadArrayHeader(ref this.reader, out length);

	public DecodeResult TryReadMapHeader(out int count) => this.StreamingDeformatter.TryReadMapHeader(ref this.reader, out count);

	public DecodeResult TryReadNull() => this.StreamingDeformatter.TryReadNull(ref this.reader);

	public DecodeResult TryReadNull(out bool isNull) => this.StreamingDeformatter.TryReadNull(ref this.reader, out isNull);

	public DecodeResult TryRead(out int value) => this.StreamingDeformatter.TryRead(ref this.reader, out value);

	public DecodeResult TrySkip(ref SerializationContext context) => this.StreamingDeformatter.TrySkip(ref this.reader, ref context);

	public DecodeResult TryReadStringSequence(out ReadOnlySequence<byte> value) => this.StreamingDeformatter.TryReadStringSequence(ref this.reader, out value);

	public DecodeResult TryReadStringSpan(out bool contiguous, out ReadOnlySpan<byte> value) => this.StreamingDeformatter.TryReadStringSpan(ref this.reader, out contiguous, out value);
}
