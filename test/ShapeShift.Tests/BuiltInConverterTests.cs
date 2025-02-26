// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.Numerics;

public partial class BuiltInConverterTests : MessagePackSerializerTestBase
{
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

	[GenerateShape<Point>]
	[GenerateShape<Color>]
	private partial class Witness;
}
