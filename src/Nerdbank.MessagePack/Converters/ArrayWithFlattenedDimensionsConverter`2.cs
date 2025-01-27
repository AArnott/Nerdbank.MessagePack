// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes an array with rank &gt; 1 by emitting them all in one flat array, preceded by an array that identifies the dimensions of the array.
/// </summary>
/// <typeparam name="TArray">The type of array.</typeparam>
/// <typeparam name="TElement">The type of elements in the array.</typeparam>
/// <param name="elementConverter">The serializer for each element.</param>
/// <remarks>
/// The format for this is:
/// <c>[[dimension0, dimension1, ...], [element0, element1, ...]]</c>.
/// </remarks>
internal class ArrayWithFlattenedDimensionsConverter<TArray, TElement>(MessagePackConverter<TElement> elementConverter) : MessagePackConverter<TArray>
{
	[ThreadStatic]
	private static int[]? dimensionsReusable;

	/// <inheritdoc/>
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The Array.CreateInstance method generates TArray instances.")]
	public override void Read(ref MessagePackReader reader, SerializationContext context,ref TArray? value)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		context.DepthStep();
		int outerCount = reader.ReadArrayHeader();
		if (outerCount != 2)
		{
			throw new MessagePackSerializationException($"Expected array length of 2 but was {outerCount}.");
		}

		int rank = reader.ReadArrayHeader();
		int[] dimensions = dimensionsReusable ??= new int[rank];
		for (int i = 0; i < rank; i++)
		{
			dimensions[i] = reader.ReadInt32();
		}

		Array array = Array.CreateInstance(typeof(TElement), dimensions);
		Span<TElement> elements = AsSpan(array);
		int elementCount = reader.ReadArrayHeader();
		if (elementCount != elements.Length)
		{
			throw new MessagePackSerializationException($"Expected {elements.Length} elements but found {elementCount}.");
		}

		for (int i = 0; i < elements.Length; i++)
		{
			TElement element = default;
			elementConverter.Read(ref reader, context, ref element);
			elements[i] = element;
		}

		value = (TArray)(object)array;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TArray? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		context.DepthStep();
		Array array = (Array)(object)value;

		writer.WriteArrayHeader(2);

		// Write the first inner array, which contains the dimensions of the array.
		int rank = array.Rank;
		writer.WriteArrayHeader(rank);
		for (int i = 0; i < rank; i++)
		{
			writer.Write(array.GetLength(i));
		}

		Span<TElement> elements = AsSpan(array);
		writer.WriteArrayHeader(elements.Length);
		for (int i = 0; i < elements.Length; i++)
		{
			elementConverter.Write(ref writer, elements[i], context);
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

	/// <summary>
	/// Exposes an array of any rank as a flat span of elements.
	/// </summary>
	/// <param name="array">The array.</param>
	/// <returns>The span of all elements.</returns>
	private static Span<TElement> AsSpan(Array array)
		=> MemoryMarshal.CreateSpan(ref Unsafe.As<byte, TElement>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length);
}

#endif
