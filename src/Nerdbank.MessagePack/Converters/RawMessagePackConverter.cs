// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Provides the raw msgpack transport intended for the <see cref="RawMessagePack"/> type.
/// </summary>
internal class RawMessagePackConverter : MessagePackConverter<RawMessagePack>
{
	/// <inheritdoc/>
	public override RawMessagePack Read(ref MessagePackReader reader, SerializationContext context) => new RawMessagePack(reader.ReadRaw(context));

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in RawMessagePack value, SerializationContext context) => writer.WriteRaw(value.MsgPack);

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<RawMessagePack> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		RawMessagePack raw = await reader.ReadNextStructureAsync(context, cancellationToken).ConfigureAwait(false);

		// It is imperative that we create a copy of the msgpack buffers immediately, because the underlying async reader reuses the buffers right away.
		return raw.ToOwned();
	}
}
