// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
using PolyType;

Tree tree = new()
{
	Fruits = [new Fruit(3), new Fruit(5)],
};

MessagePackSerializer serializer = new();
Tree deserializedTree = serializer.Deserialize<Tree>(serializer.Serialize(tree))!;
Console.WriteLine($"Fruit count: {deserializedTree.Fruits.Count}");

[GenerateShape]
partial class Tree
{
	public List<Fruit> Fruits { get; set; } = [];
}

partial class Fruit(int Seeds);
