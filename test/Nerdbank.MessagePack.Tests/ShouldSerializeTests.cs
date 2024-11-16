// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

public partial class ShouldSerializeTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void Person_AllDefaults()
	{
		Person person = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Fact]
	public async Task Person_AllDefaultsAsync()
	{
		Person person = new();
		ReadOnlySequence<byte> sequence = await this.AssertRoundtripAsync(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_AllDefaults()
	{
		PersonWithPrimaryConstructor person = new(null, 0);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Fact]
	public void SerializeDefaultValues()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = true };
		Person person = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(Person.PropertyCount, reader.ReadMapHeader());
	}

	[Fact]
	public void PersonWithName()
	{
		Person person = new() { Name = "Andrew" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.Name), reader.ReadString());
	}

	[Fact]
	public void PersonWithAge()
	{
		Person person = new() { Age = 42 };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);

		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.Age), reader.ReadString());
	}

	[Fact]
	public void Person_DifferentFavoriteColor()
	{
		Person person = new() { FavoriteColor = "Red" };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_DifferentFavoriteColor()
	{
		PersonWithPrimaryConstructor person = new(null, 0, "Red");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
	}

	[Fact]
	public void Person_NoFavoriteColor()
	{
		Person person = new() { FavoriteColor = null };
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		Assert.True(reader.TryReadNil());
	}

	[Fact]
	public void PersonWithPrimaryConstructor_NoFavoriteColor()
	{
		PersonWithPrimaryConstructor person = new(null, 0, FavoriteColor: null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(person);
		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(Person.FavoriteColor), reader.ReadString());
		Assert.True(reader.TryReadNil());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_AllDefaults()
	{
		CtorWithRenamedProperty obj = new();
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		MessagePackReader reader = new(sequence);
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_ChangedName()
	{
		CtorWithRenamedProperty obj = new("Gal");
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
	}

	[Fact]
	public void RenamedPropertyMatchedWithCtorDefaultParameter_NullName()
	{
		CtorWithRenamedProperty obj = new(null);
		ReadOnlySequence<byte> sequence = this.AssertRoundtrip(obj);

		MessagePackReader reader = new(sequence);
		Assert.Equal(1, reader.ReadMapHeader());
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
}
