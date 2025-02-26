// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using ShapeShift.MessagePack;

[Trait("ShouldSerialize", "true")]
public partial class ShouldSerializeTests : MessagePackSerializerTestBase
{
	public static IEnumerable<SerializeDefaultValuesPolicy> AllPolicies
	{
		get
		{
			// Enumerate all possible values of the SerializeDefaultValuesPolicy flags enum from None to All.
			// This is done by iterating over all possible values of the underlying integer type.
			for (int i = 0; i <= (int)SerializeDefaultValuesPolicy.Always; i++)
			{
				yield return (SerializeDefaultValuesPolicy)i;
			}
		}
	}

	[Fact]
	public void Person_AllDefaults()
	{
		Person person = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(0, reader.ReadStartMap());
	}

	[Fact]
	public async Task Person_AllDefaultsAsync()
	{
		Person person = new();
		ReadOnlySequence<byte> sequence = await this.AssertRoundtripAsync(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(0, reader.ReadStartMap());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_AllDefaultsByType()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(PersonWithPrimaryConstructor.FavoriteColor), reader.ReadString());
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_AllDefaultsByExplicitDefault()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, "Blue");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(0, reader.ReadStartMap());
	}

	[Fact]
	public void SerializeDefaultValues()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always };
		Person person = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(Person.PropertyCount, reader.ReadStartMap());
	}

	[Fact]
	public void PersonWithName()
	{
		Person person = new() { Name = "Andrew" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.Name), reader.ReadString());
	}

	[Fact]
	public void PersonWithAge()
	{
		Person person = new() { Age = 42 };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.Age), reader.ReadString());
	}

	[Fact]
	public void Person_DifferentFavoriteColor()
	{
		Person person = new() { FavoriteColor = "Red" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_DifferentFavoriteColor()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, "Red");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
	}

	[Fact]
	public void Person_NoFavoriteColor()
	{
		Person person = new() { FavoriteColor = null };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_NoFavoriteColor()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, FavoriteColor: null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_AllDefaults()
	{
		CtorWithRenamedProperty obj = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(0, reader.ReadStartMap());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_ChangedName()
	{
		CtorWithRenamedProperty obj = new("Gal");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_NullName()
	{
		CtorWithRenamedProperty obj = new(null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(1, reader.ReadStartMap());
	}

	[Theory]
	[PairwiseData]
	public void Flags_OnMembersOfAllKinds([CombinatorialMemberData(nameof(AllPolicies))] SerializeDefaultValuesPolicy policy)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = policy };

		PersonWithRequiredAndOptionalProperties obj = new() { Name = null, Stamina = 0 };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		bool hasName = (policy & (SerializeDefaultValuesPolicy.Required | SerializeDefaultValuesPolicy.ReferenceTypes)) != 0;
		bool hasStamina = (policy & (SerializeDefaultValuesPolicy.Required | SerializeDefaultValuesPolicy.ValueTypes)) != 0;
		bool hasFavoriteColor = policy.HasFlag(SerializeDefaultValuesPolicy.ReferenceTypes);
		bool hasAge = policy.HasFlag(SerializeDefaultValuesPolicy.ValueTypes);
		int expectedCount = (hasName ? 1 : 0)
			+ (hasStamina ? 1 : 0)
			+ (hasAge ? 1 : 0)
			+ (hasFavoriteColor ? 1 : 0);

		Reader reader = new(sequence, MessagePackDeformatter.Default);
		Assert.Equal(expectedCount, reader.ReadStartMap());

		if (hasName)
		{
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Name), reader.ReadString());
			reader.Skip(default);
		}

		if (hasStamina)
		{
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Stamina), reader.ReadString());
			reader.Skip(default);
		}

		if (hasFavoriteColor)
		{
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.FavoriteColor), reader.ReadString());
			reader.Skip(default);
		}

		if (hasAge)
		{
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Age), reader.ReadString());
			reader.Skip(default);
		}
	}

	[Theory]
	[InlineData(SerializeDefaultValuesPolicy.Never)]
	[InlineData(SerializeDefaultValuesPolicy.Required)]
	public void Flags_OnRequiredAndOptionalParameters(SerializeDefaultValuesPolicy policy)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = policy };
		PersonWithRequiredAndOptionalParameters obj = new(null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, MessagePackDeformatter.Default);

		if (policy.HasFlag(SerializeDefaultValuesPolicy.Required))
		{
			// One parameter of two have a default value supplied. The other is inherently required.
			// So although we provided no non-default values for this object, one property should be serialized.
			Assert.Equal(1, reader.ReadStartMap());
			Assert.Equal(nameof(PersonWithRequiredAndOptionalParameters.Name), reader.ReadString());
		}
		else
		{
			Assert.Equal(0, reader.ReadStartMap());
		}
	}

	[GenerateShape]
	public partial record Person
	{
		internal const int PropertyCount = 3;

		public string? Name { get; set; }

		public int Age { get; set; }

		[DefaultValue("Blue")]
		public string? FavoriteColor { get; set; } = "Blue";
	}

	[GenerateShape]
	public partial record PersonWithPrimaryConstructor(string? Name, int Age, string? FavoriteColor = "Blue");

	[GenerateShape]
	public partial record CtorWithRenamedProperty
	{
		public CtorWithRenamedProperty(string? name = "Guy")
		{
			this.Name = name;
		}

		[PropertyShape(Name = "person_name")]
		public string? Name { get; }
	}

	[GenerateShape]
	public partial record PersonWithRequiredAndOptionalProperties
	{
		public required string? Name { get; set; }

		public required int Stamina { get; set; }

		public string? FavoriteColor { get; set; }

		public int Age { get; set; }
	}

	[GenerateShape]
	public partial record PersonWithRequiredAndOptionalParameters(string? Name, int? Age = null);
}
