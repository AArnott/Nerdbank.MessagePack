// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

public partial class ObjectsAsMapTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
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

	[GenerateShape]
	public partial record Person
	{
		[PropertyShape(Name = "first_name")]
		public required string FirstName { get; init; }

		[PropertyShape(Name = "last_name")]
		public required string LastName { get; init; }
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
