﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Converters;

[Trait("dynamic", "true")]
public partial class ExpandoObjectConverterTests : MessagePackSerializerTestBase
{
	public ExpandoObjectConverterTests()
	{
		this.Serializer = this.Serializer.WithExpandoObjectConverter();
	}

	[Fact]
	public void SimplePropertyValues()
	{
		dynamic e = new ExpandoObject();
		e.a = 5;
		e.b = "hi";
		e.Nothing = null;

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal(5, (int)e2!.a);
		Assert.Equal("hi", (string?)e2.b);
		Assert.Null(e2!.Nothing);
	}

	[Fact]
	public void Depth()
	{
		dynamic e = new ExpandoObject();
		e.a = new byte[] { 1, 2 };
		e.b = new ExpandoObject();
		e.b.c = "bah";

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal<byte>([1, 2], (byte[])e2.a);
		Assert.Equal("bah", e2.b.c);
	}

	[Fact]
	public void Null()
	{
		Assert.Null(this.Roundtrip<ExpandoObject, Witness>(null));
	}

	[GenerateShapeFor<int>]
	[GenerateShapeFor<ExpandoObject>]
	private partial class Witness;
}
