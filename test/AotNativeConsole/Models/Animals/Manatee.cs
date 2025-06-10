// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Models.Animals;

public class Manatee : Animal
{
	public Manatee(
		Guid id,
		Vector2 position,
		float depth,
		float rotation,
		Color color,
		Rectangle sourceRectangle,
		List<Vector2> paths,
		bool isActivated,
		bool shouldLoop)
		: base(id, position, depth, AnimalTypes.Manatee)
	{
		this.Rotation = rotation;
		this.Color = color;
		this.SourceRectangle = sourceRectangle;
		this.Paths = paths;
		this.IsActivated = isActivated;
		this.ShouldLoop = shouldLoop;
	}

	[Key(3)]
	public float Rotation { get; private set; }

	[Key(4)]
	public Color Color { get; private set; }

	[Key(6)]
	public Rectangle SourceRectangle { get; private set; }

	[Key(7)]
	public List<Vector2> Paths { get; private set; }

	[Key(8)]
	public bool IsActivated { get; private set; }

	[Key(9)]
	public bool ShouldLoop { get; private set; }
}
