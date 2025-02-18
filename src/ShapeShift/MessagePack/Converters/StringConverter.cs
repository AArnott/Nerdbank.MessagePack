// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;

namespace ShapeShift.MessagePack.Converters;

/// <summary>
/// A msgpack-optimized converter for <see cref="string"/>.
/// </summary>
internal class StringConverter : ShapeShift.Converters.StringConverter
{
	/// <inheritdoc/>
	public override void VerifyCompatibility(Formatter formatter, StreamingDeformatter deformatter) => MessagePackConverter<string>.VerifyFormat(formatter, deformatter);

#if NET
	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<string?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		const uint MinChunkSize = 2048;

		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool wasNil;
		if (streamingReader.TryReadNull(out wasNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (wasNil)
		{
			reader.ReturnReader(ref streamingReader);
			return null;
		}

		string result;
		uint length;
		MessagePackStreamingDeformatter msgpackDeformatter = (MessagePackStreamingDeformatter)reader.Deformatter.StreamingDeformatter;
		while (msgpackDeformatter.TryReadStringHeader(ref streamingReader.Reader, out length).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (streamingReader.TryReadRaw(length, out ReadOnlySequence<byte> utf8BytesSequence).NeedsMoreBytes())
		{
			uint remainingBytesToDecode = length;
			using SequencePool<char>.Rental sequenceRental = SequencePool<char>.Shared.Rent();
			Sequence<char> charSequence = sequenceRental.Value;

			Decoder decoder = reader.Deformatter.Encoding.GetDecoder();
			while (remainingBytesToDecode > 0)
			{
				// We'll always require at least a reasonable number of bytes to decode at once,
				// to keep overhead to a minimum.
				uint desiredBytesThisRound = Math.Min(remainingBytesToDecode, MinChunkSize);
				if (streamingReader.SequenceReader.Remaining < desiredBytesThisRound)
				{
					// We don't have enough bytes to decode this round. Fetch more.
					streamingReader = new(await streamingReader.FetchMoreBytesAsync(desiredBytesThisRound).ConfigureAwait(false));
				}

				int thisLoopLength = unchecked((int)Math.Min(int.MaxValue, Math.Min(checked((uint)streamingReader.SequenceReader.Remaining), remainingBytesToDecode)));
				Assumes.True(streamingReader.TryReadRaw(thisLoopLength, out utf8BytesSequence) == DecodeResult.Success);
				bool flush = utf8BytesSequence.Length == remainingBytesToDecode;
				decoder.Convert(utf8BytesSequence, charSequence, flush, out _, out _);
				remainingBytesToDecode -= checked((uint)utf8BytesSequence.Length);
			}

			result = string.Create(
				checked((int)charSequence.Length),
				charSequence,
				static (span, seq) => seq.AsReadOnlySequence.CopyTo(span));
		}
		else
		{
			// We happened to get all bytes at once. Decode now.
			result = reader.Deformatter.Encoding.GetString(utf8BytesSequence);
		}

		reader.ReturnReader(ref streamingReader);
		return result;
	}
#endif
}
