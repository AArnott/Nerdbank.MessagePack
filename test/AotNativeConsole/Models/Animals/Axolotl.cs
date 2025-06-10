// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Models.Animals;

public class Axolotl : Animal
{
	public Axolotl(
		Guid id,
		Vector2 position,
		float depth,
		float rotation,
		Color color,
		Rectangle sourceRectangle,
		Guid? parentId)
		: base(id, position, depth, AnimalTypes.Axolotl)
	{
		this.Rotation = rotation;
		this.Color = color;
		this.SourceRectangle = sourceRectangle;
		this.ParentId = parentId;
	}

	[Key(3)]
	public float Rotation { get; private set; }

	[Key(4)]
	public Color Color { get; private set; }

	[Key(6)]
	public Rectangle SourceRectangle { get; private set; }

	[Key(7)]
	public Guid? ParentId { get; private set; }
}
