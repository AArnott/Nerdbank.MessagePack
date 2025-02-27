﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ShapeShift.MessagePack;

public abstract partial class EnumTests(SerializerBase serializer, TokenType expectedType) : SerializerTestBase(serializer)
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

	public TokenType ExpectedType { get; set; } = expectedType;

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
		this.ExpectedType = TokenType.Integer;
		this.AssertEnum<Simple, Witness>((Simple)15);
	}

	[Fact]
	public void NonExistentValue_Flags()
	{
		this.ExpectedType = TokenType.Integer;
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
		this.ExpectedType = TokenType.Integer;
		this.AssertEnum<FlagsEnum, Witness>(FlagsEnum.One | FlagsEnum.Two);
	}

	[Fact]
	public void NonUniqueNamesInEnum_Roundtrip()
	{
		this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.AnotherOne);
		this.AssertEnum<EnumWithNonUniqueNames, Witness>(EnumWithNonUniqueNames.One);
	}

	private ReadOnlySequence<byte> SerializeEnumName(string name)
	{
		Sequence<byte> seq = new();
		Writer writer = new(seq, this.Serializer.Formatter);
		writer.Write(name);
		writer.Flush();
		this.LogFormattedBytes(seq);
		return seq;
	}

	private void AssertEnum<T, TWitness>(T value)
		where T : struct, Enum
#if NET
		where TWitness : IShapeable<T>
#endif
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<T, TWitness>(value);
		this.AssertType(msgpack, this.ExpectedType);
		this.Logger.WriteLine(value.ToString());
	}

	private void AssertType(ReadOnlySequence<byte> msgpack, TokenType expectedType)
	{
		Reader reader = new(msgpack, this.Serializer.Deformatter);
		Assert.Equal(expectedType, reader.NextTypeCode);
	}

	public abstract class Strings(SerializerBase serializer) : EnumTests(serializer with { SerializeEnumValuesByName = true }, TokenType.String)
	{
		[Fact]
		public void CaseInsensitiveByDefault()
		{
			Assert.Equal(Simple.One, this.Serializer.Deserialize<Simple, Witness>(this.SerializeEnumName("ONE"), TestContext.Current.CancellationToken));
		}

		[Fact]
		public void UnrecognizedName()
		{
			SerializationException ex = Assert.Throws<SerializationException>(() => this.Serializer.Deserialize<Simple, Witness>(this.SerializeEnumName("FOO"), TestContext.Current.CancellationToken));
			this.Logger.WriteLine(ex.Message);
		}

		[Fact]
		public void NonUniqueNamesInEnum_ParseEitherName()
		{
			Assert.Equal(EnumWithNonUniqueNames.One, this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(this.SerializeEnumName(nameof(EnumWithNonUniqueNames.One)), TestContext.Current.CancellationToken));
			Assert.Equal(EnumWithNonUniqueNames.AnotherOne, this.Serializer.Deserialize<EnumWithNonUniqueNames, Witness>(this.SerializeEnumName(nameof(EnumWithNonUniqueNames.AnotherOne)), TestContext.Current.CancellationToken));
		}
	}

	public abstract class Ordinals(SerializerBase serializer) : EnumTests(serializer with { SerializeEnumValuesByName = false }, TokenType.Integer);

	public class MsgPackStrings() : Strings(CreateMsgPackSerializer());

	public class JsonStrings() : Strings(CreateJsonSerializer());

	public class MsgPackOrdinals() : Ordinals(CreateMsgPackSerializer());

	public class JsonOrdinals() : Ordinals(CreateJsonSerializer());

	[GenerateShape<Simple>]
	[GenerateShape<CaseInsensitiveCollisions>]
	[GenerateShape<FlagsEnum>]
	[GenerateShape<EnumWithNonUniqueNames>]
	private partial class Witness;
}
