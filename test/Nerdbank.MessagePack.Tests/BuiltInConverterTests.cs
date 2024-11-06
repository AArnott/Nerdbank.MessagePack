// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

public partial class BuiltInConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void ImmutableDictionary() => this.AssertRoundtrip(new HasImmutableDictionary() { Map = { { "a", 1 } } });

	[Fact]
	public void Int128() => this.AssertRoundtrip(new HasInt128(new Int128(1, 2)));

	[Fact]
	public void UInt128() => this.AssertRoundtrip(new HasUInt128(new UInt128(1, 2)));

	[Fact]
	public void Decimal() => this.AssertRoundtrip(new HasDecimal(1.2m));

	[Fact]
	public void BigInteger() => this.AssertRoundtrip(new HasBigInteger(1));

	[Fact]
	public void Guid() => this.AssertRoundtrip(new HasGuid(System.Guid.NewGuid()));

	[Fact]
	public void DateTime() => this.AssertRoundtrip(new HasDateTime(System.DateTime.UtcNow));

	[Fact]
	public void DateTimeOffset() => this.AssertRoundtrip(new HasDateTimeOffset(System.DateTimeOffset.Now));

	[GenerateShape]
	public partial class HasImmutableDictionary : IEquatable<HasImmutableDictionary>
	{
		public ImmutableDictionary<string, int> Map { get; set; } = ImmutableDictionary<string, int>.Empty;

		public bool Equals(HasImmutableDictionary? other) => ByValueEquality.Equal(this.Map, other?.Map);
	}

	[GenerateShape]
	public partial record HasInt128(Int128 Value);

	[GenerateShape]
	public partial record HasUInt128(UInt128 Value);

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
}
