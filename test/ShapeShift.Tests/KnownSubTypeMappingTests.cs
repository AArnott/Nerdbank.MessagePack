// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class KnownSubTypeMappingTests(ITestOutputHelper logger)
{
	[Fact]
	public void NonUniqueAliasesRejected()
	{
		KnownSubTypeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>(1));
#else
		mapping.Add<MyDerivedA>(1, Witness.ShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>(1, Witness.ShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniqueTypesRejected()
	{
		KnownSubTypeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2));
#else
		mapping.Add<MyDerivedA>(1, Witness.ShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2, Witness.ShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniquePairsRejected()
	{
		KnownSubTypeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(1));
#else
		mapping.Add<MyDerivedA>(1, Witness.ShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(1, Witness.ShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[GenerateShape]
	internal partial class MyBase;

	[GenerateShape]
	internal partial class MyDerivedA : MyBase;

	[GenerateShape]
	internal partial class MyDerivedB : MyBase;

	[GenerateShape<MyBase>]
	[GenerateShape<MyDerivedA>]
	[GenerateShape<MyDerivedB>]
	private partial class Witness;
}
