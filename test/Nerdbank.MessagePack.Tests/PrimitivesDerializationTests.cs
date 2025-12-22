// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Numerics;

public partial class PrimitivesDerializationTests : MessagePackSerializerTestBase
{
#if NET
	protected static readonly ReadOnlyMemory<object> ExpectedKeys = new object[] { "Prop1", "Prop2", "nestedArray", 45UL, -45L, "nestedObject", "decimal", "bigint", "guid", "i128", "u128" };
#else
	protected static readonly ReadOnlyMemory<object> ExpectedKeys = new object[] { "Prop1", "Prop2", "nestedArray", 45UL, -45L, "nestedObject", "decimal", "bigint", "guid" };
#endif

	protected static readonly DateTime ExpectedDateTime = new(2023, 1, 2, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// A carefully mutated <see cref="ExpectedDateTime"/> value of <see cref="DateTimeKind.Unspecified"/> kind
	/// such that if assumed to be local time, it will be converted to UTC during serialization and be deserialized to equal
	/// <see cref="ExpectedDateTime"/>.
	/// </summary>
	protected static readonly DateTime ExpectedDateTimeUnspecifiedKind = new(ExpectedDateTime.ToLocalTime().Ticks, DateTimeKind.Unspecified);

	private static readonly decimal ExpectedDecimal = 1.33333333m;
	private static readonly BigInteger ExpectedBigInteger = new BigInteger(18) * long.MaxValue;
	private static readonly Guid ExpectedGuid = Guid.NewGuid();

#if NET
	private static readonly Int128 ExpectedInt128 = new(15, 20);
	private static readonly UInt128 ExpectedUInt128 = new(15, 20);
#endif

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
		Assert.Equal(ExpectedKeys.ToArray(), deserialized.Keys);
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

		Assert.Equal(ExpectedKeys.Length, dict.Count);

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

		Assert.Equal(ExpectedKeys.ToArray(), dict.Keys);
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

		KeyValuePair<object, object?>[] array = new KeyValuePair<object, object?>[ExpectedKeys.Length + 1];
		dict.CopyTo(array, 1);
		Assert.Null(array[0].Key);
		Assert.Equal("Prop1", array[1].Key);
		Assert.Equal("Value1", array[1].Value);

		Assert.Equal(ExpectedKeys.Length, dict.Count);

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

		Assert.Equal(ExpectedKeys.ToArray(), dict.Keys);
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
		for (int i = 0; i < ExpectedKeys.Length; i++)
		{
			Assert.True(enumerator.MoveNext());
			Assert.Equal(ExpectedKeys.Span[i], enumerator.Current);
		}

		Assert.False(enumerator.MoveNext());
	}

	[Fact]
	public void Extension_Primitives()
	{
		IDictionary<object, object?> deserialized = this.DeserializePrimitives();
		Assert.Equal(ExpectedDecimal, deserialized["decimal"]);
		Assert.Equal(ExpectedBigInteger, deserialized["bigint"]);
		Assert.Equal(ExpectedGuid, deserialized["guid"]);
#if NET
		Assert.Equal(ExpectedInt128, deserialized["i128"]);
		Assert.Equal(ExpectedUInt128, deserialized["u128"]);
#endif
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
		this.Serializer = this.Serializer
			.WithObjectConverter()
			.WithAssumedDateTimeKind(DateTimeKind.Local);

		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(ExpectedKeys.Length);
		writer.Write("Prop1");
		this.Serializer.Serialize<object, Witness>(ref writer, "Value1", TestContext.Current.CancellationToken);
		writer.Write("Prop2");
		this.Serializer.Serialize<object, Witness>(ref writer, 42, TestContext.Current.CancellationToken);
		writer.Write("nestedArray");
		writer.WriteArrayHeader(5);
		this.Serializer.Serialize<object, Witness>(ref writer, true, TestContext.Current.CancellationToken);
		this.Serializer.Serialize<object, Witness>(ref writer, 3.5, TestContext.Current.CancellationToken);
		this.Serializer.Serialize<object, Witness>(ref writer, new Extension(15, new byte[] { 1, 2, 3 }), TestContext.Current.CancellationToken);
		this.Serializer.Serialize<object, Witness>(ref writer, ExpectedDateTime, TestContext.Current.CancellationToken);
		this.Serializer.Serialize<object, Witness>(ref writer, ExpectedDateTimeUnspecifiedKind, TestContext.Current.CancellationToken);
		writer.Write(45); // int key for stretching tests
		this.Serializer.Serialize<object, Witness>(ref writer, (byte[])[1, 2, 3], TestContext.Current.CancellationToken);
		writer.Write(-45); // negative int key for stretching tests
		this.Serializer.Serialize<object, Witness>(ref writer, false, TestContext.Current.CancellationToken);

		writer.Write("nestedObject");
		writer.WriteMapHeader(1);
		writer.Write("nestedProp");
		this.Serializer.Serialize<object, Witness>(ref writer, "nestedValue", TestContext.Current.CancellationToken);

		writer.Write("decimal");
		this.Serializer.Serialize<decimal, Witness>(ref writer, ExpectedDecimal, TestContext.Current.CancellationToken);

		writer.Write("bigint");
		this.Serializer.Serialize<BigInteger, Witness>(ref writer, ExpectedBigInteger, TestContext.Current.CancellationToken);

		writer.Write("guid");
		this.Serializer.Serialize<Guid, Witness>(ref writer, ExpectedGuid, TestContext.Current.CancellationToken);

#if NET
		writer.Write("i128");
		this.Serializer.Serialize<Int128, Witness>(ref writer, ExpectedInt128, TestContext.Current.CancellationToken);

		writer.Write("u128");
		this.Serializer.Serialize<UInt128, Witness>(ref writer, ExpectedUInt128, TestContext.Current.CancellationToken);
#endif

		writer.Flush();

		this.Logger.WriteLine(this.Serializer.ConvertToJson(seq));
		return new MessagePackReader(seq);
	}

#if NET
	[GenerateShapeFor<Int128>]
	[GenerateShapeFor<UInt128>]
#endif
	[GenerateShapeFor<byte[]>]
	[GenerateShapeFor<decimal>]
	[GenerateShapeFor<BigInteger>]
	[GenerateShapeFor<Guid>]
	[GenerateShapeFor<object>]
	[GenerateShapeFor<IReadOnlyDictionary<MessagePackValue, object>>]
	private partial class Witness;
}
