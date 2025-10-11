// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Extension methods for classes in System.IO.Pipelines.
/// </summary>
internal static class PipelineExtensions
{
	/// <summary>
	/// The minimum size for an object to be allocated on the large object heap (LOH).
	/// </summary>
	private const int LargeObjectHeapThreshold = 85000;

	/// <inheritdoc cref="PipeReader.ReadAtLeastAsync(int, CancellationToken)"/>
	/// <remarks>
	/// <para>
	/// This implementation avoids allocating a buffer matching <paramref name="minimumSize"/>
	/// if it would place it on the LOH (i.e. if it's 85,000 bytes or larger).
	/// </para>
	/// <para>
	/// This is a workaround for <see href="https://github.com/dotnet/runtime/issues/120618">this perf issue</see>.
	/// </para>
	/// </remarks>
	internal static ValueTask<ReadResult> ReadAtLeastNoLOHAsync(this PipeReader reader, int minimumSize, CancellationToken cancellationToken = default)
	{
		// This argument check matches what the original ReadAtLeastAsync does.
		Requires.Range(minimumSize >= 0, nameof(minimumSize));

		if (minimumSize < LargeObjectHeapThreshold)
		{
			// This won't allocate on the LOH, so just use the built-in implementation
			// which is presumably the optimal path.
			return reader.ReadAtLeastAsync(minimumSize, cancellationToken);
		}

		return HelperAsync();
		async ValueTask<ReadResult> HelperAsync()
		{
			ReadResult? readResult = null;
			do
			{
				if (readResult is { Buffer: { } buffer })
				{
					// We must call AdvanceTo between each read, and we do so with arguments
					// that will cause the reader to give us more bytes next time.
					reader.AdvanceTo(buffer.Start, buffer.End);
				}

				readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
			}
			while (!readResult.Value.IsCompleted && readResult.Value.Buffer.Length < minimumSize);

			// Don't call AdvanceTo after our last ReadAsync call.
			// That's for our caller to do.
			return readResult.Value;
		}
	}
}
