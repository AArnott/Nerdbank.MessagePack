// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

	/// <summary>
	/// Gets a token that may cancel deserialization.
	/// </summary>
	public CancellationToken CancellationToken { get; init; }

	public DecodeResult TryReadArrayHeader(out int length) => this.StreamingDeformatter.TryReadArrayHeader(ref this.reader, out length);

	public DecodeResult TryReadMapHeader(out int count) => this.StreamingDeformatter.TryReadMapHeader(ref this.reader, out count);

	public DecodeResult TryReadNull() => this.StreamingDeformatter.TryReadNull(ref this.reader);

	public DecodeResult TryReadNull(out bool isNull) => this.StreamingDeformatter.TryReadNull(ref this.reader, out isNull);
}
