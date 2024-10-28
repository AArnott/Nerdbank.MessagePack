// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes and deserializes a 1-rank array.
/// </summary>
/// <typeparam name="TElement">The element type.</typeparam>
internal class ArrayConverter<TElement>(IMessagePackConverter<TElement> elementConverter) : IMessagePackConverter<TElement[]>
{
	/// <inheritdoc/>
	public override TElement[]? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		int count = reader.ReadArrayHeader();
		TElement[] array = new TElement[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = elementConverter.Deserialize(ref reader)!;
		}

		return array;
	}

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref TElement[]? value)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteArrayHeader(value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			elementConverter.Serialize(ref writer, ref value[i]!);
		}
	}
}
