// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public partial class BuiltInConverterTests : MessagePackSerializerTestBase
{
	private const string BadGuidFormatErrorMessage = "Not a recognized GUID format.";

	[Fact]
	public void SystemDrawingColor() => this.AssertRoundtrip<Color, Witness>(Color.FromArgb(1, 2, 3, 4));

	[Fact]
	public void SystemDrawingPoint() => this.AssertRoundtrip<Point, Witness>(new Point(1, 1));

	[Fact]
	public void ImmutableDictionary() => this.AssertRoundtrip(new HasImmutableDictionary() { Map = ImmutableDictionary<string, int>.Empty.Add("a", 1) });

#if NET

	[Fact]
	public void Int128()
	{
		this.AssertRoundtrip(new HasInt128(0));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasInt128(long.MaxValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasInt128(long.MinValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasInt128(new Int128(1, 2)));
		this.AssertType(MessagePackType.Extension);

		this.AssertRoundtrip(new HasInt128(System.Int128.MaxValue));
		this.AssertType(MessagePackType.Extension);

		this.AssertRoundtrip(new HasInt128(System.Int128.MinValue));
		this.AssertType(MessagePackType.Extension);
	}

	/// <summary>
	/// Verifies that we can read <see cref="Int128"/> values that use the Bin header, which is what MessagePack-CSharp's "native" formatter uses.
	/// </summary>
	/// <remarks>
	/// Note that while our <see cref="LibraryReservedMessagePackExtensionTypeCode.Int128"/> mandates big endian encoding to match msgpack conventions,
	/// The Bin encoding used by MessagePack-CSharp uses little-endian encoding.
	/// </remarks>
	[Fact]
	public void Int128_FromBin()
	{
		Int128 value = new(1, 2);
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasInt128.Value));
		writer.WriteBinHeader(128 / 8);
		Span<byte> byteSpan = writer.GetSpan(128 / 8);
		Assert.True(((IBinaryInteger<Int128>)value).TryWriteLittleEndian(byteSpan, out int written));
		writer.Advance(written);
		writer.Flush();

		Assert.Equal(value, this.Serializer.Deserialize<HasInt128>(seq, TestContext.Current.CancellationToken)!.Value);
	}

	[Fact]
	public void UInt128()
	{
		this.AssertRoundtrip(new HasUInt128(0));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasInt128(ulong.MaxValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasUInt128(new UInt128(1, 2)));
		this.AssertType(MessagePackType.Extension);

		this.AssertRoundtrip(new HasUInt128(System.UInt128.MaxValue));
		this.AssertType(MessagePackType.Extension);
	}

	/// <summary>
	/// Verifies that we can read <see cref="UInt128"/> values that use the Bin header, which is what MessagePack-CSharp's "native" formatter uses.
	/// </summary>
	/// <remarks>
	/// Note that while our <see cref="LibraryReservedMessagePackExtensionTypeCode.UInt128"/> mandates big endian encoding to match msgpack conventions,
	/// The Bin encoding used by MessagePack-CSharp uses little-endian encoding.
	/// </remarks>
	[Fact]
	public void UInt128_FromBin()
	{
		UInt128 value = new(1, 2);
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasUInt128.Value));
		writer.WriteBinHeader(128 / 8);
		Span<byte> byteSpan = writer.GetSpan(128 / 8);
		Assert.True(((IBinaryInteger<UInt128>)value).TryWriteLittleEndian(byteSpan, out int written));
		writer.Advance(written);
		writer.Flush();

		Assert.Equal(value, this.Serializer.Deserialize<HasUInt128>(seq, TestContext.Current.CancellationToken)!.Value);
	}

#endif

#if NET9_0_OR_GREATER

	[Fact]
	public void OrderedDictionary() => this.AssertRoundtrip(new HasOrderedDictionary() { Map = { ['a'] = 1, ['b'] = 2, ['c'] = 3 } });

