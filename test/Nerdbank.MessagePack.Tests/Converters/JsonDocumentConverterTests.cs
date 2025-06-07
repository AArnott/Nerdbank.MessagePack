// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Converters;

public partial class JsonDocumentConverterTests : MessagePackSerializerTestBase
{
	private const string Json = """
		{"a":1,"b":[2,3],"c":{"d":"e"},"f":null}
		""";

	public JsonDocumentConverterTests()
	{
		this.Serializer = this.Serializer.WithSystemTextJsonConverters();
	}

	[Fact]
	public void RoundtripDOM()
	{
		JsonDocument el = JsonDocument.Parse(Json);
		JsonDocument? deserialized = this.Roundtrip<JsonDocument, Witness>(el);
		Assert.NotNull(deserialized);
		JsonElement root = deserialized.RootElement;
		Assert.True(root.TryGetProperty("a", out JsonElement a));
		Assert.Equal(1ul, a.GetUInt64());

		Assert.True(root.TryGetProperty("b", out JsonElement b));
		Assert.Equal(2, b.GetArrayLength());

		Assert.True(root.TryGetProperty("c", out JsonElement c));
		Assert.Equal("e", c.GetProperty("d").GetString());

		Assert.Equal(JsonValueKind.Null, root.GetProperty("f").ValueKind);
	}

	[Fact]
	public void Write()
	{
		JsonDocument el = JsonDocument.Parse(Json);
		byte[] msgpack = this.Serializer.Serialize<JsonDocument, Witness>(el, TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);

		string converted = MessagePackSerializer.ConvertToJson(msgpack);
		Assert.Equal(Json, converted);
	}

	[GenerateShape<JsonDocument>]
	private partial class Witness;
}
