// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;

[Trait("dynamic", "true")]
public class DynamicSerializationTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	private static readonly DateTime ExpectedDateTime = new(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);

	[Fact]
	public void PositiveIntKeyStretching()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.IsType<byte[]>(deserialized[45]);
		Assert.IsType<byte[]>(deserialized[45UL]);
		Assert.IsType<byte[]>(deserialized[(byte)45]);
		Assert.IsType<byte[]>(deserialized[(sbyte)45]);
		Assert.IsType<byte[]>(deserialized[(short)45]);
		Assert.IsType<byte[]>(deserialized[(ushort)45]);
		Assert.IsType<byte[]>(deserialized[45u]);
	}

	[Fact]
	public void NegativeIntKeyStretching()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.IsType<bool>(deserialized[-45]);
		Assert.IsType<bool>(deserialized[-45L]);
		Assert.IsType<bool>(deserialized[(sbyte)-45]);
		Assert.IsType<bool>(deserialized[(short)-45]);
	}

	[Fact]
	public void SimpleValuesByMember()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.NotNull(deserialized);
		Assert.Equal("Value1", deserialized.Prop1);
		Assert.Equal(42, (int)deserialized.Prop2);
	}

	[Fact]
	public void SimpleValuesByIndex()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.Equal(new byte[] { 1, 2, 3 }, deserialized[45]);
		Assert.Throws<RuntimeBinderException>(() => deserialized[45, 1]);
	}

	[Fact]
	public void GetDynamicMemberNames()
	{
		// C# doesn't offer a way to call this method like other languages would, so we'll call it directly.
		DynamicObject deserialized = (DynamicObject)this.DeserializeDynamic();
		Assert.Equal(["Prop1", "Prop2", "deeper"], deserialized.GetDynamicMemberNames());
	}

	[Fact]
	public void ReachIntoArray()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.Equal(4, deserialized.deeper.Length);
		Assert.Equal(true, deserialized.deeper[0]);
		Assert.Equal(3.5, deserialized.deeper[1]);
		Assert.Equal(new Extension(15, new byte[] { 1, 2, 3 }), deserialized.deeper[2]);
		Assert.Equal<DateTime>(ExpectedDateTime, deserialized.deeper[3]);
	}

	[Fact]
	public void IReadOnlyDictionary()
	{
		dynamic deserialized = this.DeserializeDynamic();
		IReadOnlyDictionary<object, object?> dict = (IReadOnlyDictionary<object, object?>)deserialized;

		Assert.Equal(5, dict.Count);

		Assert.True(dict.ContainsKey("deeper"));
		Assert.IsType<object?[]>(dict["deeper"]);
		Assert.True(dict.TryGetValue("deeper", out object? deeper));
		Assert.IsType<object?[]>(deeper);

		Assert.False(dict.ContainsKey("doesnotexist"));
		Assert.Throws<KeyNotFoundException>(() => dict["doesnotexist"]);
		Assert.False(dict.TryGetValue("doesnotexist", out _));

		bool encounteredDeeper = false;
		foreach (KeyValuePair<object, object?> item in dict)
		{
			encounteredDeeper |= item.Key is "deeper";
		}

		Assert.True(encounteredDeeper);

		Assert.Equal(["Prop1", "Prop2", "deeper", 45UL, -45L], dict.Keys);
		Assert.Equal(dict.Count, dict.Values.Count());
	}

	[Fact]
	public void IDictionaryOfKV()
	{
		dynamic deserialized = this.DeserializeDynamic();
		IDictionary<object, object?> dict = (IDictionary<object, object?>)deserialized;

		Assert.True(dict.IsReadOnly);
		Assert.Throws<NotSupportedException>(() => dict.Clear());
		Assert.Throws<NotSupportedException>(() => dict.Add("5", "3"));
		Assert.Throws<NotSupportedException>(() => dict.Add(new KeyValuePair<object, object?>("5", "3")));
		Assert.Throws<NotSupportedException>(() => dict.Remove("5"));
		Assert.Throws<NotSupportedException>(() => ((ICollection<KeyValuePair<object, object?>>)dict).Remove(new KeyValuePair<object, object?>("5", "3")));
		Assert.Throws<NotSupportedException>(() => dict["5"] = "3");

		Assert.True(dict.Contains(new KeyValuePair<object, object?>("Prop1", "Value1")));
		Assert.False(dict.Contains(new KeyValuePair<object, object?>("Prop1", "Value2")));
		Assert.False(dict.Contains(new KeyValuePair<object, object?>("PropX", "Value2")));

		KeyValuePair<object, object?>[] array = new KeyValuePair<object, object?>[6];
		dict.CopyTo(array, 1);
		Assert.Null(array[0].Key);
		Assert.Equal("Prop1", array[1].Key);
		Assert.Equal("Value1", array[1].Value);

		Assert.Equal(5, dict.Count);

		Assert.True(dict.ContainsKey("deeper"));
		Assert.IsType<object?[]>(dict["deeper"]);
		Assert.True(dict.TryGetValue("deeper", out object? deeper));
		Assert.IsType<object?[]>(deeper);

		Assert.False(dict.ContainsKey("doesnotexist"));
		Assert.Throws<KeyNotFoundException>(() => dict["doesnotexist"]);
		Assert.False(dict.TryGetValue("doesnotexist", out _));

		bool encounteredDeeper = false;
		foreach (KeyValuePair<object, object?> item in dict)
		{
			encounteredDeeper |= item.Key is "deeper";
		}

		Assert.True(encounteredDeeper);

		Assert.Equal(["Prop1", "Prop2", "deeper", 45UL, -45L], dict.Keys);
		Assert.Equal(dict.Count, dict.Values.Count());
	}

	[Fact]
	public void Enumerate()
	{
		dynamic deserialized = this.DeserializeDynamic();
		foreach (object key in deserialized)
		{
			this.Logger.WriteLine($"{key} ({key.GetType().Name}) = {deserialized[key]}");
		}

		IEnumerator enumerator = ((IEnumerable)deserialized).GetEnumerator();
		Assert.True(enumerator.MoveNext());
		Assert.Equal("Prop1", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("Prop2", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("deeper", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(45UL, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(-45L, enumerator.Current);
		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void MissingMembers()
	{
		dynamic deserialized = this.DeserializeDynamic();
		Assert.Throws<KeyNotFoundException>(() => deserialized.nonexistent);
		Assert.Throws<KeyNotFoundException>(() => deserialized[88]);
	}

	private dynamic DeserializeDynamic()
	{
		MessagePackReader reader = this.ConstructReader();
		dynamic deserialized = this.Serializer.DeserializeDynamic(ref reader, TestContext.Current.CancellationToken)!;
		return deserialized;
	}

	private MessagePackReader ConstructReader()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(5);
		writer.Write("Prop1");
		writer.Write("Value1");
		writer.Write("Prop2");
		writer.Write(42);
		writer.Write("deeper");
		writer.WriteArrayHeader(4);
		writer.Write(true);
		writer.Write(3.5);
		writer.Write(new Extension(15, new byte[] { 1, 2, 3 }));
		writer.Write(ExpectedDateTime);
		writer.Write(45); // int key for stretching tests
		writer.Write([1, 2, 3]);
		writer.Write(-45); // negative int key for stretching tests
		writer.Write(false);
		writer.Flush();

		this.Logger.WriteLine(MessagePackSerializer.ConvertToJson(seq));
		return new MessagePackReader(seq);
	}
}
