﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ShapeShift.MessagePack;

public partial class NamingPolicyApplicationTests : MessagePackSerializerTestBase
{
	public NamingPolicyApplicationTests()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = NamingPolicy.CamelCase };
	}

	[Fact]
	public void Roundtrip_NonDefaultCtor() => this.AssertRoundtrip(new NonDefaultCtor("hi", "bye"));

	[Fact]
	public void Roundtrip_DefaultCtor() => this.AssertRoundtrip(new DefaultCtor { SomeProperty = "hi", AnotherProperty = "bye" });

	[Fact]
	public void Roundtrip_KeyedProperties() => this.AssertRoundtrip(new KeyedProperties { SomeProperty = "hi" });

	[Fact]
	public void PolicyAppliedToInferredPropertyNames_DefaultCtor()
	{
		this.PolicyAppliedToInferredPropertyNamesHelper(new DefaultCtor { SomeProperty = "hi", AnotherProperty = "bye" });
	}

	[Fact]
	public void PolicyAppliedToInferredPropertyNames_NonDefaultCtor()
	{
		this.PolicyAppliedToInferredPropertyNamesHelper(new NonDefaultCtor("hi", "bye"));
	}

	[Fact]
	public void PolicyNotAppliedToExplicitPropertyNames_DefaultCtor()
	{
		this.PolicyNotAppliedToExplicitPropertyNamesHelper(new DefaultCtor { SomeProperty = "hi", AnotherProperty = "bye" });
	}

	[Fact]
	public void PolicyNotAppliedToExplicitPropertyNames_NonDefaultCtor()
	{
		this.PolicyNotAppliedToExplicitPropertyNamesHelper(new NonDefaultCtor("hi", "bye"));
	}

	private void PolicyAppliedToInferredPropertyNamesHelper<T>(T value)
#if NET
		where T : IShapeable<T>
#endif
	{
		Sequence<byte> sequence = new();
		this.Serializer = this.Serializer with { PropertyNamingPolicy = NamingPolicy.CamelCase };
		this.Serializer.Serialize(sequence, value);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		reader.ReadStartMap();

		// Assert that the naming policy is applied.
		Assert.Equal("someProperty", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	private void PolicyNotAppliedToExplicitPropertyNamesHelper<T>(T value)
#if NET
		where T : IShapeable<T>
#endif
	{
		Sequence<byte> sequence = new();
		this.Serializer = this.Serializer with { PropertyNamingPolicy = NamingPolicy.CamelCase };
		this.Serializer.Serialize(sequence, value);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		reader.ReadStartMap();

		reader.Skip(new SerializationContext());
		reader.Skip(new SerializationContext());

		// Property name that is explicitly set via PropertyShapeAttribute.Name should never be changed.
		Assert.Equal("ExpressName", reader.ReadString());
	}

	[GenerateShape]
	public partial record NonDefaultCtor(
		string SomeProperty,
		[property: PropertyShape(Name = "ExpressName")] string AnotherProperty);

	[GenerateShape]
	public partial record DefaultCtor
	{
		public string? SomeProperty { get; set; }

		[PropertyShape(Name = "ExpressName")]
		public string? AnotherProperty { get; set; }
	}

	[GenerateShape]
	public partial record KeyedProperties
	{
		[Key(0)]
		public string? SomeProperty { get; set; }
	}
}
