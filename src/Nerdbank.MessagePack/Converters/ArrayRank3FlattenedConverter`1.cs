// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Converts 3-dimensional arrays by flattening them into a single array for serialization.
/// </summary>
/// <typeparam name="TElement">The type of element stored by the array.</typeparam>
/// <param name="elementConverter">The converter to use for array elements.</param>
/// <remarks>
/// The format for this is:
/// <c>[[dimension0, dimension1, ...], [element0, element1, ...]]</c>.
/// </remarks>
internal class ArrayRank3FlattenedConverter<TElement>(MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TElement[,,]>
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

		// The outer array is always 2 long: [dimensions, data]
		ArrayConverterUtilities.ReadArrayHeader(ref reader, 2);

		// Read the lengths of each dimension.
		Span<int> dimensionLengths = stackalloc int[Rank];
		ArrayConverterUtilities.ReadArrayHeader(ref reader, Rank);

		for (int i = 0; i < Rank; i++)
		{
			dimensionLengths[i] = reader.ReadInt32();
		}

		// Now read in the data itself.
		var result = new TElement[dimensionLengths[0], dimensionLengths[1], dimensionLengths[2]];

		ArrayConverterUtilities.ReadArrayHeader(ref reader, dimensionLengths[0] * dimensionLengths[1] * dimensionLengths[2]);
		for (int i = 0; i < dimensionLengths[0]; i++)
		{
			for (int j = 0; j < dimensionLengths[1]; j++)
			{
				for (int k = 0; k < dimensionLengths[2]; k++)
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

		writer.WriteArrayHeader(2);

		writer.WriteArrayHeader(Rank);
		writer.Write(length0);
		writer.Write(length1);
		writer.Write(length2);

		writer.WriteArrayHeader(length0 * length1 * length2);
		for (int i = 0; i < length0; i++)
		{
			for (int j = 0; j < length1; j++)
			{
				for (int k = 0; k < length2; k++)
				{
					elementConverter.Write(ref writer, in value[i, j, k], context);
				}
			}
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array",
			["items"] = new JsonArray(
				new JsonObject()
				{
					["type"] = "array",
					["items"] = new JsonObject()
					{
						["type"] = "integer",
					},
				},
				new JsonObject()
				{
					["type"] = "array",
					["items"] = elementConverter.GetJsonSchema(context, typeShape),
				}),
		};
}

#endif
