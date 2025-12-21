// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract partial class EnumTests : MessagePackSerializerTestBase
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

	/// <summary>
	/// This enum has two members with the same value but different names.
	/// </summary>
	public enum EnumWithNonUniqueNames
	{
		One = 1,
		AnotherOne = 1,
		Two = 2,
	}

	public enum EnumWithRenamedValues
	{
		[EnumMemberShape(Name = "1st")]
		First,
		[EnumMemberShape(Name = "2nd")]
		Second,
	}

	public MessagePackType ExpectedType { get; set; }

	[Test]
	public async Task SimpleEnum()
	{
		await this.AssertEnum<Simple, Witness>(Simple.Two);
	}

	[Test]
	public async Task Enum_WithCaseInsensitiveCollisions()
	{
		await this.AssertEnum<CaseInsensitiveCollisions, Witness>(CaseInsensitiveCollisions.OnE);
		await this.AssertEnum<CaseInsensitiveCollisions, Witness>(CaseInsensitiveCollisions.One);
	}

	[Test]
	public async Task NonExistentValue_NonFlags()
	{
		this.ExpectedType = MessagePackType.Integer;
		await this.AssertEnum<Simple, Witness>((Simple)15);
	}

	[Test]
	public async Task NonExistentValue_Flags()
	{
		this.ExpectedType = MessagePackType.Integer;
		await this.AssertEnum<FlagsEnum, Witness>((FlagsEnum)15);
	}

	[Test]
	public async Task OneValueFromFlags()
	{
		await this.AssertEnum<FlagsEnum, Witness>(FlagsEnum.One);
	}

	[Test]
	public async Task MultipleFlags()
	{
		this.ExpectedType = MessagePackType.Integer;
		await this.AssertEnum<FlagsEnum, Witness>(FlagsEnum.One | FlagsEnum.Two);
	}

	[Test]
	public async Task NonUniqueNamesInEnum_Roundtrip()
	{
		await this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.AnotherOne);
		await this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.One);
	}

	private static ReadOnlySequence<byte> SerializeEnumName(string name)
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.Write(name);
		writer.Flush();
		return seq;
	}

	private async Task<ReadOnlySequence<byte>> AssertEnum<T, TWitness>(T value)
		where T : struct, Enum
#if NET
		where TWitness : IShapeable<T>
#endif
	{
		ReadOnlySequence<byte> msgpack = await this.AssertRoundtrip<T, TWitness>(value);
		await this.AssertType(msgpack, this.ExpectedType);
		this.Logger.LogTrace(value.ToString());
		return msgpack;
	}

	private async Task AssertType(ReadOnlySequence<byte> msgpack, MessagePackType expectedType)
	{
		MessagePackReader reader = new(msgpack);
		await Assert.That(reader.NextMessagePackType).IsEqualTo(expectedType);
	}

	[InheritsTests]
	public class EnumAsStringTests : EnumTests
	{
		public EnumAsStringTests()
		{
			this.Serializer = this.Serializer with { SerializeEnumValuesByName = true };
			this.ExpectedType = MessagePackType.String;
		}

		[Test]
		public async Task CaseInsensitiveByDefault()
		{
			await Assert.That(this.Serializer.Deserialize<Simple, Witness>(SerializeEnumName("ONE"), this.TimeoutToken)).IsEqualTo(Simple.One);
		}

		[Test]
		public async Task UnrecognizedName()
		{
			MessagePackSerializationException? ex = await Assert.ThrowsAsync<MessagePackSerializationException>(() => Task.Run(() => this.Serializer.Deserialize<Simple, Witness>(SerializeEnumName("FOO"), this.TimeoutToken)));
			await Assert.That(ex).IsNotNull();
			this.Logger.LogTrace(ex!.Message);
		}

		[Test]
		public async Task NonUniqueNamesInEnum_ParseEitherName()
		{
			await Assert.That(this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(SerializeEnumName(nameof(EnumWithNonUniqueNames.One)), this.TimeoutToken)).IsEqualTo(EnumWithNonUniqueNames.One);
			await Assert.That(this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(SerializeEnumName(nameof(EnumWithNonUniqueNames.AnotherOne)), this.TimeoutToken)).IsEqualTo(EnumWithNonUniqueNames.AnotherOne);
		}

		[Test]
		public async Task RenamedEnumValues()
		{
			ReadOnlySequence<byte> msgpack = await this.AssertEnum<EnumWithRenamedValues, Witness>(EnumWithRenamedValues.First);
			MessagePackReader reader = new(msgpack);
			string? actual = reader.ReadString();
			await Assert.That(actual).IsEqualTo("1st");
		}
	}

	[InheritsTests]
	public class EnumAsOrdinalTests : EnumTests
	{
		public EnumAsOrdinalTests()
		{
			this.Serializer = this.Serializer with { SerializeEnumValuesByName = false };
			this.ExpectedType = MessagePackType.Integer;
		}
	}

	[GenerateShapeFor<Simple>]
	[GenerateShapeFor<CaseInsensitiveCollisions>]
	[GenerateShapeFor<FlagsEnum>]
	[GenerateShapeFor<EnumWithNonUniqueNames>]
	[GenerateShapeFor<EnumWithRenamedValues>]
	private partial class Witness;
}
