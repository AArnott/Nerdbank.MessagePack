// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using Nerdbank.MessagePack;

namespace AotNativeConsole.Models.Animals;

public class SnowLeopard : Animal
{
	public SnowLeopard(Guid id, Vector2 position, float depth, Guid? parentId)
		: base(id, position, depth, AnimalTypes.SnowLeopard)
	{
		this.ParentId = parentId;
	}

	[Key(3)]
	public Guid? ParentId { get; private set; }
}
