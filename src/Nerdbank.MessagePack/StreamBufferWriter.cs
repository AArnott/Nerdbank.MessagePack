// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Adapts a <see cref="Stream"/> to operate as an <see cref="IBufferWriter{T}"/>.
/// </summary>
/// <param name="stream">The underlying stream. This will never be disposed of.</param>
internal class StreamBufferWriter(Stream stream) : IBufferWriter<byte>, IDisposable
{
	private const int SizeHint0Size = 4096;
	private bool advanceNext;
	private byte[]? buffer;
	private bool disposed;

	/// <inheritdoc/>
	public void Advance(int count)
	{
		Verify.Operation(this.advanceNext, $"Call {nameof(this.GetSpan)} or {nameof(this.GetMemory)} first.");
		Verify.NotDisposed(!this.disposed, this);
		Assumes.NotNull(this.buffer);

		stream.Write(this.buffer[..count]);
		this.advanceNext = false;
	}

	/// <inheritdoc/>
	public Memory<byte> GetMemory(int sizeHint = 0)
	{
		Verify.Operation(!this.advanceNext, $"Call {nameof(this.Advance)} betwen calls to this method.");
		Verify.NotDisposed(!this.disposed, this);

		this.advanceNext = true;
		return this.AcquireBuffer(sizeHint);
	}

	/// <inheritdoc/>
	public Span<byte> GetSpan(int sizeHint = 0)
	{
		Verify.Operation(!this.advanceNext, $"Call {nameof(this.Advance)} betwen calls to this method.");
		Verify.NotDisposed(!this.disposed, this);

		this.advanceNext = true;
		return this.AcquireBuffer(sizeHint);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		this.disposed = true;
		if (this.buffer is not null)
		{
			ArrayPool<byte>.Shared.Return(this.buffer);
			this.buffer = null;
		}
	}

	private byte[] AcquireBuffer(int sizeHint)
	{
		if (this.buffer is null)
		{
			this.buffer = ArrayPool<byte>.Shared.Rent(sizeHint == 0 ? SizeHint0Size : sizeHint);
		}
		else if (this.buffer.Length < sizeHint)
		{
			ArrayPool<byte>.Shared.Return(this.buffer);
			this.buffer = ArrayPool<byte>.Shared.Rent(sizeHint);
		}

		return this.buffer;
	}
}
