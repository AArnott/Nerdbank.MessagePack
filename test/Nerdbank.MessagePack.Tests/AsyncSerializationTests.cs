// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class AsyncSerializationTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public async Task RoundtripPoco()
	{
		await this.RoundtripAsync(new Poco(1, 2));
	}

	[Fact]
	public async Task LargeArray()
	{
		await this.RoundtripAsync(new ArrayOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i)).ToArray()));
	}

	[GenerateShape]
	public partial record Poco(int X, int Y);

	[GenerateShape]
	public partial class ArrayOfPocos(Poco[] pocos) : IEquatable<ArrayOfPocos>
	{
		public Poco[]? Pocos => pocos;

		public bool Equals(ArrayOfPocos? other) => other is not null && ByValueEquality.Equal(this.Pocos, other.Pocos);
	}
}
