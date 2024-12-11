// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

/// <summary>
/// A <see cref="PipeReader"/> that is pre-buffered yet will only part with its data in chunks.
/// </summary>
internal class FragmentedPipeReader : PipeReader
{
	private readonly ReadOnlySequence<byte> buffer;
	private readonly int chunkSize;
	private SequencePosition consumed;
	private SequencePosition examined;
	private bool expectAdvance;

	public FragmentedPipeReader(ReadOnlySequence<byte> buffer, int chunkSize = 1)
	{
		this.buffer = buffer;
		this.chunkSize = chunkSize;
		this.consumed = this.examined = buffer.Start;
	}

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
		SequencePosition chunkEnd = this.buffer.GetPosition(this.chunkSize, this.examined);

		ReadOnlySequence<byte> chunk = this.buffer.Slice(this.consumed, chunkEnd);
		bool complete = chunk.End.Equals(this.buffer.End);
		this.expectAdvance = true;
		return new(new ReadResult(chunk, isCanceled: false, complete));
	}

	public override bool TryRead(out ReadResult result) => throw new NotImplementedException();
}
