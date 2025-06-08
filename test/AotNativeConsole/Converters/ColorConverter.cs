// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Converters;

/// <summary>
/// Color converter for MessagePack. Compatible with FNA.
/// </summary>
public class ColorConverter : MessagePackConverter<Color>
{
	public override void Write(ref MessagePackWriter writer, in Color value, SerializationContext context)
	{
		context.DepthStep();
		writer.WriteInt32(value.ToArgb());
	}

	public override Color Read(ref MessagePackReader reader, SerializationContext context)
	{
		context.DepthStep();
		var value = Color.FromArgb(reader.ReadInt32());
		return value;
	}
}
