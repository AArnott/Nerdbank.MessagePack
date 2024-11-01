// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

public partial class ConvertToJsonTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void ConvertToJson_Null() => this.AssertConvertToJson<Primitives>("null", null);

	[Fact]
	public void ConvertToJson_Simple() => this.AssertConvertToJson(@"{""Seeds"":3,""Is"":true,""Number"":1.2}", new Primitives(3, true, 1.2));

	[Fact]
	public void ConvertToJson_Simple2() => this.AssertConvertToJson(@"{""Seeds"":-3,""Is"":false,""Number"":-0.3}", new Primitives(-3, false, -0.3));

	[Fact]
	public void ConvertToJson_Array() => this.AssertConvertToJson(@"{""IntArray"":[1,2,3]}", new ArrayWrapper(1, 2, 3));

	/// <summary>
	/// Verifies the behavior of the simpler method that takes a buffer and returns a string.
	/// </summary>
	[Fact]
	public void ConvertToJson_Sequence() => Assert.Equal("null", MessagePackSerializer.ConvertToJson(new([0xc0])));

	private void AssertConvertToJson<T>([StringSyntax("json")] string expected, T? value)
		where T : IShapeable<T>
	{
		string json = this.ConvertToJson(value);
		this.Logger.WriteLine(json);
		Assert.Equal(expected, json);
	}

	private string ConvertToJson<T>(T? value)
		where T : IShapeable<T>
	{
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, value);
		StringWriter jsonWriter = new();
		MessagePackReader reader = new(sequence);
		MessagePackSerializer.ConvertToJson(ref reader, jsonWriter);
		return jsonWriter.ToString();
	}

	[GenerateShape]
	public partial record Primitives(int Seeds, bool Is, double Number);

	[GenerateShape]
	public partial record ArrayWrapper(params int[] IntArray);
}
