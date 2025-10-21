// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1516 // Elements must be separated by blank line

namespace Benchmarks.DataModels;

public enum Enum1
{
	Value1,
	Value2,
	Value3,
}

public enum Enum2
{
	A,
	B,
	C,
	D,
	E,
	F,
	G,
	H,
	I,
	J,
	K,
	L,
	M,
	N,
	O,
	P,
	Q,
	R,
	S,
	T,
	U,
	V,
	W,
	X,
	Y,
	Z,
}

public enum Enum3
{
	Small,
	Medium,
	Large,
}

public enum Enum4
{
	Option1,
	Option2,
	Option3,
	Option4,
	Option5,
	Option6,
	Option7,
	Option8,
	Option9,
	Option10,
	Option11,
	Option12,
	Option13,
	Option14,
	Option15,
	Option16,
	Option17,
	Option18,
	Option19,
	Option20,
	Option21,
	Option22,
	Option23,
	Option24,
	Option25,
	Option26,
	Option27,
	Option28,
	Option29,
	Option30,
}

public enum Enum5
{
	First,
	Second,
	Third,
}

public enum Enum6
{
	Item1,
	Item2,
	Item3,
	Item4,
	Item5,
}

public enum TinyEnum
{
	Yes,
	No,
}

public enum MediumEnum
{
	Alpha,
	Beta,
	Gamma,
	Delta,
}

public enum LargeEnum
{
	Value01,
	Value02,
	Value03,
	Value04,
	Value05,
	Value06,
	Value07,
	Value08,
	Value09,
	Value10,
	Value11,
	Value12,
	Value13,
	Value14,
	Value15,
	Value16,
	Value17,
	Value18,
	Value19,
	Value20,
	Value21,
	Value22,
	Value23,
	Value24,
	Value25,
	Value26,
	Value27,
	Value28,
	Value29,
	Value30,
	Value31,
	Value32,
	Value33,
	Value34,
	Value35,
	Value36,
	Value37,
	Value38,
	Value39,
	Value40,
	Value41,
	Value42,
	Value43,
	Value44,
	Value45,
	Value46,
	Value47,
	Value48,
	Value49,
	Value50,
}

public record struct AnotherRecordStruct
{
	public string? Name { get; set; }
	public Enum1 Type { get; set; }
}

public record struct RecordStruct1
{
	public int X { get; set; }
	public string? Y { get; set; }
}

public record struct RecordStruct2
{
	public bool Flag { get; set; }
	public List<RecordStruct1>? Items { get; set; }
}

public record struct SimpleRecordStruct
{
	public double Value { get; set; }
}

public record struct ComplexRecordStruct
{
	public int A { get; set; }
	public string? B { get; set; }
	public bool C { get; set; }
	public List<SimpleRecordStruct>? D { get; set; }
	public Enum3 E { get; set; }
	public required int F { get; init; }
	public float G { get; set; }
	public long H { get; set; }
	public short I { get; set; }
	public byte J { get; set; }
	public char K { get; set; }
	public decimal L { get; set; }
	public DateTime M { get; set; }
	public Guid N { get; set; }
	public TimeSpan O { get; set; }
	public Uri? P { get; set; }
	public List<string>? Q { get; set; }
	public Dictionary<string, int>? R { get; set; }
	public Enum4 S { get; set; }
	public RecordStruct1 T { get; set; }
	public BaseRecord? U { get; set; }
}

public record struct TinyRecordStruct
{
	public bool Flag { get; set; }
}

public record struct MediumRecordStruct
{
	public double X { get; set; }
	public List<TinyRecordStruct>? Y { get; set; }
	public Enum6 Z { get; set; }
}

public record struct LargeRecordStruct
{
	public int A { get; set; }
	public string? B { get; set; }
	public bool C { get; set; }
	public List<MediumRecordStruct>? D { get; set; }
	public Enum3 E { get; set; }
	public required int F { get; init; }
	public float G { get; set; }
	public long H { get; set; }
	public short I { get; set; }
	public byte J { get; set; }
	public char K { get; set; }
	public decimal L { get; set; }
	public DateTime M { get; set; }
	public Guid N { get; set; }
	public TimeSpan O { get; set; }
	public Uri? P { get; set; }
	public List<string>? Q { get; set; }
	public Dictionary<string, int>? R { get; set; }
	public Enum4 S { get; set; }
	public RecordStruct1 T { get; set; }
	public BaseRecord? U { get; set; }
	public BaseClass? V { get; set; }
	public AnotherRecord? W { get; set; }
	public AnotherRecordStruct X { get; set; }
	public TinyEnum Y { get; set; }
	public MediumEnum Z { get; set; }
	public List<LargeRecordStruct>? AA { get; set; }
}

