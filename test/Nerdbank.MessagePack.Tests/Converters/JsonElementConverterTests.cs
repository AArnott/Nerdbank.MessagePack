// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Converters;

public partial class JsonElementConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	private const string Json = """
		{"a":1,"b":[2,3],"c":{"d":"e"},"f":null}
		""";

	private static readonly ReadOnlyMemory<byte> JsonUtf8 = Encoding.UTF8.GetBytes(Json);

	[Fact]
	public void RoundtripDOM()
	{
		Utf8JsonReader reader = new(JsonUtf8.Span);
		JsonElement el = JsonElement.ParseValue(ref reader);
		JsonElement deserialized = this.Roundtrip<JsonElement, Witness>(el);
		Assert.True(deserialized.TryGetProperty("a", out JsonElement a));
		Assert.Equal(1ul, a.GetUInt64());

		Assert.True(deserialized.TryGetProperty("b", out JsonElement b));
		Assert.Equal(2, b.GetArrayLength());

		Assert.True(deserialized.TryGetProperty("c", out JsonElement c));
		Assert.Equal("e", c.GetProperty("d").GetString());

		Assert.Equal(JsonValueKind.Null, deserialized.GetProperty("f").ValueKind);
	}

	[Fact]
	public void Write()
	{
		Utf8JsonReader reader = new(JsonUtf8.Span);
		JsonElement el = JsonElement.ParseValue(ref reader);
		byte[] msgpack = this.Serializer.Serialize<JsonElement, Witness>(el, TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);

		string converted = MessagePackSerializer.ConvertToJson(msgpack);
		Assert.Equal(Json, converted);
	}

	[GenerateShape<JsonElement>]
	private partial class Witness;
}
