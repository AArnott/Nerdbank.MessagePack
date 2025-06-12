// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Microsoft.CSharp.RuntimeBinder;

[Trait("dynamic", "true")]
public partial class PrimitivesDynamicDeserializationTests : PrimitivesDerializationTests
{
	[Fact]
	public void ReachIntoArray()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Equal(4, deserialized.nestedArray.Length);
		Assert.Equal(true, deserialized.nestedArray[0]);
		Assert.Equal(3.5, deserialized.nestedArray[1]);
		Assert.Equal(new Extension(15, new byte[] { 1, 2, 3 }), deserialized.nestedArray[2]);
		Assert.Equal<DateTime>(ExpectedDateTime, deserialized.nestedArray[3]);
	}

	[Fact]
	public void ReachIntoMap_Property()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Equal("nestedValue", deserialized.nestedObject.nestedProp);
	}

	[Fact]
	public void SimpleValuesByMember()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.NotNull(deserialized);
		Assert.Equal("Value1", deserialized.Prop1);
		Assert.Equal(42, (int)deserialized.Prop2);
	}

	[Fact]
	public void SimpleValuesByIndex_MultipleIndexes()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Throws<RuntimeBinderException>(() => deserialized[45, 1]);
	}

	[Fact]
	public void MissingMembers_Property()
	{
		dynamic deserialized = this.DeserializePrimitives();
		this.Logger.WriteLine(Assert.Throws<KeyNotFoundException>(() => deserialized.nonexistent).Message);
	}

	[Fact]
	public void WritingNotAllowed_Dynamic()
	{
		dynamic deserialized = this.DeserializePrimitives();
		this.Logger.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized["doesNotExist"] = "hi").Message);
		this.Logger.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized["nestedObject"] = "hi").Message);
		this.Logger.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.doesNotExist = 3).Message);
		this.Logger.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.nestedObject = "hi").Message);
	}

	[Fact]
	public void InvokingMethodsNotAllowed()
	{
		dynamic deserialized = this.DeserializePrimitives();
		this.Logger.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.GetEnumerator()).Message);
	}

	[Fact]
	public void Enumerate_Dynamic()
	{
		dynamic deserialized = this.DeserializePrimitives();
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
		Assert.Equal("nestedArray", enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(45UL, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal(-45L, enumerator.Current);
		Assert.True(enumerator.MoveNext());
		Assert.Equal("nestedObject", enumerator.Current);
		Assert.False(enumerator.MoveNext());
	}

	protected override IDictionary<object, object?> DeserializePrimitives()
	{
		MessagePackReader reader = this.ConstructReader();
		dynamic? deserialized = this.Serializer.DeserializeDynamicPrimitives(ref reader, TestContext.Current.CancellationToken);
		Assert.NotNull(deserialized);
		return deserialized!;
	}
}
