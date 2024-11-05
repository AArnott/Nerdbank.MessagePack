// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes and deserializes a 1-rank array.
/// </summary>
/// <typeparam name="TElement">The element type.</typeparam>
internal class ArrayConverter<TElement>(MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TElement[]>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <inheritdoc/>
	public override TElement[]? Deserialize(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		context.DepthStep();
		int count = reader.ReadArrayHeader();
		TElement[] array = new TElement[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = elementConverter.Deserialize(ref reader, context)!;
		}

		return array;
	}

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref TElement[]? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		writer.WriteArrayHeader(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			elementConverter.Serialize(ref writer, ref value[i]!, context);
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask SerializeAsync(MessagePackAsyncWriter writer, TElement[]? value, SerializationContext context, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		if (elementConverter.PreferAsyncSerialization)
		{
			writer.WriteArrayHeader(value.Length);
			for (int i = 0; i < value.Length; i++)
			{
				await elementConverter.SerializeAsync(writer, value[i], context, cancellationToken).ConfigureAwait(false);
				await writer.FlushIfAppropriateAsync(context, cancellationToken).ConfigureAwait(false);
			}
		}
		else
		{
			int progress = 0;
			do
			{
				SerializeStep(value, ref progress, context, cancellationToken);
				await writer.FlushIfAppropriateAsync(context, cancellationToken).ConfigureAwait(false);
			}
			while (progress < value.Length);

			void SerializeStep(TElement[] value, ref int progress, SerializationContext context, CancellationToken cancellationToken)
			{
				MessagePackWriter syncWriter = writer.CreateWriter();
				if (progress == 0)
				{
					syncWriter.WriteArrayHeader(value.Length);
				}

				for (; progress < value.Length; progress++)
				{
					if (writer.IsTimeToFlush(context))
					{
						break;
					}

					elementConverter.Serialize(ref syncWriter, ref value[progress]!, context);
					cancellationToken.ThrowIfCancellationRequested();
				}

				syncWriter.Flush();
			}
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TElement[]?> DeserializeAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		if (await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
		{
			return null;
		}

		context.DepthStep();

		if (elementConverter.PreferAsyncSerialization)
		{
			int count = await reader.ReadArrayHeaderAsync(cancellationToken).ConfigureAwait(false);
			TElement[] array = new TElement[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = (await elementConverter.DeserializeAsync(reader, context, cancellationToken).ConfigureAwait(false))!;
			}

			return array;
		}
		else
		{
			ReadOnlySequence<byte> map = await reader.ReadNextStructureAsync(context, cancellationToken).ConfigureAwait(false);
			TElement[] array = Read(new MessagePackReader(map));
			reader.AdvanceTo(map.End);
			return array;

			TElement[] Read(MessagePackReader syncReader)
			{
				int count = syncReader.ReadArrayHeader();
				TElement[] array = new TElement[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = elementConverter.Deserialize(ref syncReader, context)!;
				}

				return array;
			}
		}
	}
}
