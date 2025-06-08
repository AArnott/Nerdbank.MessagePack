// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Converters;

/// <summary>
/// Vector2 converter for MessagePack. Compatible with FNA &amp; MonoGame.
/// </summary>
public class Vector2Converter : MessagePackConverter<Vector2>
{
	public override void Write(ref MessagePackWriter writer, in Vector2 value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteArrayHeader(2);
		writer.Write(value.X);
		writer.Write(value.Y);
	}

	public override Vector2 Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		var count = reader.ReadArrayHeader();
		if (count != 2)
		{
			throw new InvalidOperationException($"Invalid Vector2 array length: {count}. Expected 2.");
		}

		return new Vector2(reader.ReadSingle(), reader.ReadSingle());
	}
}
