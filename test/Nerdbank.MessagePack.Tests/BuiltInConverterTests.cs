﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.Numerics;

public partial class BuiltInConverterTests : MessagePackSerializerTestBase
{
	private const string BadGuidFormatErrorMessage = "Not a recognized GUID format.";

	public static OptionalConverters.GuidFormat[] GuidStringFormats =>
	[
		OptionalConverters.GuidFormat.StringD,
		OptionalConverters.GuidFormat.StringN,
		OptionalConverters.GuidFormat.StringB,
		OptionalConverters.GuidFormat.StringP,
		OptionalConverters.GuidFormat.StringX,
	];

	[Fact]
	public void SystemDrawingColor() => this.AssertRoundtrip<Color, Witness>(Color.FromArgb(1, 2, 3, 4));

	[Fact]
	public void SystemDrawingPoint() => this.AssertRoundtrip<Point, Witness>(new Point(1, 1));

	[Fact]
	public void ImmutableDictionary() => this.AssertRoundtrip(new HasImmutableDictionary() { Map = ImmutableDictionary<string, int>.Empty.Add("a", 1) });

#if NET

	[Fact]
	public void Int128() => this.AssertRoundtrip(new HasInt128(new Int128(1, 2)));

	[Fact]
	public void UInt128() => this.AssertRoundtrip(new HasUInt128(new UInt128(1, 2)));

#endif

#if NET9_0_OR_GREATER

	[Fact]
	public void OrderedDictionary() => this.AssertRoundtrip(new HasOrderedDictionary() { Map = { ['a'] = 1, ['b'] = 2, ['c'] = 3 } });

#endif

	[Fact]
	public void Decimal() => this.AssertRoundtrip(new HasDecimal(1.2m));

	[Fact]
	public void BigInteger() => this.AssertRoundtrip(new HasBigInteger(1));

	[Theory, PairwiseData]
	public void Guid(OptionalConverters.GuidFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);
		Guid value = System.Guid.NewGuid();
		this.Logger.WriteLine($"Randomly generated guid: {value}");
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new HasGuid(value));
		Assert.True(this.DataMatchesSchema(msgpack, Witness.ShapeProvider.Resolve<HasGuid>()));
	}

	[Theory, PairwiseData]
	public void Guid_ParseAnyStringFormat([CombinatorialMemberData(nameof(GuidStringFormats))] OptionalConverters.GuidFormat format, bool uppercase)
	{
		// Serialize the original with the specified format.
		// Deserialize with any format besides the one we were expecting.
		(Guid before, Guid after) = this.RoundtripModifiedGuid(
			s => uppercase ? s.ToUpperInvariant() : s,
			this.Serializer.WithGuidConverter(format),
			this.Serializer.WithGuidConverter(format == OptionalConverters.GuidFormat.StringD ? OptionalConverters.GuidFormat.StringN : OptionalConverters.GuidFormat.StringD));

		Assert.Equal(before, after);
	}

	[Theory, PairwiseData]
	public void Guid_TruncatedInputs([CombinatorialMemberData(nameof(GuidStringFormats))] OptionalConverters.GuidFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);

		bool empty = false;
		int trimCount = 1;
		while (!empty)
		{
			MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.RoundtripModifiedGuid(s =>
			{
				empty = s.Length == trimCount;
				return s[..^trimCount++];
			}));
			Assert.Equal(BadGuidFormatErrorMessage, ex.GetBaseException().Message);
		}
	}

	[Theory, PairwiseData]
	public void Guid_MissingHexCharacter([CombinatorialMemberData(nameof(GuidStringFormats))] OptionalConverters.GuidFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);

		// Remove a character that will be a hex character in any format.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.RoundtripModifiedGuid(s => s.Remove(6, 1)));
		this.Logger.WriteLine(ex.GetBaseException().Message);
		Assert.Equal(BadGuidFormatErrorMessage, ex.GetBaseException().Message);
	}

	[Fact]
	public void DateTime() => this.AssertRoundtrip(new HasDateTime(System.DateTime.UtcNow));

	[Fact]
	public void DateTime_Unspecified()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() =>
			this.Serializer.Serialize(new HasDateTime(new System.DateTime(2000, 1, 1)), TestContext.Current.CancellationToken));
		ArgumentException inner = Assert.IsType<ArgumentException>(ex.GetBaseException());
		this.Logger.WriteLine(inner.Message);
	}

	[Theory]
	[InlineData(DateTimeKind.Utc)]
	[InlineData(DateTimeKind.Local)]
	public void DateTime_Unspecified_UnderConfiguration(DateTimeKind kind)
	{
		this.Serializer = this.Serializer.WithAssumedDateTimeKind(kind);
		DateTime original = new(2000, 1, 1); // Unspecified kind.
		HasDateTime? deserialized = this.Roundtrip(new HasDateTime(original));

		// Deserialized value should always be UTC.
		Assert.Equal(System.DateTime.SpecifyKind(original, kind).ToUniversalTime(), deserialized?.Value);
		Assert.Equal(DateTimeKind.Utc, deserialized?.Value.Kind);
	}

	[Fact]
	public void DateTimeOffset() => this.AssertRoundtrip(new HasDateTimeOffset(System.DateTimeOffset.Now));

	private (Guid Before, Guid After) RoundtripModifiedGuid(Func<string, string> modifier, MessagePackSerializer? serializer = null, MessagePackSerializer? deserializer = null)
	{
		serializer ??= this.Serializer;
		deserializer ??= this.Serializer;

		Guid original = System.Guid.NewGuid();
		this.Logger.WriteLine($"Randomly generated guid: {original}");
		byte[] msgpack = serializer.Serialize(original, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		msgpack = this.Serializer.Serialize(
			modifier(this.Serializer.Deserialize<string>(msgpack, Witness.ShapeProvider, TestContext.Current.CancellationToken)!),
			Witness.ShapeProvider,
			TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);
		Guid deserialized = deserializer.Deserialize<Guid>(msgpack, Witness.ShapeProvider, TestContext.Current.CancellationToken);
		return (original, deserialized);
	}

	[GenerateShape]
	public partial class HasImmutableDictionary : IEquatable<HasImmutableDictionary>
	{
		public ImmutableDictionary<string, int> Map { get; set; } = ImmutableDictionary<string, int>.Empty;

		public bool Equals(HasImmutableDictionary? other) => StructuralEquality.Equal(this.Map, other?.Map);
	}

#if NET

	[GenerateShape]
	public partial record HasInt128(Int128 Value);

	[GenerateShape]
	public partial record HasUInt128(UInt128 Value);

#endif

#if NET9_0_OR_GREATER

	[GenerateShape]
	public partial class HasOrderedDictionary : IEquatable<HasOrderedDictionary>
	{
		public OrderedDictionary<char, int> Map { get; set; } = new();

		public bool Equals(HasOrderedDictionary? other) => StructuralEquality.Equal((IReadOnlyList<KeyValuePair<char, int>>)this.Map, other?.Map);
	}

#endif

	[GenerateShape]
	public partial record HasDecimal(decimal Value);

	[GenerateShape]
	public partial record HasBigInteger(BigInteger Value);

	[GenerateShape]
	public partial record HasGuid(Guid Value);

	[GenerateShape]
	public partial record HasDateTime(DateTime Value);

	[GenerateShape]
	public partial record HasDateTimeOffset(DateTimeOffset Value);

	[GenerateShapeFor<Guid>]
	[GenerateShapeFor<Point>]
	[GenerateShapeFor<Color>]
	private partial class Witness;
}
