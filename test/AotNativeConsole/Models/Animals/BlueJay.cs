// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Models.Animals;

public class BlueJay : Animal
{
	public BlueJay(
		Guid id,
		Vector2 position,
		float depth,
		float rotation,
		Color color,
		Rectangle sourceRectangle,
		bool isPressed,
		List<Guid> targets,
		Guid? parentId)
		: base(id, position, depth, AnimalTypes.BlueJay)
	{
		this.Rotation = rotation;
		this.Color = color;
		this.SourceRectangle = sourceRectangle;
		this.IsPressed = isPressed;
		this.Targets = targets ?? [];
		this.ParentId = parentId;
	}

	[Key(3)]
	public float Rotation { get; private set; }

	[Key(4)]
	public Color Color { get; private set; }

	[Key(6)]
	public Rectangle SourceRectangle { get; private set; }

	[Key(7)]
	public bool IsPressed { get; private set; }

	[Key(8)]
	public List<Guid> Targets { get; private set; }

	[Key(9)]
	public Guid? ParentId { get; private set; }
}
