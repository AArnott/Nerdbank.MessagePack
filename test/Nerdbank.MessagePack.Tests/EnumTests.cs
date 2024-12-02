// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract partial class EnumTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	public enum Simple
	{
		One,
		Two,
		Three,
	}

	public enum CaseInsensitiveCollisions
	{
		One,
		OnE,
	}

	[Flags]
	public enum FlagsEnum
	{
		None = 0,
		One = 1,
		Two = 2,
		Four = 4,
	}

	public enum EnumWithNonUniqueNames
	{
		One = 1,
		AnotherOne = 1,
		Two = 2,
	}

	public MessagePackType ExpectedType { get; set; }

	[Fact]
	public void SimpleEnum()
	{
		this.AssertEnum<Simple, Witness>(Simple.Two);
	}

	[Fact]
	public void Enum_WithCaseInsensitiveCollisions()
	{
		this.AssertEnum<CaseInsensitiveCollisions, Witness>(CaseInsensitiveCollisions.OnE);
		this.AssertEnum<CaseInsensitiveCollisions, Witness>(CaseInsensitiveCollisions.One);
	}

	[Fact]
	public void NonExistentValue_NonFlags()
	{
		this.ExpectedType = MessagePackType.Integer;
		this.AssertEnum<Simple, Witness>((Simple)15);
	}

	[Fact]
	public void NonExistentValue_Flags()
	{
		this.ExpectedType = MessagePackType.Integer;
		this.AssertEnum<FlagsEnum, Witness>((FlagsEnum)15);
	}

	[Fact]
	public void OneValueFromFlags()
	{
		this.AssertEnum<FlagsEnum, Witness>(FlagsEnum.One);
	}

	[Fact]
	public void MultipleFlags()
	{
		this.ExpectedType = MessagePackType.Integer;
		this.AssertEnum<FlagsEnum, Witness>(FlagsEnum.One | FlagsEnum.Two);
	}

	[Fact]
	public void NonUniqueNamesInEnum_Roundtrip()
	{
		this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.AnotherOne);
		this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.One);
	}

	private static ReadOnlySequence<byte> SerializeEnumName(string name)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write(name);
		writer.Flush();
		return seq;
	}

	private void AssertEnum<T, TWitness>(T value)
		where T : struct, Enum
		where TWitness : IShapeable<T>
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<T, TWitness>(value);
		this.AssertType(msgpack, this.ExpectedType);
		this.Logger.WriteLine(value.ToString());
	}

	private void AssertType(ReadOnlySequence<byte> msgpack, MessagePackType expectedType)
	{
		MessagePackReader reader = new(msgpack);
		Assert.Equal(expectedType, reader.NextMessagePackType);
	}

	public class EnumAsStringTests : EnumTests
	{
		public EnumAsStringTests(ITestOutputHelper logger)
			: base(logger)
		{
			this.Serializer = this.Serializer with { SerializeEnumValuesByName = true };
			this.ExpectedType = MessagePackType.String;
		}

		[Fact]
		public void CaseInsensitiveByDefault()
		{
			Assert.Equal(Simple.One, this.Serializer.Deserialize<Simple, Witness>(SerializeEnumName("ONE")));
		}

		[Fact]
		public void UnrecognizedName()
		{
			MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<Simple, Witness>(SerializeEnumName("FOO")));
			this.Logger.WriteLine(ex.Message);
		}

		[Fact]
		public void NonUniqueNamesInEnum_ParseEitherName()
		{
			Assert.Equal(EnumWithNonUniqueNames.One, this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(SerializeEnumName(nameof(EnumWithNonUniqueNames.One))));
			Assert.Equal(EnumWithNonUniqueNames.AnotherOne, this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(SerializeEnumName(nameof(EnumWithNonUniqueNames.AnotherOne))));
		}
	}

	public class EnumAsOrdinalTests : EnumTests
	{
		public EnumAsOrdinalTests(ITestOutputHelper logger)
			: base(logger)
		{
			this.Serializer = this.Serializer with { SerializeEnumValuesByName = false };
			this.ExpectedType = MessagePackType.Integer;
		}
	}

	[GenerateShape<Simple>]
	[GenerateShape<CaseInsensitiveCollisions>]
	[GenerateShape<FlagsEnum>]
	[GenerateShape<EnumWithNonUniqueNames>]
	private partial class Witness;
}
