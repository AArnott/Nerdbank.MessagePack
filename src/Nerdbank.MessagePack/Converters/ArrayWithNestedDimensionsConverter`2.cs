// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes and deserializes an array with rank 1 (or more).
/// </summary>
/// <typeparam name="TArray">The type of the array.</typeparam>
/// <typeparam name="TElement">The type of element stored in the array.</typeparam>
/// <remarks>
/// The msgpack spec doesn't define how to encode multi-dimensional arrays,
/// so we just nest arrays for each dimension.
/// This may change if <see href="https://github.com/msgpack/msgpack/pull/267">this pull request</see> is ever merged
/// into the msgpack spec.
/// </remarks>
internal class ArrayWithNestedDimensionsConverter<TArray, TElement>(MessagePackConverter<TElement> elementConverter, int rank) : MessagePackConverter<TArray>
{
	[ThreadStatic]
	private static int[]? dimensionsReusable;

#pragma warning disable NBMsgPack031 // Exactly one structure -- it can't see into this.method calls
	/// <inheritdoc/>
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The Array.CreateInstance method generates TArray instances.")]
	public override TArray? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		int[] dimensions = dimensionsReusable ??= new int[rank];
		this.PeekDimensionsLength(reader, dimensions);
		Array array = Array.CreateInstance(typeof(TElement), dimensions);
		Span<TElement> elements = AsSpan(array);
		this.ReadSubArray(ref reader, dimensions, elements, context);

		return (TArray)(object)array;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TArray? value, SerializationContext context)
	{
		Array? array = (Array?)(object?)value;
		if (array is null)
		{
			writer.WriteNil();
			return;
		}

		Debug.Assert(rank == array.Rank, $"{rank} == {array.Rank}");

		int[] dimensions = dimensionsReusable ??= new int[rank];
		for (int i = 0; i < rank; i++)
		{
			dimensions[i] = array.GetLength(i);
		}

		this.WriteSubArray(ref writer, dimensions.AsSpan(), AsSpan(array), context);
	}
#pragma warning restore NBMsgPack031 // Exactly one structure

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> new()
		{
			["type"] = "array", // We could go into more detail if needed.
		};

	/// <summary>
	/// Exposes an array of any rank as a flat span of elements.
	/// </summary>
	/// <param name="array">The array.</param>
	/// <returns>The span of all elements.</returns>
	private static Span<TElement> AsSpan(Array array) =>
		MemoryMarshal.CreateSpan(ref Unsafe.As<byte, TElement>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length);

	/// <summary>
	/// Writes an array containing one dimension of an array, and its children, recursively.
	/// </summary>
	/// <param name="writer">The msgpack writer.</param>
	/// <param name="dimensions">The remaining dimensions to be written.</param>
	/// <param name="elements">A flat list of elements to write.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Write" path="/param[@name='context']"/></param>
	private void WriteSubArray(ref MessagePackWriter writer, Span<int> dimensions, Span<TElement> elements, SerializationContext context)
	{
		context.DepthStep();
		int outerDimension = dimensions[0];
		writer.WriteArrayHeader(outerDimension);
		if (dimensions.Length > 1 && outerDimension > 0)
		{
			int subArrayLength = elements.Length / outerDimension;
			for (int i = 0; i < outerDimension; i++)
			{
				this.WriteSubArray(ref writer, dimensions[1..], elements[..subArrayLength], context);
				elements = elements[subArrayLength..];
			}
		}
		else
		{
			for (int i = 0; i < outerDimension; i++)
			{
				elementConverter.Write(ref writer, elements[i], context);
			}
		}
	}

	/// <summary>
	/// Reads an array containing one dimension of an array, and its children, recursively.
	/// </summary>
	/// <param name="reader">The msgpack reader.</param>
	/// <param name="dimensions">The remaining dimensions to be read.</param>
	/// <param name="elements">A flat list of elements to populate.</param>
	/// <param name="context"><inheritdoc cref="MessagePackConverter{T}.Read" path="/param[@name='context']"/></param>
	private void ReadSubArray(ref MessagePackReader reader, Span<int> dimensions, Span<TElement> elements, SerializationContext context)
	{
		context.DepthStep();
		int count = reader.ReadArrayHeader();

		int outerDimension = dimensions[0];
		if (dimensions.Length > 1 && outerDimension > 0)
		{
			int subArrayLength = elements.Length / outerDimension;
			for (int i = 0; i < outerDimension; i++)
			{
				this.ReadSubArray(ref reader, dimensions[1..], elements[..subArrayLength], context);
				elements = elements[subArrayLength..];
			}
		}
		else
		{
			for (int i = 0; i < outerDimension; i++)
			{
				elements[i] = elementConverter.Read(ref reader, context)!;
			}
		}
	}

	/// <summary>
	/// Reads the array headers necessary to determine the length of each dimension for an array.
	/// </summary>
	/// <param name="reader">The reader. This is <em>not</em> a <see langword="ref" /> so as to not impact the caller's read position.</param>
	/// <param name="dimensions">The dimensional array to initialize.</param>
	private void PeekDimensionsLength(MessagePackReader reader, int[] dimensions)
	{
		for (int i = 0; i < dimensions.Length; i++)
		{
			dimensions[i] = reader.ReadArrayHeader();
		}
	}
}
