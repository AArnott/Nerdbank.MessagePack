// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

/// <summary>
/// A <see cref="PipeReader"/> that is pre-buffered yet will only part with its data in chunks.
/// </summary>
internal class FragmentedPipeReader : PipeReader
{
	private readonly ReadOnlySequence<byte> buffer;

	private readonly int? chunkSize;

	private readonly SequencePosition[]? chunkPositions;
#if NETFRAMEWORK
	private readonly long[]? chunkIndexes;
#endif

	private SequencePosition consumed;
	private SequencePosition examined;
	private bool expectAdvance;
	private SequencePosition? lastReadReturnedPosition;

	public FragmentedPipeReader(ReadOnlySequence<byte> buffer, int chunkSize = 1)
	{
		this.buffer = buffer;
		this.consumed = this.examined = buffer.Start;
		this.chunkSize = chunkSize;
	}

	public FragmentedPipeReader(ReadOnlySequence<byte> buffer, params SequencePosition[] chunkPositions)
	{
		Requires.Argument(chunkPositions.Length > 0, nameof(chunkPositions), "Must provide at least one chunk position.");
		this.buffer = buffer;
		this.consumed = this.examined = buffer.Start;
		this.chunkPositions = chunkPositions;
#if NETFRAMEWORK
		this.chunkIndexes = [.. chunkPositions.Select(p => buffer.Slice(0, p).Length)];
#endif
	}

	internal int ChunksRead { get; private set; }

	public override void AdvanceTo(SequencePosition consumed) => this.AdvanceTo(consumed, consumed);

	public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
	{
		this.consumed = consumed;
		this.examined = examined;
		this.expectAdvance = false;
	}

	public override void CancelPendingRead() => throw new NotImplementedException();

	public override void Complete(Exception? exception = null)
	{
	}

	public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
	{
		Verify.Operation(!this.expectAdvance, "Out of order operations. AdvanceTo call is missing.");
		SequencePosition chunkEnd;
		if (this.chunkSize.HasValue)
		{
			chunkEnd = this.buffer.GetPosition(this.chunkSize.Value, this.examined);
		}
		else
		{
			Assumes.NotNull(this.chunkPositions);
			if (this.lastReadReturnedPosition.HasValue && this.examined.Equals(this.lastReadReturnedPosition.Value))
			{
				// The caller has examined everything we gave them. Give them more.
				this.ChunksRead++;
#if NETFRAMEWORK
				long examinedIndex = this.buffer.Slice(0, this.examined).Length;
				int lastChunkGivenIndex = Array.IndexOf(this.chunkIndexes!, examinedIndex);
#else
				int lastChunkGivenIndex = Array.IndexOf(this.chunkPositions, this.examined);
#endif
				Assumes.True(lastChunkGivenIndex >= 0);
				chunkEnd = this.chunkPositions.Length > lastChunkGivenIndex + 1 ? this.chunkPositions[lastChunkGivenIndex + 1] : this.buffer.End;
			}
			else
			{
				// The caller hasn't finished processing the last chunk we gave them.
				// Give them the same chunk again.
				chunkEnd = this.lastReadReturnedPosition ?? this.chunkPositions[0];
				if (this.lastReadReturnedPosition is null)
				{
					// This is the first read.
					this.ChunksRead++;
				}
			}
		}

		ReadOnlySequence<byte> chunk = this.buffer.Slice(this.consumed, chunkEnd);
		bool complete = chunk.End.Equals(this.buffer.End);
		this.expectAdvance = true;
		this.lastReadReturnedPosition = chunk.End;
		return new(new ReadResult(chunk, isCanceled: false, complete));
	}

	public override bool TryRead(out ReadResult result) => throw new NotImplementedException();
}
