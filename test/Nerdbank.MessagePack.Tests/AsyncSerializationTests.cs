// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class AsyncSerializationTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public async Task RoundtripPoco() => await this.AssertRoundtripAsync(new Poco(1, 2));

	[Fact]
	public async Task RoundtripPocoWithDefaultCtor() => await this.AssertRoundtripAsync(new PocoWithDefaultCtor { X = 1, Y = 2 });

	[Fact]
	public async Task LargeArray() => await this.AssertRoundtripAsync(new ArrayOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i)).ToArray()));

	[Fact]
	public async Task Null_Array() => await this.AssertRoundtripAsync(new ArrayOfPocos(null));

	[Fact]
	public async Task Null() => await this.AssertRoundtripAsync<Poco>(null);

	[Fact]
	public async Task ArrayOfInts() => await this.AssertRoundtripAsync(new ArrayOfPrimitives([1, 2, 3]));

	[Fact]
	public async Task ObjectAsArrayOfValues() => await this.AssertRoundtripAsync(new PocoAsArray(42));

	[Fact]
	public async Task ObjectAsArrayOfValues_Null() => await this.AssertRoundtripAsync<PocoAsArray>(null);

	[Fact]
	public async Task ObjectAsArrayOfValues_DefaultCtor() => await this.AssertRoundtripAsync(new PocoAsArrayWithDefaultCtor { Value = 42 });

	[Fact]
	public async Task ObjectAsArrayOfValues_DefaultCtor_Null() => await this.AssertRoundtripAsync<PocoAsArrayWithDefaultCtor>(null);

	[GenerateShape]
	public partial record Poco(int X, int Y);

	[GenerateShape]
	public partial record PocoWithDefaultCtor
	{
		public int X { get; set; }

		public int Y { get; set; }
	}

	[GenerateShape]
	public partial class ArrayOfPocos(Poco[]? pocos) : IEquatable<ArrayOfPocos>
	{
		public Poco[]? Pocos => pocos;

		public bool Equals(ArrayOfPocos? other) => other is not null && ByValueEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class ArrayOfPrimitives(int[]? values) : IEquatable<ArrayOfPrimitives>
	{
		public int[]? Values => values;

		public bool Equals(ArrayOfPrimitives? other) => other is not null && ByValueEquality.Equal(this.Values, other.Values);
	}

	[GenerateShape]
	public partial record PocoAsArray([property: Key(0)] int Value);

	[GenerateShape]
	public partial record PocoAsArrayWithDefaultCtor
	{
		[Key(0)]
		public int Value { get; set; }
	}
}
