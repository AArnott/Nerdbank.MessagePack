// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PolyType.Tests;

/// <summary>
/// Tests for serialization of C# union types (the C# 15 <c>union</c> feature).
/// </summary>
/// <remarks>
/// <para>
/// Some tests use union types declared in the PolyType.TestCases package, which exercise consuming unions from another assembly.
/// </para>
/// <para>
/// The union types declared in this file are source generated, and include both <c>union</c> declarations and hand-authored types that follow the C# union member pattern.
/// On frameworks that predate .NET 11, <see cref="UnionAttribute"/> is supplied by a polyfill in the PolyType.TestCases package.
/// </para>
/// </remarks>
public partial class CSharpUnionTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task ValueCase(bool async)
	{
		CSharpScalarUnion value = new(42);
		CSharpScalarUnion result = async ? await this.RoundtripAsync(value) : this.Roundtrip(value);
		Assert.Equal(42, result.Value);
		Assert.Equal("""["Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task ReferenceCase(bool async)
	{
		CSharpScalarUnion value = new("hi");
		CSharpScalarUnion result = async ? await this.RoundtripAsync(value) : this.Roundtrip(value);
		Assert.Equal("hi", result.Value);
		Assert.Equal("""["String","hi"]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void NullPayload()
	{
		// A null payload selects the first nullable case.
		CSharpScalarUnion result = this.Roundtrip(default(CSharpScalarUnion));
		Assert.Null(result.Value);
		Assert.Equal("""["String",null]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void ClassUnion_NullReference()
	{
		Assert.Null(this.Roundtrip<CSharpClassUnion>(null));
		Assert.Equal("null", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void ObjectPayload()
	{
		CSharpHierarchyUnion result = this.Roundtrip(new CSharpHierarchyUnion(new CSharpDog("Rover", 10)));
		Assert.Equal(new CSharpDog("Rover", 10), result.Value);
		Assert.Equal("""["CSharpDog",{"BarkVolume":10,"Name":"Rover"}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void Recursive()
	{
		CSharpRecursiveUnion result = this.Roundtrip(new CSharpRecursiveUnion(new CSharpRecursiveUnion(new CSharpRecursiveUnion(true))));
		Assert.Equal("""["CSharpRecursiveUnion",["CSharpRecursiveUnion",["Boolean",true]]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		CSharpRecursiveUnion inner = Assert.IsType<CSharpRecursiveUnion>(Assert.IsType<CSharpRecursiveUnion>(result.Value).Value);
		Assert.Equal(true, inner.Value);
	}

	[Test]
	public void NoMatchingCase_Throws()
	{
		// default(CSharpAmbiguousNumericUnion) holds a null payload, but no case admits null.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(default(CSharpAmbiguousNumericUnion), GetShape<CSharpAmbiguousNumericUnion>(), this.TimeoutToken));
		this.Logger.LogInformation(ex.ToString());
		Assert.IsAssignableFrom<InvalidOperationException>(ex.GetBaseException());
	}

	[Test]
	public void PerfOverSchemaStability_UsesIntegerTags()
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = true };
		CSharpScalarUnion result = this.Roundtrip(new CSharpScalarUnion("hi"));
		Assert.Equal("hi", result.Value);

		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
	}

	[Test]
	public void DiscriminatorObjects()
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = true };
		CSharpScalarUnion result = this.Roundtrip(new CSharpScalarUnion(42));
		Assert.Equal(42, result.Value);
		Assert.Equal("""{"Int32":42}""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void Schema()
	{
		ITypeShape<CSharpHierarchyUnion> shape = GetShape<CSharpHierarchyUnion>();
		this.Logger.LogInformation(SchemaToString(this.Serializer.GetJsonSchema(shape)));
		Assert.True(this.DataMatchesSchema(new(this.Serializer.Serialize(new CSharpHierarchyUnion(new CSharpDog("Rover", 10)), shape, this.TimeoutToken)), shape));
		Assert.True(this.DataMatchesSchema(new(this.Serializer.Serialize(new CSharpHierarchyUnion(42), shape, this.TimeoutToken)), shape));
	}

	[Test]
	public void DefaultStructuralEquality()
	{
		IEqualityComparer<CSharpHierarchyUnion> comparer = StructuralEqualityComparer.GetDefault(GetShape<CSharpHierarchyUnion>());
		Assert.True(comparer.Equals(new(new CSharpDog("Rover", 10)), new(new CSharpDog("Rover", 10))));
		Assert.False(comparer.Equals(new(new CSharpDog("Rover", 10)), new(new CSharpDog("Rover", 11))));
		Assert.False(comparer.Equals(new(new CSharpDog("Rover", 10)), new(42)));
		Assert.Equal(comparer.GetHashCode(new(new CSharpDog("Rover", 10))), comparer.GetHashCode(new(new CSharpDog("Rover", 10))));
	}

	[Test]
	public void HashCollisionResistantEquality()
	{
		IEqualityComparer<CSharpRecursiveUnion> comparer = StructuralEqualityComparer.GetHashCollisionResistant(GetShape<CSharpRecursiveUnion>());
		Assert.True(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(new CSharpRecursiveUnion(true))));
		Assert.False(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(new CSharpRecursiveUnion(false))));
		Assert.False(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(true)));
		Assert.Equal(comparer.GetHashCode(new(new CSharpRecursiveUnion(true))), comparer.GetHashCode(new(new CSharpRecursiveUnion(true))));
	}

	[Test]
	public void ObjectPayloads()
	{
		Pet result = this.Roundtrip(new Pet(new Dog("Rover", 10)));
		Assert.Equal(new Dog("Rover", 10), result.Value);
		Assert.Equal("""["Dog",{"Name":"Rover","BarkVolume":10}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));

		result = this.Roundtrip(new Pet(new Cat("Whiskers", 13)));
		Assert.Equal(new Cat("Whiskers", 13), result.Value);
	}

	[Test]
	public void UnionAsProperty()
	{
		Household? result = this.Roundtrip(new Household { Pet = new Pet(new Cat("Whiskers", 13)) });
		Assert.Equal(new Cat("Whiskers", 13), result?.Pet?.Value);
		Assert.Equal("""{"Pet":["Cat",{"Name":"Whiskers","MeowPitch":13}]}""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));

		result = this.Roundtrip(new Household { Pet = null });
		Assert.NotNull(result);
		Assert.Null(result.Pet);
	}

	[Test]
	public void RecursiveDeclaredUnion()
	{
		BoolOrNested result = this.Roundtrip(new BoolOrNested(new BoolOrNested(new BoolOrNested(true))));
		Assert.Equal("""["BoolOrNested",["BoolOrNested",["Boolean",true]]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		BoolOrNested inner = Assert.IsType<BoolOrNested>(Assert.IsType<BoolOrNested>(result.Value).Value);
		Assert.Equal(true, inner.Value);
	}

	[Test]
	public void StructWithTryGetValue()
	{
		Assert.Equal(5, this.Roundtrip(new NumberOrText(5)).Value);
		Assert.Equal("five", this.Roundtrip(new NumberOrText("five")).Value);
		Assert.Null(this.Roundtrip(new NumberOrText((string?)null)).Value);
	}

	[Test]
	public void DeclaredUnion_NoMatchingCase_Throws()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(default(IntOrBool), GetShape<IntOrBool>(), this.TimeoutToken));
		this.Logger.LogInformation(ex.ToString());
		Assert.IsAssignableFrom<InvalidOperationException>(ex.GetBaseException());
	}

	[Test]
	public void ReferencePreservation()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		Dog dog = new("Rover", 10);
		PetClassUnion pet = new(dog);
		ThreePets? result = this.Roundtrip(new ThreePets { First = pet, Second = new PetClassUnion(dog), Third = pet });
		Assert.NotNull(result?.First);
		Assert.Equal(dog, result.First.Value);
		Assert.Same(result.First, result.Third);
		Assert.NotSame(result.First, result.Second);
		Assert.Same(result.First.Value, result.Second?.Value);
	}

	private static ITypeShape<T> GetShape<T>()
#if NET
		where T : IShapeable<T> => T.GetTypeShape();
#else
		=> TypeShapeResolver.ResolveDynamicOrThrow<T>();
#endif

#pragma warning disable SA1201 // StyleCop does not yet recognize union declarations.
	[GenerateShape]
	public partial union Pet(Cat, Dog);

	/// <summary>A union whose default value matches no case.</summary>
	[GenerateShape]
	public partial union IntOrBool(int, bool);

	[GenerateShape]
	public partial union BoolOrNested(bool, BoolOrNested);
#pragma warning restore SA1201

	/// <summary>A hand-authored struct union that implements the non-boxing access pattern.</summary>
	[Union, GenerateShape]
	public readonly partial struct NumberOrText
	{
		private readonly int number;
		private readonly string? text;
		private readonly bool isText;

		public NumberOrText(int value) => this.number = value;

		public NumberOrText(string? value)
		{
			this.text = value;
			this.isText = true;
		}

		public object? Value => this.isText ? this.text : this.number;

		public bool HasValue => !this.isText || this.text is not null;

		public bool TryGetValue(out int value)
		{
			value = this.number;
			return !this.isText;
		}

		public bool TryGetValue([NotNullWhen(true)] out string? value)
		{
			value = this.text;
			return this.isText && this.text is not null;
		}
	}

	/// <summary>A hand-authored union implemented as a class, so that reference preservation applies to the union itself.</summary>
	[Union, GenerateShape]
	public sealed partial class PetClassUnion
	{
		public PetClassUnion(Cat value) => this.Value = value;

		public PetClassUnion(Dog value) => this.Value = value;

		public object? Value { get; }
	}

	public record Cat(string Name, int MeowPitch);

	public record Dog(string Name, int BarkVolume);

	[GenerateShape]
	public partial class Household
	{
		public Pet? Pet { get; set; }
	}

	[GenerateShape]
	public partial class ThreePets
	{
		public PetClassUnion? First { get; set; }

		public PetClassUnion? Second { get; set; }

		public PetClassUnion? Third { get; set; }
	}
}
