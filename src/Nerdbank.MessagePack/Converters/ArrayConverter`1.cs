// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

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
	public override TElement[]? Read(ref MessagePackReader reader, SerializationContext context)
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
			array[i] = elementConverter.Read(ref reader, context)!;
		}

		return array;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TElement[]? value, SerializationContext context)
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
			elementConverter.Write(ref writer, value[i], context);
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TElement[]? value, SerializationContext context)
	{
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
				await elementConverter.WriteAsync(writer, value[i], context).ConfigureAwait(false);
				await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
			}
		}
		else
		{
			int progress = 0;
			do
			{
				MessagePackWriter syncWriter = writer.CreateWriter();
				syncWriter.WriteArrayHeader(value.Length);
				for (; progress < value.Length && !writer.IsTimeToFlush(context, syncWriter); progress++)
				{
					elementConverter.Write(ref syncWriter, value[progress], context);
					context.CancellationToken.ThrowIfCancellationRequested();
				}

				writer.ReturnWriter(ref syncWriter);
				await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
			}
			while (progress < value.Length);
		}
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TElement[]?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNil(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync());
		}

		if (success)
		{
			return null;
		}

		context.DepthStep();

		if (elementConverter.PreferAsyncSerialization)
		{
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync());
			}

			reader.ReturnReader(ref streamingReader);
			TElement[] array = new TElement[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = (await elementConverter.ReadAsync(reader, context).ConfigureAwait(false))!;
			}

			return array;
		}
		else
		{
			reader.ReturnReader(ref streamingReader);
			await reader.BufferNextStructureAsync(context);
			MessagePackReader syncReader = reader.CreateBufferedReader();

			int count = syncReader.ReadArrayHeader();
			TElement[] array = new TElement[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = elementConverter.Read(ref syncReader, context)!;
			}

			reader.ReturnReader(ref syncReader);
			return array;
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		return new JsonObject
		{
			["type"] = "array",
			["items"] = context.GetJsonSchema(((IEnumerableTypeShape<TElement[], TElement>)typeShape).ElementType),
		};
	}
}
