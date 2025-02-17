﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// Serializes and deserializes a 1-rank array.
/// </summary>
/// <typeparam name="TElement">The element type.</typeparam>
internal class ArrayConverter<TElement>(Converter<TElement> elementConverter) : Converter<TElement[]>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => true;

	/// <inheritdoc/>
	public override TElement[]? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return null;
		}

		context.DepthStep();
		int count = reader.ReadArrayHeader();
		var array = new TElement[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = elementConverter.Read(ref reader, context)!;
		}

		return array;
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TElement[]? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
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
	public override async ValueTask WriteAsync(AsyncWriter writer, TElement[]? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		context.DepthStep();
		if (elementConverter.PreferAsyncSerialization)
		{
			Writer syncWriter = writer.CreateWriter();
			syncWriter.WriteArrayHeader(value.Length);
			writer.ReturnWriter(ref syncWriter);

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
				Writer syncWriter = writer.CreateWriter();
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
	public override async ValueTask<TElement[]?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool success;
		while (streamingReader.TryReadNull(out success).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		if (success)
		{
			reader.ReturnReader(ref streamingReader);
			return null;
		}

		context.DepthStep();

		if (elementConverter.PreferAsyncSerialization)
		{
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			var array = new TElement[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = (await elementConverter.ReadAsync(reader, context).ConfigureAwait(false))!;
			}

			return array;
		}
		else
		{
			reader.ReturnReader(ref streamingReader);
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			Reader syncReader = reader.CreateBufferedReader();

			int count = syncReader.ReadArrayHeader();
			var array = new TElement[count];
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
