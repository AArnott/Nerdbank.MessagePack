// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ShippedFSharpUnions;

public partial class ShippedUnionFormatTests : MessagePackSerializerTestBase
{
	// Null, base, string alias, integer alias, multi-level base, multi-level derived, and nested multi-level case.
	private const string HierarchyHex =
		"C0\n" +
		"92C081A44E616D65A3416461\n" +
		"92A3436F7782A6576569676874CD0190A44E616D65A6426573736965\n" +
		"920282A5436F6C6F72A562726F776EA44E616D65A3526578\n" +
		"92C081A44E616D65A6537069726974\n" +
		"92AC53686970706564486F72736592C082A5537065656414A44E616D65A6537069726974\n" +
		"92B35368697070656451756172746572486F72736583A5436F6C6F72A3626179A5537065656414A44E616D65A6537069726974";

	private const string DiscriminatorObjectHex =
		"81A3436F7782A6576569676874CD0190A44E616D65A6426573736965";

	// Cow, Horse, Dog, nullary Cat, and a record holding a Cat.
	private const string FSharpHex =
		"92A3436F7782A46E616D65A6426573736965A6776569676874CD0190\n" +
		"92A5486F72736582A46E616D65A6537069726974A5737065656414\n" +
		"92A3446F6782A46E616D65A3526578A5636F6C6F72A562726F776E\n" +
		"92A343617480\n" +
		"81A350657492A343617481A46E616D65A8576869736B657273";

	[Test]
	public void DerivedTypeHierarchy_BytesAreUnchanged()
	{
		string actual = string.Join(
			"\n",
			Hex(this.Serializer.Serialize<ShippedAnimal>(null, this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedAnimal>(new ShippedAnimal("Ada"), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedAnimal>(new ShippedCow("Bessie", 400), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedAnimal>(new ShippedDog("Rex", "brown"), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedMount>(new ShippedMount("Spirit"), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedMount>(new ShippedHorse("Spirit", 20), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<ShippedMount>(new ShippedQuarterHorse("Spirit", 20, "bay"), this.TimeoutToken)));
		Assert.Equal(HierarchyHex, actual);
	}

	[Test]
	public void DerivedTypeHierarchy_DiscriminatorObjectBytesAreUnchanged()
	{
		MessagePackSerializer serializer = this.Serializer with { UseDiscriminatorObjects = true };
		string actual = Hex(serializer.Serialize<ShippedAnimal>(new ShippedCow("Bessie", 400), this.TimeoutToken));
		Assert.Equal(DiscriminatorObjectHex, actual);
	}

	[Test]
	public void FSharpUnion_BytesAreUnchanged()
	{
		string actual = string.Join(
			"\n",
			Hex(this.Serializer.Serialize<Animal, Witness>(Animal.NewCow("Bessie", 400), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<Animal, Witness>(Animal.NewHorse("Spirit", 20), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<Animal, Witness>(Animal.NewDog("Rex", "brown"), this.TimeoutToken)),
			Hex(this.Serializer.Serialize<Animal, Witness>(Animal.Cat, this.TimeoutToken)),
			Hex(this.Serializer.Serialize<Household, Witness>(new Household(Pet.NewCat("Whiskers")), this.TimeoutToken)));
		Assert.Equal(FSharpHex, actual);
	}

	private static string Hex(ReadOnlyMemory<byte> bytes)
	{
		char[] hex = new char[bytes.Length * 2];
		ReadOnlySpan<byte> span = bytes.Span;
		for (int i = 0; i < span.Length; i++)
		{
			byte value = span[i];
			hex[i * 2] = "0123456789ABCDEF"[value >> 4];
			hex[(i * 2) + 1] = "0123456789ABCDEF"[value & 0xF];
		}

		return new string(hex);
	}

	[GenerateShape]
	[DerivedTypeShape(typeof(ShippedCow), Name = "Cow")]
	[DerivedTypeShape(typeof(ShippedDog), Tag = 2)]
	public partial record ShippedAnimal(string Name);

	[GenerateShape]
	public partial record ShippedCow(string Name, int Weight) : ShippedAnimal(Name);

	[GenerateShape]
	public partial record ShippedDog(string Name, string Color) : ShippedAnimal(Name);

	[GenerateShape]
	[DerivedTypeShape(typeof(ShippedHorse))]
	[DerivedTypeShape(typeof(ShippedQuarterHorse))]
	public partial record ShippedMount(string Name);

	[GenerateShape]
	[DerivedTypeShape(typeof(ShippedQuarterHorse))]
	public partial record ShippedHorse(string Name, int Speed) : ShippedMount(Name);

	[GenerateShape]
	public partial record ShippedQuarterHorse(string Name, int Speed, string Color) : ShippedHorse(Name, Speed);

	[GenerateShapeFor<ShippedAnimal>]
	[GenerateShapeFor<ShippedCow>]
	[GenerateShapeFor<ShippedDog>]
	[GenerateShapeFor<ShippedMount>]
	[GenerateShapeFor<ShippedHorse>]
	[GenerateShapeFor<ShippedQuarterHorse>]
	[GenerateShapeFor<Animal>]
	[GenerateShapeFor<Pet>]
	[GenerateShapeFor<Household>]
	private partial class Witness;
}
