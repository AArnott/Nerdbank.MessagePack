// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;
using PolyType;

namespace AotNativeConsole.Models.Animals;

[DerivedTypeShape(typeof(Capybara), Tag = 0)]
[DerivedTypeShape(typeof(SnowLeopard), Tag = 1)]
[DerivedTypeShape(typeof(Axolotl), Tag = 2)]
[DerivedTypeShape(typeof(BlueJay), Tag = 3)]
[DerivedTypeShape(typeof(KomodoDragon), Tag = 4)]
[DerivedTypeShape(typeof(Manatee), Tag = 5)]
[DerivedTypeShape(typeof(RedPanda), Tag = 6)]
public abstract partial class Animal
{
	private readonly string name;

	protected Animal(Guid id, Vector2 position, float depth, AnimalTypes type)
	{
		this.Id = id;

		this.SetDepth(depth);

		this.Position = position;
		this.Type = type;

		this.name = type.ToString();
	}

	[Key(0)]
	public Guid Id { get; private set; }

	[Key(1)]
	public Vector2 Position { get; private set; }

	[Key(2)]
	public float Depth { get; protected set; }

	[PropertyShape(Ignore = true)]
	public AnimalTypes Type { get; }

	public Animal SetDepth(float depth)
	{
		this.Depth = depth;
		return this;
	}

	public override string ToString()
	{
		return this.name;
	}
}
