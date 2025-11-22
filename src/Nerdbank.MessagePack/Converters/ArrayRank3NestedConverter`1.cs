// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Converts 3-dimensional arrays by nesting them into individual msgpack arrays for serialization.
/// </summary>
/// <typeparam name="TElement">The type of element stored by the array.</typeparam>
/// <param name="elementConverter">The converter to use for array elements.</param>
/// <remarks>
/// The format for this is:
/// <c>[[0-0, 0-1, ...], [1-0, 1-1, ...]]</c>.
/// </remarks>
internal class ArrayRank3NestedConverter<TElement>(MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TElement[,,]>
{
	private const int Rank = 3;

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // Converters should read or write exactly one msgpack structure
	public override TElement[,,]? Read(ref MessagePackReader reader, SerializationContext context)
#pragma warning restore NBMsgPack031 // Converters should read or write exactly one msgpack structure
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		context.DepthStep();

		Span<int> dimensionLengths = stackalloc int[Rank];
		ArrayConverterUtilities.PeekNestedDimensionsLength(reader, dimensionLengths);
		var result = new TElement[dimensionLengths[0], dimensionLengths[1], dimensionLengths[2]];

		int length0 = ArrayConverterUtilities.ReadArrayHeader(ref reader, dimensionLengths[0]);
		for (int i = 0; i < length0; i++)
		{
			int length1 = ArrayConverterUtilities.ReadArrayHeader(ref reader, dimensionLengths[1]);
			for (int j = 0; j < length1; j++)
			{
				int length2 = ArrayConverterUtilities.ReadArrayHeader(ref reader, dimensionLengths[2]);
				for (int k = 0; k < length2; k++)
				{
					result[i, j, k] = elementConverter.Read(ref reader, context)!;
				}
			}
		}

		return result;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TElement[,,]? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();

		int length0 = value.GetLength(0);
		int length1 = value.GetLength(1);
		int length2 = value.GetLength(2);
		writer.WriteArrayHeader(length0);
		for (int i = 0; i < length0; i++)
		{
			writer.WriteArrayHeader(length1);
			for (int j = 0; j < length1; j++)
			{
				writer.WriteArrayHeader(length2);
				for (int k = 0; k < length2; k++)
				{
					elementConverter.Write(ref writer, value[i, j, k], context);
				}
			}
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array", // We could go into more detail if needed.
		};
}

#endif
