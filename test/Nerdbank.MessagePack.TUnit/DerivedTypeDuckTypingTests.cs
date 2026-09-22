// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable DuckTyping // Experimental API

public partial class DerivedTypeDuckTypingTests : MessagePackSerializerTestBase
{
	public DerivedTypeDuckTypingTests()
	{
		this.Serializer = this.Serializer with
		{
#if NET
			DerivedTypeUnions = [
				new DerivedTypeDuckTyping(
					TypeShapeResolver.Resolve<Animal>(),
					TypeShapeResolver.Resolve<Dog>(),
					TypeShapeResolver.Resolve<Cat>()),
#else
			DerivedTypeUnions = [
				new DerivedTypeDuckTyping(
					TypeShapeResolver.ResolveDynamicOrThrow<Animal>(),
					TypeShapeResolver.ResolveDynamicOrThrow<Dog>(),
					TypeShapeResolver.ResolveDynamicOrThrow<Cat>()),
#endif
			],
		};
	}

	[Test]
	public void RequiredPropertyDistinction_Roundtrip()
	{
		this.AssertRoundtrip<Animal>(new Dog("Buddy", 5));
		this.AssertRoundtrip<Animal>(new Cat("Whiskers", 3));
	}

	[GenerateShape]
	public abstract partial record Animal(string Name);

	[GenerateShape]
	public partial record Dog(string Name, int BarkVolume) : Animal(Name);

	[GenerateShape]
	public partial record Cat(string Name, int MeowPitch) : Animal(Name);

	[GenerateShape]
	public partial record IdenticalTypeBase;

	[GenerateShape]
	public partial record IdenticalType1 : IdenticalTypeBase
	{
		public string CommonProperty { get; init; } = string.Empty;
	}

	[GenerateShape]
	public partial record IdenticalType2 : IdenticalTypeBase
	{
		public string CommonProperty { get; init; } = string.Empty;
	}

	[GenerateShapeFor<Animal>]
	[GenerateShapeFor<Dog>]
	[GenerateShapeFor<Cat>]
	[GenerateShapeFor<IdenticalType1>]
	[GenerateShapeFor<IdenticalType2>]
	private partial class Witness;
}
