// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using AotNativeConsole.Converters;
using AotNativeConsole.Models;
using AotNativeConsole.Models.Animals;
using Nerdbank.MessagePack;

internal static class ManyModels
{
	internal static MessagePackSerializer Serializer { get; } = CreateSerializer();

	internal static void Run()
	{
		MyShape myShape = CreateShape();
		byte[] bytes = Serializer.Serialize(myShape);

		MyShape? myShape2 = Serializer.Deserialize<MyShape>(bytes);
		byte[] bytes2 = Serializer.Serialize(myShape2);

		if (bytes.Length != bytes2.Length)
		{
			throw new Exception("Bad");
		}

		for (int i = 0; i < bytes.Length; i++)
		{
			if (!bytes[i].Equals(bytes2[i]))
			{
				throw new Exception("Bad");
			}
		}

		Console.WriteLine("Success");
	}

	private static MyShape CreateShape()
	{
		MyShape shape = new(
			Guid.NewGuid(),
			"Test shape",
			new Version(2, 0),
			[
				new Capybara(Guid.NewGuid(), new Vector2(64f, 0f), 0.1f),
					new SnowLeopard(Guid.NewGuid(), new Vector2(64f, 128f), 0.1f, null),
					new KomodoDragon(Guid.NewGuid(), new Vector2(64f, 256f), 0f, 0.1f, Color.White, Rectangle.Empty, false, 0.5f)
			]);

		return shape;
	}

	private static MessagePackSerializer CreateSerializer()
	{
		MessagePackSerializer serializer = new MessagePackSerializer().WithGuidConverter();
		return serializer with
		{
			Converters = [
				..serializer.Converters,
				new AotNativeConsole.Converters.RectangleConverter(),
				new Vector2Converter(),
			],
		};
	}
}