#endif

	[Fact]
	public void Decimal()
	{
		this.AssertRoundtrip(new HasDecimal(1.2m));
		this.AssertRoundtrip(new HasDecimal(new decimal(ulong.MaxValue) * 1000));
		this.AssertRoundtrip(new HasDecimal(new decimal(ulong.MaxValue) * -1000));

		// Verify the actual encoding for one of these to lock it in.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(1, reader.ReadMapHeader());
		reader.Skip(default); // property name
		Extension decimalEncoding = reader.ReadExtension();
		Assert.Equal(this.Serializer.LibraryExtensionTypeCodes.Decimal, decimalEncoding.Header.TypeCode);
		Assert.Equal("00-00-00-80-E7-03-00-00-18-FC-FF-FF-FF-FF-FF-FF", BitConverter.ToString(decimalEncoding.Data.ToArray()));
	}

	/// <summary>
	/// Verifies that we can read <see cref="decimal"/> values that use the Bin header, which is what MessagePack-CSharp's "native" formatter uses.
	/// </summary>
	[Fact]
	public void Decimal_FromBin()
	{
		Assert.SkipUnless(BitConverter.IsLittleEndian, "This test is written assuming little-endian.");
		Span<decimal> value = [new decimal(ulong.MaxValue) * -1000];
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasDecimal.Value));
		writer.Write(MemoryMarshal.Cast<decimal, byte>(value));
		writer.Flush();

		Assert.Equal(value[0], this.Serializer.Deserialize<HasDecimal>(seq, TestContext.Current.CancellationToken)!.Value);
	}

	/// <summary>
	/// Verifies that we can read <see cref="decimal"/> values that use the UTF-8 encoding, which is what MessagePack-CSharp's default formatter uses.
	/// </summary>
	[Fact]
	public void Decimal_FromString()
	{
		decimal value = 1.2m;
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasDecimal.Value));
		writer.Write(value.ToString(CultureInfo.InvariantCulture));
		writer.Flush();

		Assert.Equal(value, this.Serializer.Deserialize<HasDecimal>(seq, TestContext.Current.CancellationToken)!.Value);
	}

	[Fact]
	public void BigInteger()
	{
		this.AssertRoundtrip(new HasBigInteger(1));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasBigInteger(ulong.MaxValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasBigInteger(ulong.MinValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasBigInteger(long.MaxValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasBigInteger(long.MinValue));
		this.AssertType(MessagePackType.Integer);

		this.AssertRoundtrip(new HasBigInteger(new BigInteger(ulong.MaxValue) * 3));
		this.AssertType(MessagePackType.Extension);
	}

	/// <summary>
	/// Verifies that we can read <see cref="decimal"/> values that use the Bin header, which is what MessagePack-CSharp's "native" formatter uses.
	/// </summary>
	[Fact]
	public void BigInteger_FromBin()
	{
		BigInteger value = new BigInteger(ulong.MaxValue) * 3;
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasBigInteger.Value));
		writer.Write(value.ToByteArray()); // Always in LE order even on BE machines.
		writer.Flush();

		Assert.Equal(value, this.Serializer.Deserialize<HasBigInteger>(seq, TestContext.Current.CancellationToken)!.Value);
	}

	[Fact]
	public void Guid()
	{
		// Test that Guid serialization works by default (using binary format)
		Guid value = System.Guid.NewGuid();
		this.Logger.WriteLine($"Randomly generated guid: {value}");
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new HasGuid(value));
		Assert.True(this.DataMatchesSchema(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<HasGuid>()));
	}

	[Fact]
	public void CultureInfo_Roundtrips()
	{
		Assert.Equal("fr-FR", this.Roundtrip<CultureInfo, Witness>(CultureInfo.GetCultureInfo("fr-FR"))?.Name);
		Assert.Equal("es", this.Roundtrip<CultureInfo, Witness>(CultureInfo.GetCultureInfo("es"))?.Name);
	}

	[Fact]
	public void CultureInfo_Encoding()
	{
		byte[] msgpack = this.Serializer.Serialize(CultureInfo.GetCultureInfo("es-ES"), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		MessagePackReader reader = new(msgpack);
		Assert.Equal("es-ES", reader.ReadString());
	}

	[Fact]
	public void Encoding_Roundtrip()
	{
		Assert.Equal(Encoding.UTF8.WebName, this.Roundtrip<Encoding, Witness>(Encoding.UTF8)?.WebName);
	}

	[Fact]
	public void Encoding_Encoding()
	{
		byte[] msgpack = this.Serializer.Serialize(Encoding.GetEncoding("utf-8"), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		MessagePackReader reader = new(msgpack);
		Assert.Equal("utf-8", reader.ReadString());
	}

	[Theory, PairwiseData]
	public void Guid_StringFormats(OptionalConverters.GuidStringFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);
		Guid value = System.Guid.NewGuid();
		this.Logger.WriteLine($"Randomly generated guid: {value}");
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new HasGuid(value));
		Assert.True(this.DataMatchesSchema(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<HasGuid>()));
	}

	[Theory, PairwiseData]
	public void Guid_ParseAnyStringFormat(OptionalConverters.GuidStringFormat format, bool uppercase)
	{
		// Serialize the original with the specified format.
		// Deserialize with any format besides the one we were expecting.
		(Guid before, Guid after) = this.RoundtripModifiedGuid(
			s => uppercase ? s.ToUpperInvariant() : s,
			this.Serializer.WithGuidConverter(format),
			this.Serializer.WithGuidConverter(format == OptionalConverters.GuidStringFormat.StringD ? OptionalConverters.GuidStringFormat.StringN : OptionalConverters.GuidStringFormat.StringD));

		Assert.Equal(before, after);
	}

	[Theory, PairwiseData]
	public void Guid_TruncatedInputs(OptionalConverters.GuidStringFormat format)
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
	public void Guid_MissingHexCharacter(OptionalConverters.GuidStringFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);

		// Remove a character that will be a hex character in any format.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.RoundtripModifiedGuid(s => s.Remove(6, 1)));
		this.Logger.WriteLine(ex.GetBaseException().Message);
		Assert.Equal(BadGuidFormatErrorMessage, ex.GetBaseException().Message);
	}

	/// <summary>
	/// Verifies that we can read <see cref="Guid"/> values that use the Bin header, which is what MessagePack-CSharp's "native" formatter uses.
	/// This tests interoperability with the default binary format.
	/// </summary>
	[Fact]
	public void Guid_FromBin()
	{
		Assert.SkipUnless(BitConverter.IsLittleEndian, "This test is written assuming little-endian.");

		Span<Guid> valueSpan = [System.Guid.NewGuid()];
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasGuid.Value));

		writer.Write(MemoryMarshal.Cast<Guid, byte>(valueSpan));
		writer.Flush();

		Assert.Equal(valueSpan[0], this.Serializer.Deserialize<HasGuid>(seq, TestContext.Current.CancellationToken)!.Value);
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

	[Theory, PairwiseData]
	public void DateTime_HiFi(DateTimeKind kind)
	{
		this.Serializer = this.Serializer.WithHiFiDateTime();
		DateTime original = new(2000, 1, 1, 2, 3, 4, 567, kind);
		HasDateTime? deserialized = this.Roundtrip(new HasDateTime(original));

		// Deserialized value should have retained the Kind.
		Assert.Equal(original.Kind, deserialized?.Value.Kind);

		// And in all other ways, be equal.
		Assert.Equal(original, deserialized?.Value);

		// Verify the actual encoding for each case.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		reader.ReadMapHeader();
		reader.ReadString(); // skip property name
		if (kind == DateTimeKind.Utc)
		{
			// Standard encoding.
			Assert.Equal(original, reader.ReadDateTime());
		}
		else
		{
			// HiFi encoding.
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(original.Ticks, reader.ReadInt64());
			Assert.Equal((int)original.Kind, reader.ReadInt32());
		}
	}

	[Fact]
	public void DateTimeOffset()
	{
		this.AssertRoundtrip(new HasDateTimeOffset(System.DateTimeOffset.Now));

		// Try specific offset values because CI/PR builds run on agents that run on the UTC time zone.
		this.AssertRoundtrip(new HasDateTimeOffset(System.DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(4))));
	}

	[Fact]
	public void ByteArrayWithOldSpec()
	{
		byte[] original = [1, 2, 3];
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq) { OldSpec = true };
		this.Serializer.Serialize(ref writer, original, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		writer.Flush();

		// Verify that the test is doing what we think it is.
		MessagePackReader reader = new(seq);
		Assert.Equal(MessagePackType.String, reader.NextMessagePackType);

		byte[]? deserialized = this.Serializer.Deserialize<byte[]>(seq, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal(original, deserialized);
	}

	[Fact]
	public void Extension() => this.AssertRoundtrip(new Extension(15, new byte[] { 1, 2, 3 }));

	private (Guid Before, Guid After) RoundtripModifiedGuid(Func<string, string> modifier, MessagePackSerializer? serializer = null, MessagePackSerializer? deserializer = null)
	{
		serializer ??= this.Serializer;
		deserializer ??= this.Serializer;

		Guid original = System.Guid.NewGuid();
		this.Logger.WriteLine($"Randomly generated guid: {original}");
		byte[] msgpack = serializer.Serialize(original, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		msgpack = this.Serializer.Serialize(
			modifier(this.Serializer.Deserialize<string>(msgpack, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken)!),
			Witness.GeneratedTypeShapeProvider,
			TestContext.Current.CancellationToken);
		this.LogMsgPack(msgpack);
		Guid deserialized = deserializer.Deserialize<Guid>(msgpack, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		return (original, deserialized);
	}

	private void AssertType(MessagePackType expectedType)
	{
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		reader.ReadMapHeader();
		reader.ReadString();
		Assert.Equal(expectedType, reader.NextMessagePackType);
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
	[GenerateShapeFor<byte[]>]
	[GenerateShapeFor<CultureInfo>]
	[GenerateShapeFor<Encoding>]
	private partial class Witness;
}
