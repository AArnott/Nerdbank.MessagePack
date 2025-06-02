// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Converters;

public partial class JsonNodeConverterTests : MessagePackSerializerTestBase
{
	public JsonNodeConverterTests(ITestOutputHelper logger)
		: base(logger)
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

	[GenerateShape<JsonNode>]
	private partial class Witness;
}
