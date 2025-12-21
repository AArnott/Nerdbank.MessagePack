// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Originally taken from https://github.com/morfah/MessagePackCompareTest
// Copyright (c) 2025 morfah
using System.Diagnostics.CodeAnalysis;
using AotNativeConsole.Models.Animals;

namespace AotNativeConsole.Models;

[GenerateShape]
public partial class MyShape
{
	public MyShape(
		Guid id,
		string name,
		Version version,
		List<Animal> animals)
	{
		this.Id = id;
		this.Version = version;
		this.Animals = animals ?? [];

		this.SetName(name);
	}

	[Key(0)]
	public Guid Id { get; }

	[Key(1)]
	public string Name { get; private set; }

	[Key(2)]
	public Version Version { get; private set; }

	[Key(7)]
	public List<Animal> Animals { get; }

	[MemberNotNull(nameof(Name))]
	public MyShape SetName(string name)
	{
		this.Name = name;
		return this;
	}

	public MyShape AddShapeItem(Animal shapeItem)
	{
		if (shapeItem is null)
		{
			throw new ArgumentNullException(nameof(shapeItem));
		}

		this.Animals.Add(shapeItem);
		return this;
	}
}
