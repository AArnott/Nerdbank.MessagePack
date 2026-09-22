// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using Benchmarks.DataModels;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LargeDataModelBenchmark
{
	private static readonly MessagePackSerializer Serializer = new();
	private static readonly LargeDataModel Value = CreateValue();
	private static readonly byte[] SerializedValue = Serializer.Serialize(Value);
	private readonly ArrayBufferWriter<byte> buffer = new();

	[Benchmark]
	[BenchmarkCategory("large-data-model", "Serialize")]
	public void Serialize()
	{
		Serializer.Serialize(this.buffer, Value);
		this.buffer.Clear();
	}

	[Benchmark]
	[BenchmarkCategory("large-data-model", "Deserialize")]
	public LargeDataModel? Deserialize() => Serializer.Deserialize<LargeDataModel>(SerializedValue);

	private static LargeDataModel CreateValue()
	{
		DerivedRecord1 baseRecord = new()
		{
			Id = 1,
			Name = "base record",
			Description = "derived record",
			Numbers = [1, 127, 65_536],
		};
		ComplexRecord complexRecord = CreateComplexRecord(baseRecord);
		DerivedClass1 baseClass = new(2, "derived class")
		{
			BaseName = "base class",
			DerivedProp = "derived class",
			Records = [complexRecord],
		};
		AnotherRecord anotherRecord = new()
		{
			Id = 3,
			Classes = [baseClass],
		};

		return new LargeDataModel
		{
			BaseRec = baseRecord,
			BaseCls = baseClass,
			CompRec = complexRecord,
			CompRecStruct = CreateComplexRecordStruct(baseRecord),
			LargeRec = CreateLargeRecord(baseRecord, baseClass, anotherRecord),
			LargeRecStruct = CreateLargeRecordStruct(baseRecord, baseClass, anotherRecord),
			AnotherRecs = [anotherRecord],
			AnotherRecStructs = [new AnotherRecordStruct { Name = "record struct", Type = Enum1.Value2 }],
			EnumVal1 = Enum1.Value3,
			EnumVal2 = Enum2.Z,
			EnumVal3 = Enum3.Large,
			EnumVal4 = Enum4.Option30,
			EnumVal5 = Enum5.Third,
			EnumVal6 = Enum6.Item5,
			TinyEnumVal = TinyEnum.Yes,
			MediumEnumVal = MediumEnum.Delta,
			LargeEnumVal = LargeEnum.Value50,
		};
	}

	private static ComplexRecord CreateComplexRecord(BaseRecord baseRecord)
	{
		return new ComplexRecord
		{
			Prop1 = 42,
			Prop2 = "complex record",
			Prop3 = true,
			Prop4 = Math.PI,
			Prop5 = [new SimpleRecord { Value = 5 }],
			Prop6 = Enum1.Value2,
			Prop7 = new RecordStruct1 { X = 7, Y = "record struct" },
			Prop8 = "required",
			Prop9 = 9,
			Prop10 = 10.5f,
			Prop11 = 11_000_000_000,
			Prop12 = 12,
			Prop13 = 13,
			Prop14 = 'n',
			Prop15 = 15.25m,
			Prop16 = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
			Prop17 = new Guid("5D4575C4-20A6-4334-A49D-66339434A92E"),
			Prop18 = TimeSpan.FromMinutes(18),
			Prop19 = new Uri("https://example.test/complex"),
			Prop20 = ["one", "two"],
			Prop21 = new Dictionary<string, int> { ["key"] = 21 },
			Prop22 = Enum2.Q,
			Prop23 = new RecordStruct2 { Flag = true, Items = [new RecordStruct1 { X = 23, Y = "nested" }] },
			Prop24 = baseRecord,
		};
	}

	private static ComplexRecordStruct CreateComplexRecordStruct(BaseRecord baseRecord)
	{
		return new ComplexRecordStruct
		{
			A = 31,
			B = "complex struct",
			C = true,
			D = [new SimpleRecordStruct { Value = 3.25 }],
			E = Enum3.Medium,
			F = 36,
			G = 37.5f,
			H = 38_000_000_000,
			I = 39,
			J = 40,
			K = 's',
			L = 42.5m,
			M = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc),
			N = new Guid("A6FBE934-3A03-47E8-966C-118B8D97CB4A"),
			O = TimeSpan.FromSeconds(45),
			P = new Uri("https://example.test/struct"),
			Q = ["three", "four"],
			R = new Dictionary<string, int> { ["struct"] = 48 },
			S = Enum4.Option20,
			T = new RecordStruct1 { X = 50, Y = "value" },
			U = baseRecord,
		};
	}

	private static LargeRecord CreateLargeRecord(BaseRecord baseRecord, BaseClass baseClass, AnotherRecord anotherRecord)
	{
		return new LargeRecord
		{
			Prop1 = 51,
			Prop2 = "large record",
			Prop3 = true,
			Prop4 = 54.5,
			Prop5 = [new MediumRecord { A = 55, B = "medium", C = true, D = [new TinyRecord { Value = 56 }], E = Enum5.Second }],
			Prop6 = Enum2.P,
			Prop7 = new RecordStruct1 { X = 57, Y = "large" },
			Prop8 = "required large",
			Prop9 = 59,
			Prop10 = 60.5f,
			Prop11 = 61_000_000_000,
			Prop12 = 62,
			Prop13 = 63,
			Prop14 = 'l',
			Prop15 = 65.5m,
			Prop16 = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc),
			Prop17 = new Guid("3D6E8DD2-8B00-4ED8-9083-58B3146CA8AB"),
			Prop18 = TimeSpan.FromHours(1),
			Prop19 = new Uri("https://example.test/large"),
			Prop20 = ["five", "six"],
			Prop21 = new Dictionary<string, int> { ["large"] = 69 },
			Prop22 = Enum4.Option10,
			Prop23 = new RecordStruct2 { Flag = true, Items = [new RecordStruct1 { X = 71, Y = "item" }] },
			Prop24 = baseRecord,
			Prop25 = baseClass,
			Prop26 = anotherRecord,
			Prop27 = new AnotherRecordStruct { Name = "nested struct", Type = Enum1.Value1 },
			Prop28 = TinyEnum.No,
			Prop29 = MediumEnum.Gamma,
			Prop30 = [],
		};
	}

	private static LargeRecordStruct CreateLargeRecordStruct(BaseRecord baseRecord, BaseClass baseClass, AnotherRecord anotherRecord)
	{
		return new LargeRecordStruct
		{
			A = 81,
			B = "large struct",
			C = true,
			D = [new MediumRecordStruct { X = 82.5, Y = [new TinyRecordStruct { Flag = true }], Z = Enum6.Item4 }],
			E = Enum3.Small,
			F = 86,
			G = 87.5f,
			H = 88_000_000_000,
			I = 89,
			J = 90,
			K = 't',
			L = 92.5m,
			M = new DateTime(2025, 4, 5, 6, 7, 8, DateTimeKind.Utc),
			N = new Guid("00CFE314-4DC2-4E29-BA71-8478DA3DE2FA"),
			O = TimeSpan.FromDays(1),
			P = new Uri("https://example.test/large-struct"),
			Q = ["seven", "eight"],
			R = new Dictionary<string, int> { ["large struct"] = 98 },
			S = Enum4.Option5,
			T = new RecordStruct1 { X = 100, Y = "struct value" },
			U = baseRecord,
			V = baseClass,
			W = anotherRecord,
			X = new AnotherRecordStruct { Name = "deep struct", Type = Enum1.Value3 },
			Y = TinyEnum.Yes,
			Z = MediumEnum.Beta,
			AA = [],
		};
	}
}

#endif
