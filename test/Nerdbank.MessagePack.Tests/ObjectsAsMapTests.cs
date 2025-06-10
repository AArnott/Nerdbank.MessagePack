// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

public partial class ObjectsAsMapTests : MessagePackSerializerTestBase
{
	[Fact]
	public void PropertyWithAlteredName()
	{
		Person person = new Person { FirstName = "Andrew", LastName = "Arnott" };
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, person, TestContext.Current.CancellationToken);
		this.LogMsgPack(buffer);

		MessagePackReader reader = new(buffer);
		Assert.Equal(2, reader.ReadMapHeader());
		Assert.Equal("first_name", reader.ReadString());
		Assert.Equal("Andrew", reader.ReadString());
		Assert.Equal("last_name", reader.ReadString());
		Assert.Equal("Arnott", reader.ReadString());

		Assert.Equal(person, this.Serializer.Deserialize<Person>(buffer, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void PropertyAndConstructorNameCaseMismatch() => this.AssertRoundtrip(new ClassWithConstructorParameterNameMatchTest("Andrew"));

	[Fact]
	public void PropertyGettersIgnored()
	{
		ClassWithUnserializedPropertyGetters obj = new() { Value = true };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadMapHeader());
	}

	[Fact]
	public async Task FetchRequiredBetweenPropertyAndItsSyncValue()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(2);
		writer.Write("Name");
		writer.Write("Andrew");
		writer.Write("Age");
		writer.Flush();
		SequencePosition breakPosition = seq.AsReadOnlySequence.End;
		writer.Write(1);
		writer.Flush();

		FragmentedPipeReader reader = new(seq.AsReadOnlySequence, breakPosition);
		PersonWithAge? person = await this.Serializer.DeserializeAsync<PersonWithAge>(reader, TestContext.Current.CancellationToken);
		Assert.Equal(1, person?.Age);
	}

	[Fact]
	public async Task FetchRequiredBetweenPropertyAndItsSyncValue_DefaultCtor()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(2);
		writer.Write("Name");
		writer.Write("Andrew");
		writer.Write("Age");
		writer.Flush();
		SequencePosition breakPosition = seq.AsReadOnlySequence.End;
		writer.Write(1);
		writer.Flush();

		FragmentedPipeReader reader = new(seq.AsReadOnlySequence, breakPosition);
		PersonWithAgeDefaultCtor? person = await this.Serializer.DeserializeAsync<PersonWithAgeDefaultCtor>(reader, TestContext.Current.CancellationToken);
		Assert.Equal(1, person?.Age);
	}

	[GenerateShape]
	public partial record Person
	{
		[PropertyShape(Name = "first_name")]
		public required string FirstName { get; init; }

		[PropertyShape(Name = "last_name")]
		public required string LastName { get; init; }
	}

	[GenerateShape]
	public partial record PersonWithAge(string Name, int Age);

	[GenerateShape]
	public partial record PersonWithAgeDefaultCtor
	{
		public string? Name { get; set; }

		public int Age { get; set; }
	}

	[GenerateShape]
	public partial class ClassWithConstructorParameterNameMatchTest : IEquatable<ClassWithConstructorParameterNameMatchTest>
	{
		public ClassWithConstructorParameterNameMatchTest(string name) => this.Name = name;

		public string Name { get; set; }

		public bool Equals(ClassWithConstructorParameterNameMatchTest? other) => other is not null && this.Name == other.Name;
	}

	[GenerateShape]
	public partial record ClassWithUnserializedPropertyGetters
	{
		public IObservable<PropertyChangedEventArgs> PropertyChanged => throw new NotImplementedException();

		public bool Value { get; set; }
	}
}
