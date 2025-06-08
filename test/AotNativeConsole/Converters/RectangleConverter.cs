// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Converters;

/// <summary>
/// Rectangle converter for MessagePack. Compatible with FNA &amp; MonoGame.
/// </summary>
public class RectangleConverter : MessagePackConverter<Rectangle>
{
	public override void Write(ref MessagePackWriter writer, in Rectangle value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(4);
		writer.Write(value.X);
		writer.Write(value.Y);
		writer.Write(value.Width);
		writer.Write(value.Height);
	}

	public override Rectangle Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		var count = reader.ReadArrayHeader();
		if (count != 4)
		{
			throw new InvalidOperationException($"Invalid Rectangle array length: {count}. Expected 4.");
		}

		return new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
	}
}
