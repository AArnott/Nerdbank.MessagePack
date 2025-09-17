// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Converters;

public partial class JsonNodeConverterTests : MessagePackSerializerTestBase
{
	public JsonNodeConverterTests()
	{
		this.Serializer = this.Serializer.WithSystemTextJsonConverters();
	}

	[Fact]
	public void Roundtrip_JsonNode()
	{
		JsonNode node = JsonNode.Parse("""
			{"a":1,"b":[2,3],"c":{"d":"e"},"f":null}
			""")!;
		JsonNode? deserialized = this.Roundtrip<JsonNode, Witness>(node);
		Assert.NotNull(deserialized);
		Assert.Equal(1ul, deserialized["a"]?.GetValue<ulong>());
		Assert.Equal(2ul, deserialized["b"]?[0]?.GetValue<ulong>());
		Assert.Equal(3ul, deserialized["b"]?[1]?.GetValue<ulong>());
		Assert.Equal("e", deserialized["c"]?["d"]?.GetValue<string>());
		Assert.Null(deserialized["f"]);
	}

	[Fact]
	public void Roundtrip_JsonNode_WithFloatingPoint()
	{
		JsonNode node = JsonNode.Parse("""
			{"pi":3.14,"e":2.718,"negFloat":-123.456,"zero":0.0,"large":1.23e10}
			""")!;
		JsonNode? deserialized = this.Roundtrip<JsonNode, Witness>(node);
		Assert.NotNull(deserialized);
		Assert.Equal(3.14, deserialized["pi"]?.GetValue<double>());
		Assert.Equal(2.718, deserialized["e"]?.GetValue<double>());
		Assert.Equal(-123.456, deserialized["negFloat"]?.GetValue<double>());
		Assert.Equal(0.0, deserialized["zero"]?.GetValue<double>());
		Assert.Equal(1.23e10, deserialized["large"]?.GetValue<double>());
	}

	[Fact]
	public void Write_JsonNode_WithFloatingPoint()
	{
		// Test that we can serialize JsonNode containing floating point numbers
		JsonNode node = JsonValue.Create(3.14);
		byte[] msgpack = this.Serializer.Serialize<JsonNode, Witness>(node, TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);

		// Verify we can convert back to JSON
		string converted = this.Serializer.ConvertToJson(msgpack);
		Assert.Equal("3.14", converted);
	}

	[Fact]
	public void Write_JsonNode_WithMixedNumbers()
	{
		JsonNode node = JsonNode.Parse("""
			{"int":42,"uint":123,"float":3.14,"double":2.718281828}
			""")!;
		byte[] msgpack = this.Serializer.Serialize<JsonNode, Witness>(node, TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);

		string converted = this.Serializer.ConvertToJson(msgpack);
		JsonNode? reparsed = JsonNode.Parse(converted);
		Assert.NotNull(reparsed);
		Assert.Equal(42, reparsed["int"]?.GetValue<int>());
		Assert.Equal(123u, reparsed["uint"]?.GetValue<uint>());
		Assert.Equal(3.14, reparsed["float"]?.GetValue<double>() ?? 0, 10);
		Assert.Equal(2.718281828, reparsed["double"]?.GetValue<double>() ?? 0, 10);
	}

	[GenerateShapeFor<JsonNode>]
	private partial class Witness;
}
