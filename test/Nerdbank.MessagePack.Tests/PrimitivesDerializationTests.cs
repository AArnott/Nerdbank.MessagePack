// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

public partial class PrimitivesDerializationTests : MessagePackSerializerTestBase
{
	protected static readonly DateTime ExpectedDateTime = new(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);

	[Fact]
	public void PositiveIntKeyStretching()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
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
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		Assert.IsType<bool>(deserialized[-45]);
		Assert.IsType<bool>(deserialized[-45L]);
		Assert.IsType<bool>(deserialized[(sbyte)-45]);
		Assert.IsType<bool>(deserialized[(short)-45]);
	}

	[Fact]
	public void SimpleValuesByIndex()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		Assert.Equal(new byte[] { 1, 2, 3 }, deserialized[45]);
	}

	[Fact]
	public void Keys()
	{
		// C# doesn't offer a way to call this method like other languages would, so we'll call it directly.
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		Assert.Equal(["Prop1", "Prop2", "nestedArray", 45UL, -45L, "nestedObject"], deserialized.Keys);
	}

	[Fact]
	public void ReachIntoMap()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		Assert.Equal("nestedValue", ((IDictionary<object, object?>)deserialized["nestedObject"]!)["nestedProp"]);
	}

	[Fact]
	public void IReadOnlyDictionary()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		IReadOnlyDictionary<object, object?> dict = (IReadOnlyDictionary<object, object?>)deserialized;

		Assert.Equal(6, dict.Count);

		Assert.True(dict.ContainsKey("nestedArray"));
		Assert.IsType<object?[]>(dict["nestedArray"]);
		Assert.True(dict.TryGetValue("nestedArray", out object? nestedArray));
		Assert.IsType<object?[]>(nestedArray);

		Assert.False(dict.ContainsKey("doesnotexist"));
		Assert.Throws<KeyNotFoundException>(() => dict["doesnotexist"]);
		Assert.False(dict.TryGetValue("doesnotexist", out _));

		bool encounteredDeeper = false;
		foreach (KeyValuePair<object, object?> item in dict)
		{
			encounteredDeeper |= item.Key is "nestedArray";
		}

		Assert.True(encounteredDeeper);

		Assert.Equal(["Prop1", "Prop2", "nestedArray", 45UL, -45L, "nestedObject"], dict.Keys);
		Assert.Equal(dict.Count, dict.Values.Count());
	}

	[Fact]
	public void IDictionaryOfKV()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
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

		KeyValuePair<object, object?>[] array = new KeyValuePair<object, object?>[7];
		dict.CopyTo(array, 1);
		Assert.Null(array[0].Key);
		Assert.Equal("Prop1", array[1].Key);
		Assert.Equal("Value1", array[1].Value);

		Assert.Equal(6, dict.Count);

		Assert.True(dict.ContainsKey("nestedArray"));
		Assert.IsType<object?[]>(dict["nestedArray"]);
		Assert.True(dict.TryGetValue("nestedArray", out object? nestedArray));
		Assert.IsType<object?[]>(nestedArray);

		Assert.False(dict.ContainsKey("doesnotexist"));
		Assert.Throws<KeyNotFoundException>(() => dict["doesnotexist"]);
		Assert.False(dict.TryGetValue("doesnotexist", out _));

		bool encounteredDeeper = false;
		foreach (KeyValuePair<object, object?> item in dict)
		{
			encounteredDeeper |= item.Key is "nestedArray";
		}

		Assert.True(encounteredDeeper);

		Assert.Equal(["Prop1", "Prop2", "nestedArray", 45UL, -45L, "nestedObject"], dict.Keys);
		Assert.Equal(dict.Count, dict.Values.Count());
	}

	[Fact]
	public void Enumerate_Dictionary()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		foreach (KeyValuePair<object, object?> pair in deserialized)
		{
			this.Logger.WriteLine($"{pair.Key} ({pair.Key.GetType().Name}) = {deserialized[pair.Key]}");
		}

		IEnumerator enumerator = ((IEnumerable)deserialized).GetEnumerator();
		Assert.True(enumerator.MoveNext());
		Assert.Equal("Prop1", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("Prop2", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("nestedArray", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(45UL, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(-45L, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("nestedObject", enumerator.Current);
		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void MissingMembers_Indexer()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		this.Logger.WriteLine(Assert.Throws<KeyNotFoundException>(() => deserialized[88]).Message);
	}

	[Fact]
	public void WritingNotAllowed_Indexer()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		this.Logger.WriteLine(Assert.Throws<NotSupportedException>(() => deserialized["doesNotExist"] = "hi").Message);
		this.Logger.WriteLine(Assert.Throws<NotSupportedException>(() => deserialized["nestedObject"] = "hi").Message);
	}

	protected virtual IDictionary<object, object?> DeserializePrimitives()
	{
		MessagePackReader reader = this.ConstructReader();
		object? deserialized = this.Serializer.DeserializePrimitives(ref reader, TestContext.Current.CancellationToken);
		Assert.NotNull(deserialized);
		return (IDictionary<object, object?>)deserialized;
	}

	protected MessagePackReader ConstructReader()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(6);
		writer.Write("Prop1");
		writer.Write("Value1");
		writer.Write("Prop2");
		writer.Write(42);
		writer.Write("nestedArray");
		writer.WriteArrayHeader(4);
		writer.Write(true);
		writer.Write(3.5);
		writer.Write(new Extension(15, new byte[] { 1, 2, 3 }));
		writer.Write(ExpectedDateTime);
		writer.Write(45); // int key for stretching tests
		writer.Write([1, 2, 3]);
		writer.Write(-45); // negative int key for stretching tests
		writer.Write(false);

		writer.Write("nestedObject");
		writer.WriteMapHeader(1);
		writer.Write("nestedProp");
		writer.Write("nestedValue");

		writer.Flush();

		this.Logger.WriteLine(this.Serializer.ConvertToJson(seq));
		return new MessagePackReader(seq);
	}

	[GenerateShapeFor<IReadOnlyDictionary<MessagePackValue, object>>]
	private partial class Witness;
}
