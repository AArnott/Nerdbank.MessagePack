// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

public abstract partial class ObjectsAsMapTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	[Fact]
	public void PropertyWithAlteredName()
	{
		Person person = new Person { FirstName = "Andrew", LastName = "Arnott" };
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, person, TestContext.Current.CancellationToken);
		this.LogFormattedBytes(buffer);

		Reader reader = new(buffer, this.Serializer.Deformatter);
		int? count = reader.ReadStartMap();
		bool isFirstElement = true;
		Assert.True(count is 2 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal("first_name", reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		Assert.Equal("Andrew", reader.ReadString());
		Assert.True(count is 2 || reader.TryAdvanceToNextElement(ref isFirstElement));
		Assert.Equal("last_name", reader.ReadString());
		reader.ReadMapKeyValueSeparator();
		Assert.Equal("Arnott", reader.ReadString());
		Assert.True(count is 2 || !reader.TryAdvanceToNextElement(ref isFirstElement));

		Assert.Equal(person, this.Serializer.Deserialize<Person>(buffer, TestContext.Current.CancellationToken));
	}

	[Fact]
	public void PropertyAndConstructorNameCaseMismatch() => this.AssertRoundtrip(new ClassWithConstructorParameterNameMatchTest("Andrew"));

	[Fact]
	public void PropertyGettersIgnored()
	{
		ClassWithUnserializedPropertyGetters obj = new() { Value = true };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(obj);
		Reader reader = new(msgpack, this.Serializer.Deformatter);
		Assert.Equal(1, CountMapElements(reader));
	}

	[Fact]
	public async Task FetchRequiredBetweenPropertyAndItsSyncValue()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		writer.WriteStartMap(2);
		writer.Write("Name");
		writer.WriteMapKeyValueSeparator();
		writer.Write("Andrew");
		writer.WriteMapPairSeparator();
		writer.Write("Age");
		writer.Flush();
		SequencePosition breakPosition = seq.AsReadOnlySequence.End;
		writer.WriteMapKeyValueSeparator();
		writer.Write(1);
		writer.WriteEndMap();
		writer.Flush();

		FragmentedPipeReader reader = new(seq.AsReadOnlySequence, breakPosition);
		PersonWithAge? person = await this.Serializer.DeserializeAsync<PersonWithAge>(reader, TestContext.Current.CancellationToken);
		Assert.Equal(1, person?.Age);
	}

	[Fact]
	public async Task FetchRequiredBetweenPropertyAndItsSyncValue_DefaultCtor()
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		writer.WriteStartMap(2);
		writer.Write("Name");
		writer.WriteMapKeyValueSeparator();
		writer.Write("Andrew");
		writer.WriteMapPairSeparator();
		writer.Write("Age");
		writer.Flush();
		SequencePosition breakPosition = seq.AsReadOnlySequence.End;
		writer.WriteMapKeyValueSeparator();
		writer.Write(1);
		writer.WriteEndMap();
		writer.Flush();

		FragmentedPipeReader reader = new(seq.AsReadOnlySequence, breakPosition);
		PersonWithAgeDefaultCtor? person = await this.Serializer.DeserializeAsync<PersonWithAgeDefaultCtor>(reader, TestContext.Current.CancellationToken);
		Assert.Equal(1, person?.Age);
	}

	public class Json() : ObjectsAsMapTests(CreateJsonSerializer());

	public class MsgPack() : ObjectsAsMapTests(CreateMsgPackSerializer());

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
