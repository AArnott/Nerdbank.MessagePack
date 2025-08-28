// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Nerdbank.MessagePack;
using PolyType;

internal static class StreamingTree
{
	internal static async Task RunAsync()
	{
		Tree tree = new()
		{
			Fruits = [new Fruit(3), new Fruit(5)],
		};

		MessagePackSerializer serializer = new();

		byte[] bytes = serializer.Serialize(tree);

		Console.WriteLine(serializer.ConvertToJson(bytes));

		// synchronous deserialization
		Tree deserializedTree = serializer.Deserialize<Tree>(bytes)!;
		Console.WriteLine($"Tree with {deserializedTree.Fruits.Count} fruit.");

		// "async" enumerating deserialization using an expression tree.
		MessagePackSerializer.StreamingEnumerationOptions<Tree, Fruit> options = new(t => t.Fruits);
		await foreach (Fruit? fruit in serializer.DeserializeEnumerableAsync(PipeReader.Create(new(bytes)), options))
		{
			Console.WriteLine($"  Fruit with {fruit?.Seeds} seeds");
		}
	}
}

[GenerateShape]
partial class Tree
{
	public List<Fruit> Fruits { get; set; } = [];
}

partial record Fruit(int Seeds);