[GenerateShape]
public partial record LargeDataModel
{
	public BaseRecord? BaseRec { get; set; }
	public BaseClass? BaseCls { get; set; }
	public ComplexRecord? CompRec { get; set; }
	public ComplexRecordStruct CompRecStruct { get; set; }
	public LargeRecord? LargeRec { get; set; }
	public LargeRecordStruct LargeRecStruct { get; set; }
	public List<AnotherRecord>? AnotherRecs { get; set; }
	public List<AnotherRecordStruct>? AnotherRecStructs { get; set; }
	public Enum1 EnumVal1 { get; set; }
	public Enum2 EnumVal2 { get; set; }
	public Enum3 EnumVal3 { get; set; }
	public Enum4 EnumVal4 { get; set; }
	public Enum5 EnumVal5 { get; set; }
	public Enum6 EnumVal6 { get; set; }
	public TinyEnum TinyEnumVal { get; set; }
	public MediumEnum MediumEnumVal { get; set; }
	public LargeEnum LargeEnumVal { get; set; }
}

[DerivedTypeShape(typeof(DerivedRecord1))]
[DerivedTypeShape(typeof(DerivedRecord2))]
public abstract record BaseRecord
{
	public int Id { get; set; }
	public string? Name { get; set; }
}

public record DerivedRecord1 : BaseRecord
{
	public required string Description { get; init; }
	public List<int>? Numbers { get; set; }
}

public record DerivedRecord2 : BaseRecord
{
	public bool IsActive { get; set; }
	public DerivedRecord1? SubRecord { get; set; }
}

public record SimpleRecord
{
	public int Value { get; set; }
}

public record ComplexRecord
{
	public int Prop1 { get; set; }
	public string? Prop2 { get; set; }
	public bool Prop3 { get; set; }
	public double Prop4 { get; set; }
	public List<SimpleRecord>? Prop5 { get; set; }
	public Enum1 Prop6 { get; set; }
	public RecordStruct1 Prop7 { get; set; }
	public required string Prop8 { get; init; }
	public int? Prop9 { get; set; }
	public float Prop10 { get; set; }
	public long Prop11 { get; set; }
	public short Prop12 { get; set; }
	public byte Prop13 { get; set; }
	public char Prop14 { get; set; }
	public decimal Prop15 { get; set; }
	public DateTime Prop16 { get; set; }
	public Guid Prop17 { get; set; }
	public TimeSpan Prop18 { get; set; }
	public Uri? Prop19 { get; set; }
	public List<string>? Prop20 { get; set; }
	public Dictionary<string, int>? Prop21 { get; set; }
	public Enum2 Prop22 { get; set; }
	public RecordStruct2 Prop23 { get; set; }
	public BaseRecord? Prop24 { get; set; }
}

[DerivedTypeShape(typeof(DerivedClass1))]
[DerivedTypeShape(typeof(DerivedClass2))]
public class BaseClass
{
	public BaseClass(int baseId)
	{
		this.BaseId = baseId;
	}

	public int BaseId { get; set; }
	public string? BaseName { get; set; }
}

public class DerivedClass1 : BaseClass
{
	public DerivedClass1(int baseId, string derivedProp)
		: base(baseId)
	{
		this.DerivedProp = derivedProp;
	}

	public required string DerivedProp { get; init; }
	public List<ComplexRecord>? Records { get; set; }
}

public class DerivedClass2 : BaseClass
{
	public DerivedClass2(int baseId, bool isDerived)
		: base(baseId)
	{
		this.IsDerived = isDerived;
	}

	public bool IsDerived { get; set; }
	public DerivedClass1? SubDerived { get; set; }
}

public record AnotherRecord
{
	public int Id { get; set; }
	public List<BaseClass>? Classes { get; set; }
}

public record TinyRecord
{
	public int Value { get; set; }
}

public record MediumRecord
{
	public int A { get; set; }
	public string? B { get; set; }
	public bool C { get; set; }
	public List<TinyRecord>? D { get; set; }
	public Enum5 E { get; set; }
}

public record LargeRecord
{
	public int Prop1 { get; set; }
	public string? Prop2 { get; set; }
	public bool Prop3 { get; set; }
	public double Prop4 { get; set; }
	public List<MediumRecord>? Prop5 { get; set; }
	public Enum2 Prop6 { get; set; }
	public RecordStruct1 Prop7 { get; set; }
	public required string Prop8 { get; init; }
	public int? Prop9 { get; set; }
	public float Prop10 { get; set; }
	public long Prop11 { get; set; }
	public short Prop12 { get; set; }
	public byte Prop13 { get; set; }
	public char Prop14 { get; set; }
	public decimal Prop15 { get; set; }
	public DateTime Prop16 { get; set; }
	public Guid Prop17 { get; set; }
	public TimeSpan Prop18 { get; set; }
	public Uri? Prop19 { get; set; }
	public List<string>? Prop20 { get; set; }
	public Dictionary<string, int>? Prop21 { get; set; }
	public Enum4 Prop22 { get; set; }
	public RecordStruct2 Prop23 { get; set; }
	public BaseRecord? Prop24 { get; set; }
	public BaseClass? Prop25 { get; set; }
	public AnotherRecord? Prop26 { get; set; }
	public AnotherRecordStruct Prop27 { get; set; }
	public TinyEnum Prop28 { get; set; }
	public MediumEnum Prop29 { get; set; }
	public List<LargeRecord>? Prop30 { get; set; }
}
