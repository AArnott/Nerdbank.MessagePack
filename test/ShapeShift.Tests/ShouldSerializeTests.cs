// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

[Trait("ShouldSerialize", "true")]
public abstract partial class ShouldSerializeTests(SerializerBase serializer) : SerializerTestBase(serializer)
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

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(0, CountMapElements(reader));
	}

	[Fact]
	public async Task Person_AllDefaultsAsync()
	{
		Person person = new();
		ReadOnlySequence<byte> sequence = await this.AssertRoundtripAsync(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(0, CountMapElements(reader));
	}

	[Fact]
	public void PersonWithPrimaryConstructor_AllDefaultsByType()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		Assert.True(count is 1 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal(nameof(PersonWithPrimaryConstructor.FavoriteColor), reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		Assert.True(reader.TryReadNull());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_AllDefaultsByExplicitDefault()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, "Blue");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(0, CountMapElements(reader));
	}

	[Fact]
	public void SerializeDefaultValues()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always };
		Person person = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(Person.PropertyCount, CountMapElements(reader));
	}

	[Fact]
	public void PersonWithName()
	{
		Person person = new() { Name = "Andrew" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
		Assert.True(ObjectMapHasKey(reader, nameof(Person.Name)));
	}

	[Fact]
	public void PersonWithAge()
	{
		Person person = new() { Age = 42 };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		Assert.True(count is 1 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal(nameof(Person.Age), reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		reader.Skip(default);
		if (count is null)
		{
			Assert.False(reader.TryAdvanceToNextElement(ref isFirstElement));
		}
	}

	[Fact]
	public void Person_DifferentFavoriteColor()
	{
		Person person = new() { FavoriteColor = "Red" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
		Assert.True(ObjectMapHasKey(reader, nameof(Person.FavoriteColor)));
	}

	[Fact]
	public void PersonWithPrimaryConstructor_DifferentFavoriteColor()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, "Red");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
		Assert.True(ObjectMapHasKey(reader, nameof(Person.FavoriteColor)));
	}

	[Fact]
	public void Person_NoFavoriteColor()
	{
		Person person = new() { FavoriteColor = null };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, this.Serializer.Deformatter);
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		Assert.True(count is 1 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		Assert.True(reader.TryReadNull());
		if (count is null)
		{
			Assert.False(reader.TryAdvanceToNextElement(ref isFirstElement));
		}
	}

	[Fact]
	public void PersonWithPrimaryConstructor_NoFavoriteColor()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		PersonWithPrimaryConstructor person = new(null, 0, FavoriteColor: null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		Reader reader = new(sequence, this.Serializer.Deformatter);
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		Assert.True(count is 1 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		Assert.True(reader.TryReadNull());
		if (count is null)
		{
			Assert.False(reader.TryAdvanceToNextElement(ref isFirstElement));
		}
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_AllDefaults()
	{
		CtorWithRenamedProperty obj = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(0, CountMapElements(reader));
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_ChangedName()
	{
		CtorWithRenamedProperty obj = new("Gal");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_NullName()
	{
		CtorWithRenamedProperty obj = new(null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
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

		Reader reader = new(sequence, this.Serializer.Deformatter);
		Assert.Equal(expectedCount, CountMapElements(reader));
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;

		if (hasName)
		{
			Assert.True(count is not null || reader.TryAdvanceToNextElement(ref isFirstElement));
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Name), reader.ReadString());
			reader.ReadMapKeyValueSeparator();
			reader.Skip(default);
		}

		if (hasStamina)
		{
			Assert.True(count is not null || reader.TryAdvanceToNextElement(ref isFirstElement));
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Stamina), reader.ReadString());
			reader.ReadMapKeyValueSeparator();
			reader.Skip(default);
		}

		if (hasFavoriteColor)
		{
			Assert.True(count is not null || reader.TryAdvanceToNextElement(ref isFirstElement));
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.FavoriteColor), reader.ReadString());
			reader.ReadMapKeyValueSeparator();
			reader.Skip(default);
		}

		if (hasAge)
		{
			Assert.True(count is not null || reader.TryAdvanceToNextElement(ref isFirstElement));
			Assert.Equal(nameof(PersonWithRequiredAndOptionalProperties.Age), reader.ReadString());
			reader.ReadMapKeyValueSeparator();
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

		Reader reader = new(sequence, this.Serializer.Deformatter);

		if (policy.HasFlag(SerializeDefaultValuesPolicy.Required))
		{
			// One parameter of two have a default value supplied. The other is inherently required.
			// So although we provided no non-default values for this object, one property should be serialized.
			int? count = reader.ReadStartMap();
			bool isFirstElement = true;
			Assert.True(count is 1 || reader.TryAdvanceToNextElement(ref isFirstElement));
			Assert.Equal(nameof(PersonWithRequiredAndOptionalParameters.Name), reader.ReadString());
			reader.ReadMapKeyValueSeparator();
			reader.Skip(default);
			if (count is null)
			{
				Assert.False(reader.TryAdvanceToNextElement(ref isFirstElement));
			}
		}
		else
		{
			Assert.Equal(0, CountMapElements(reader));
		}
	}

	public class Json() : ShouldSerializeTests(CreateJsonSerializer());

	public class MsgPack() : ShouldSerializeTests(CreateMsgPackSerializer());

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
