// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS0618 // Type or member is obsolete -- remove this line after https://github.com/AArnott/Nerdbank.MessagePack/pull/771 merges

public partial class DerivedTypeMappingTests(ITestOutputHelper logger)
{
	[Fact]
	public void NonUniqueAliasesRejected_Integers()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>(1));
#else
		mapping.Add<MyDerivedA>(1, Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>(1, Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniqueAliasesRejected_Strings()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>("A");
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>("A"));
#else
		mapping.Add<MyDerivedA>("A", Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedB>("A", Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniqueTypesRejected_Integers()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2));
#else
		mapping.Add<MyDerivedA>(1, Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2, Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniqueTypesRejected_Strings()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>("A");
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2));
#else
		mapping.Add<MyDerivedA>("A", Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(2, Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniquePairsRejected_Integers()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(1));
#else
		mapping.Add<MyDerivedA>(1, Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>(1, Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NonUniquePairsRejected_Strings()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>("A");
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>("A"));
#else
		mapping.Add<MyDerivedA>("A", Witness.GeneratedTypeShapeProvider);
		ArgumentException ex = Assert.Throws<ArgumentException>(() => mapping.Add<MyDerivedA>("A", Witness.GeneratedTypeShapeProvider));
#endif
		logger.WriteLine(ex.Message);
	}

	[Fact]
	public void ObjectInitializerSyntax()
	{
		DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
		{
			{ 1, typeof(MyDerivedA) },
			{ "B", typeof(MyDerivedB) },
		};
		Assert.Equal(2, mapping.Count);
		Assert.Equal(typeof(MyDerivedA), mapping[1]);
		Assert.Equal(typeof(MyDerivedB), mapping["B"]);
	}

	[Fact]
	public void DictionaryInitializerSyntax()
	{
		DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
		{
			[1] = typeof(MyDerivedA),
			["B"] = typeof(MyDerivedB),
		};
		Assert.Equal(2, mapping.Count);
		Assert.Equal(typeof(MyDerivedA), mapping[1]);
		Assert.Equal(typeof(MyDerivedB), mapping["B"]);
	}

	[Fact]
	public void ObjectInitializerSyntax_NonUniqueTypes()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
			{
				{ 1, typeof(MyDerivedA) },
				{ "A", typeof(MyDerivedA) },
			};
		});
	}

	[Fact]
	public void ObjectInitializerSyntax_NonUniqueAlias()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
			{
				{ 1, typeof(MyDerivedA) },
				{ 1, typeof(MyDerivedB) },
			};
		});
	}

	[Fact]
	public void DictionaryInitializerSyntax_NonUniqueTypes()
	{
		Assert.Throws<ArgumentException>(() =>
		{
			DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
			{
				[1] = typeof(MyDerivedA),
				["B"] = typeof(MyDerivedA),
			};
		});
	}

	[Fact]
	public void DictionaryInitializerSyntax_AddTypeAgainAfterImplicitRemoval()
	{
		DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
		{
			[1] = typeof(MyDerivedA),   // adds MyDerivedA
			[1] = typeof(MyDerivedB),   // implicitly removes MyDerivedA
			["A"] = typeof(MyDerivedA), // adds MyDerivedA again
		};
		Assert.Equal(2, mapping.Count);
		Assert.Equal(typeof(MyDerivedB), mapping[1]);
		Assert.Equal(typeof(MyDerivedA), mapping["A"]);
	}

	[Fact]
	public void DerivedShapeMapping_DisabledTests()
	{
		var union = DerivedTypeUnion.CreateDisabled(typeof(MyBase));
		Assert.True(union.Disabled);
		Assert.Same(typeof(MyBase), union.BaseType);
	}

	[Fact]
	[SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.", Justification = "The whole point of this test is to ensure that the Count property matches what the enumerator would produce.")]
	public void EnumerateMappings()
	{
		MessagePackSerializer serializer = new();
		Assert.Equal(0, serializer.DerivedTypeUnions.Count);
		Assert.Equal(serializer.DerivedTypeUnions.Count, serializer.DerivedTypeUnions.Count());

		serializer = serializer with { DerivedTypeUnions = [new DerivedTypeMapping<MyBase>(Witness.GeneratedTypeShapeProvider) { [1] = typeof(MyDerivedA) }] };
		Assert.Equal(1, serializer.DerivedTypeUnions.Count);
		Assert.Equal(serializer.DerivedTypeUnions.Count, serializer.DerivedTypeUnions.Count());
	}

	[Fact]
	public void ShapeMappingFrozenAfterAdding()
	{
		DerivedShapeMapping<MyBase> mapping = new();
#if NET
		mapping.Add<MyDerivedA>(1);
#else
		mapping.AddSourceGenerated<MyDerivedA>(1);
#endif
		MessagePackSerializer serializer = new()
		{
			DerivedTypeUnions = [mapping],
		};

		// Now that we've added the mapping to the serializer, it is frozen.
#if NET
		Assert.Throws<InvalidOperationException>(() => mapping.Add<MyDerivedA>(1));
#else
		Assert.Throws<InvalidOperationException>(() => mapping.AddSourceGenerated<MyDerivedA>(1));
#endif
	}

	[Fact]
	public void TypeMappingFrozenAfterAdding()
	{
		DerivedTypeMapping<MyBase> mapping = new(Witness.GeneratedTypeShapeProvider)
		{
			{ 1, typeof(MyDerivedA) },
		};
		MessagePackSerializer serializer = new()
		{
			DerivedTypeUnions = [mapping],
		};

		// Now that we've added the mapping to the serializer, it is frozen.
		Assert.Throws<InvalidOperationException>(() => mapping.Add(2, typeof(MyDerivedB)));
		Assert.Throws<InvalidOperationException>(() => mapping[2] = typeof(MyDerivedB));
	}

	[GenerateShape]
	internal partial class MyBase;

	[GenerateShape]
	internal partial class MyDerivedA : MyBase;

	[GenerateShape]
	internal partial class MyDerivedB : MyBase;

	[GenerateShapeFor<MyBase>]
	[GenerateShapeFor<MyDerivedA>]
	[GenerateShapeFor<MyDerivedB>]
	private partial class Witness;
}
