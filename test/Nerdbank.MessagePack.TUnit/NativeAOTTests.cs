// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Specialized;

public partial class NativeAOTTests
{
	[Test]
	public async Task FirstAOTTest()
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
		await foreach (Fruit? fruit in serializer.DeserializePathEnumerableAsync(PipeReader.Create(new(bytes)), options))
		{
			Console.WriteLine($"  Fruit with {fruit?.Seeds} seeds");
		}
	}

	[Test]
	public void NameValueCollectionExplicitConverter()
	{
		NameValueCollection value = new() { { "Name", "Value" } };
		MessagePackSerializer serializer = new MessagePackSerializer().WithNameValueCollectionConverter();

		byte[] msgpack = serializer.Serialize(value, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<NameValueCollection>());
		NameValueCollection? roundtripped = serializer.Deserialize(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<NameValueCollection>());

		Assert.Equal("Value", roundtripped?["Name"]);
	}

	[GenerateShape]
	public partial class Tree
	{
		public List<Fruit> Fruits { get; set; } = [];
	}

	public partial record Fruit(int Seeds);

	[GenerateShapeFor<NameValueCollection>]
	private partial class Witness;
}
