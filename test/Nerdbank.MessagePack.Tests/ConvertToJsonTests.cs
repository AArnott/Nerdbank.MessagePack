// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

public partial class ConvertToJsonTests : MessagePackSerializerTestBase
{
	[Fact]
	public void Null() => this.AssertConvertToJson<Primitives>("null", null);

	[Fact]
	public void Simple() => this.AssertConvertToJson(@"{""Seeds"":3,""Is"":true,""Number"":1.2}", new Primitives(3, true, 1.2));

	[Fact]
	public void Simple2() => this.AssertConvertToJson(@"{""Seeds"":-3,""Is"":true,""Number"":-0.3}", new Primitives(-3, true, -0.3));

	[Fact]
	public void Array() => this.AssertConvertToJson(@"{""IntArray"":[1,2,3]}", new ArrayWrapper(1, 2, 3));

	/// <summary>
	/// Verifies the behavior of the simpler method that takes a buffer and returns a string.
	/// </summary>
	[Fact]
	public void Sequence() => Assert.Equal("null", MessagePackSerializer.ConvertToJson(new([0xc0])));

	[Fact]
	public void Indented_Object_Tabs()
	{
		this.AssertConvertToJson(
			"""
			{
				"Seeds": 3,
				"Is": true,
				"Number": 1.2
			}
			""",
			new Primitives(3, true, 1.2),
			new() { Indentation = "\t" });
	}

	[Fact]
	public void Indented_Object_Spaces()
	{
		this.AssertConvertToJson(
			"""
			{
			  "Seeds": 3,
			  "Is": true,
			  "Number": 1.2
			}
			""",
			new Primitives(3, true, 1.2),
			new() { Indentation = "  " });
	}

	[Fact]
	public void Indented_Array()
	{
		this.AssertConvertToJson(
			"""
			{
				"IntArray": [
					1,
					2,
					3
				]
			}
			""",
			new ArrayWrapper([1, 2, 3]),
			new() { Indentation = "\t" });
	}

	[Fact]
	public void Indented_TrailingCommas_Array()
	{
		this.AssertConvertToJson5(
			"""
			{
				"IntArray": [
					1,
					2,
					3,
				],
			}
			""",
			new ArrayWrapper([1, 2, 3]),
			new() { Indentation = "\t", TrailingCommas = true });
	}

	/// <summary>
	/// Asserts that trailing commas are not added when we're not using newlines anyway.
	/// </summary>
	[Fact]
	public void TrailingCommas_IgnoredWithoutIndentation()
	{
		this.AssertConvertToJson(
			"""
			{"Seeds":3,"Is":true,"Number":1.2}
			""",
			new Primitives(3, true, 1.2),
			new() { TrailingCommas = true });
	}

	[Fact]
	public void Indented_TrailingCommas()
	{
		this.AssertConvertToJson5(
			"""
			{
				"Seeds": 3,
				"Is": true,
				"Number": 1.2,
			}
			""",
			new Primitives(3, true, 1.2),
			new() { Indentation = "\t", TrailingCommas = true });
	}

	[Fact]
	public void Indented_NestingObject()
	{
		this.AssertConvertToJson5(
			"""
			{
				"Nested": {
					"Array": [
						{},
						{
							"Value": "Hi",
						},
						{
							"Array": [],
						},
					],
				},
			}
			""",
			new NestingObject(new NestingObject(Array: [new NestingObject(), new NestingObject(Value: "Hi"), new NestingObject(Array: [])])),
			new() { Indentation = "\t", TrailingCommas = true });
	}

	private void AssertConvertToJson<T>([StringSyntax("json")] string expected, T? value, MessagePackSerializer.JsonOptions? options = null)
#if NET
		where T : IShapeable<T>
#endif
	{
		string json = this.ConvertToJson(value, options);
		this.Logger.WriteLine(json);
		Assert.Equal(expected, json);
	}

	private void AssertConvertToJson5<T>([StringSyntax("json5")] string expected, T? value, MessagePackSerializer.JsonOptions? options = null)
#if NET
		where T : IShapeable<T>
#endif
	{
		string json = this.ConvertToJson(value, options);
		this.Logger.WriteLine(json);
		Assert.Equal(expected, json);
	}

	private string ConvertToJson<T>(T? value, MessagePackSerializer.JsonOptions? options)
#if NET
		where T : IShapeable<T>
#endif
	{
		Sequence<byte> sequence = new();
#if NET
		this.Serializer.Serialize(sequence, value);
#else
		this.Serializer.Serialize(sequence, value, Witness.ShapeProvider);
#endif
		StringWriter jsonWriter = new();
		MessagePackReader reader = new(sequence);
		MessagePackSerializer.ConvertToJson(ref reader, jsonWriter, options);
		return jsonWriter.ToString();
	}

	[GenerateShape]
	public partial record Primitives(int Seeds, bool Is, double Number);

	[GenerateShape]
	public partial record ArrayWrapper(params int[] IntArray);

	[GenerateShape]
	public partial record NestingObject(NestingObject? Nested = null, NestingObject[]? Array = null, string? Value = null);

	[GenerateShape<Primitives>]
	private partial class Witness;
}
