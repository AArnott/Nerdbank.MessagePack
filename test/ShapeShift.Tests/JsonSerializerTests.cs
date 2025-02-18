// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

public partial class JsonSerializerTests : JsonSerializerTestBase
{
	[Fact]
	public void BasicTest()
	{
		ReadOnlySequence<byte> utf8Bytes = this.AssertRoundtrip(new Person() { Name = "Andrew", Age = 42 });
		Assert.Equal("{\"Name\":\"Andrew\",\"Age\":42}", this.Serializer.Encoding.GetString(utf8Bytes.ToArray()));
	}

	[GenerateShape]
	internal partial record Person
	{
		public required string Name { get; set; }

		public int Age { get; set; }
	}
}
