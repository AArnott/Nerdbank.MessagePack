// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class JsonSerializerTests : JsonSerializerTestBase
{
	[Fact]
	public void BasicTest()
	{
		ReadOnlySequence<byte> utf8Bytes = this.AssertRoundtrip(new Person() { Name = "Andrew", Age = 42 });
		Assert.Equal("{\"Name\":\"Andrew\",\"Age\":42}", this.Serializer.Formatter.Encoding.GetString(utf8Bytes.ToArray()));
	}

	[Fact]
	public void DeserializeJsonWithWhitespace()
	{
		Person expected = new() { Name = "Andrew", Age = 42 };
		Person? actual = this.Serializer.Deserialize<Person>(
			"""
			{
				"Name": "Andrew",
				"Age": 42
			}
			""",
#if !NET
			Witness.ShapeProvider,
#endif
			TestContext.Current.CancellationToken);
		Assert.Equal(expected, actual);
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
