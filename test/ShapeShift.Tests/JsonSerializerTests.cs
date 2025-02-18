// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

public partial class JsonSerializerTests
{
	[Fact]
	public void BasicTest()
	{
		JsonSerializer serializer = new();
		Person original = new() { Name = "Andrew", Age = 42 };
		byte[] utf8Json = serializer.Serialize(original, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal("{\"Name\":\"Andrew\",\"Age\":42}", Encoding.UTF8.GetString(utf8Json));
		Person? deserialized = serializer.Deserialize<Person>(utf8Json, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal(original, deserialized);
	}

	[GenerateShape]
	internal partial record Person
	{
		public required string Name { get; set; }

		public int Age { get; set; }
	}

	[GenerateShape<Person>]
	private partial class Witness;
}
