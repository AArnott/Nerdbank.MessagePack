// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft;

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
	public override async ValueTask SerializeAsync(PipeWriter pipeWriter, TElement[]? value, SerializationContext context, CancellationToken cancellationToken)
	{
		Requires.NotNull(pipeWriter);

		cancellationToken.ThrowIfCancellationRequested();

		if (value is null)
		{
			WriteNil(pipeWriter);
			return;
		}

		context.DepthStep();
		if (elementConverter.PreferAsyncSerialization)
		{
			SerializeHeader(pipeWriter);
			for (int i = 0; i < value.Length; i++)
			{
				await elementConverter.SerializeAsync(pipeWriter, value[i], context, cancellationToken).ConfigureAwait(false);
				await FlushIfAppropriateAsync(pipeWriter, context, cancellationToken).ConfigureAwait(false);
			}

			void SerializeHeader(PipeWriter pipeWriter)
			{
				MessagePackWriter writer = new(pipeWriter);
				writer.WriteArrayHeader(value.Length);
				writer.Flush();
			}
		}
		else
		{
			int progress = 0;
			do
			{
				SerializeStep(pipeWriter, value, ref progress, context, cancellationToken);
				await FlushIfAppropriateAsync(pipeWriter, context, cancellationToken).ConfigureAwait(false);
			}
			while (progress < value.Length);

			void SerializeStep(PipeWriter pipeWriter, TElement[] value, ref int progress, SerializationContext context, CancellationToken cancellationToken)
			{
				MessagePackWriter writer = new(pipeWriter);
				if (progress == 0)
				{
					writer.WriteArrayHeader(value.Length);
				}

				for (; progress < value.Length; progress++)
				{
					if (IsTimeToFlush(pipeWriter, context))
					{
						break;
					}

					elementConverter.Serialize(ref writer, ref value[progress]!, context);
					cancellationToken.ThrowIfCancellationRequested();
				}

				writer.Flush();
			}
		}
	}

	/// <inheritdoc/>
	public override ValueTask<TElement[]?> DeserializeAsync(PipeReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		// TODO: implement this.
		return base.DeserializeAsync(reader, context, cancellationToken);
	}
}
