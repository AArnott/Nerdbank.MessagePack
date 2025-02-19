// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace ShapeShift.Converters;

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
internal class ArrayWithFlattenedDimensionsConverter<TArray, TElement>(Converter<TElement> elementConverter) : Converter<TArray>
{
	[ThreadStatic]
	private static int[]? dimensionsReusable;

	/// <inheritdoc/>
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The Array.CreateInstance method generates TArray instances.")]
	public override TArray? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		context.DepthStep();
		int? outerCount = reader.ReadStartVector();
		if (outerCount is not (null or 2))
		{
			ThrowWrongNumberOfElements(outerCount.Value);
		}

		int? rank = reader.ReadStartVector();
		if (rank is null)
		{
			throw new NotImplementedException();
		}

		int[] dimensions = dimensionsReusable ??= new int[rank.Value];
		for (int i = 0; i < rank; i++)
		{
			dimensions[i] = reader.ReadInt32();
		}

		Array array = Array.CreateInstance(typeof(TElement), dimensions);
		Span<TElement> elements = AsSpan(array);
		int? elementCount = reader.ReadStartVector();
		if (elementCount is null)
		{
			throw new NotImplementedException();
		}

		if (elementCount != elements.Length)
		{
			throw new SerializationException($"Expected {elements.Length} elements but found {elementCount}.");
		}

		for (int i = 0; i < elements.Length; i++)
		{
			elements[i] = elementConverter.Read(ref reader, context)!;
		}

		return (TArray)(object)array;
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in TArray? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		context.DepthStep();
		Array array = (Array)(object)value;

		writer.WriteStartVector(2);

		// Write the first inner array, which contains the dimensions of the array.
		int rank = array.Rank;
		writer.WriteStartVector(rank);
		for (int i = 0; i < rank; i++)
		{
			writer.Write(array.GetLength(i));
		}

		Span<TElement> elements = AsSpan(array);
		writer.WriteStartVector(elements.Length);
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

	[DoesNotReturn]
	private static void ThrowWrongNumberOfElements(int actual) => throw new SerializationException($"Expected an array of 2 elements, but found {actual}.");

	[DoesNotReturn]
	private static void ThrowTooManyElements() => throw new SerializationException("Expected an array of 2 elements, but found more.");
}

#endif
