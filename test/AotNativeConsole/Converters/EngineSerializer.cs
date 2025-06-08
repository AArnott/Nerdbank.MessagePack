// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Converters;

public sealed class EngineSerializer
{
	private EngineSerializer()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithGuidConverter();
		this.Serializer = serializer with
		{
			Converters = [
				..serializer.Converters,
				new ColorConverter(),
				new PointConverter(),
				new RectangleConverter(),
				new RectangleFConverter(),
				new Vector2Converter(),
			],
		};
	}

	public static EngineSerializer Instance { get; } = new();

	public MessagePackSerializer Serializer { get; }
}
