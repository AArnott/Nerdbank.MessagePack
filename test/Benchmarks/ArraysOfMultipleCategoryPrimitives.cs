// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// long, int, short, sbyte, ulong, uint, and ushort values are classified into multiple categories (UInt64, Fix and so on) and serialized into variable-length binary according to each of their values.
/// Simple Random.GetBytes value generation tends to generate the values which is serialized into longest binary.
/// Since such bias is bad because it leads to over-optimization, this benchmark class is introduced.
/// Multiple category forces serializer code serializing all categories.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public partial class ArraysOfMultipleCategoryPrimitives
{
	private const int Length = 10_000;
	private static readonly MessagePackSerializer AcceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = false };
	private static readonly MessagePackSerializer UnacceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = true };
	private static readonly MessagePackSerializer TestSerializer = new() { SerializeDefaultValues = true };
	private static readonly sbyte[] Int8Values = GetRandomValues<sbyte>(Length);
	private static readonly sbyte[] Int8ValuesMultipleCategory = GetRandomInt8Values2Category(Length);
	private static readonly byte[] Int8ValuesMsgPack = TestSerializer.Serialize<sbyte[], Witness>(Int8Values);
	private static readonly byte[] Int8ValuesMsgPackMultipleCategory = TestSerializer.Serialize<sbyte[], Witness>(Int8ValuesMultipleCategory);
	private static readonly short[] Int16Values = GetRandomValues<short>(Length);
	private static readonly short[] Int16ValuesMultipleCategory = GetRandomInt16Values5Category(Length);
	private static readonly byte[] Int16ValuesMsgPack = TestSerializer.Serialize<short[], Witness>(Int16Values);
	private static readonly byte[] Int16ValuesMsgPackMultipleCategory = TestSerializer.Serialize<short[], Witness>(Int16ValuesMultipleCategory);
	private static readonly int[] Int32Values = GetRandomValues<int>(Length);
	private static readonly int[] Int32ValuesMultipleCategory = GetRandomInt32Values7Category(Length);
	private static readonly byte[] Int32ValuesMsgPack = TestSerializer.Serialize<int[], Witness>(Int32Values);
	private static readonly byte[] Int32ValuesMsgPackMultipleCategory = TestSerializer.Serialize<int[], Witness>(Int32ValuesMultipleCategory);
	private static readonly long[] Int64Values = GetRandomValues<long>(Length);
	private static readonly long[] Int64ValuesMultipleCategory = GetRandomInt64Values9Category(Length);
	private static readonly byte[] Int64ValuesMsgPack = TestSerializer.Serialize<long[], Witness>(Int64Values);
	private static readonly byte[] Int64ValuesMsgPackMultipleCategory = TestSerializer.Serialize<long[], Witness>(Int64ValuesMultipleCategory);
	private static readonly ushort[] UInt16Values = GetRandomValues<ushort>(Length);
	private static readonly ushort[] UInt16ValuesMultipleCategory = GetRandomUInt16Values3Category(Length);
	private static readonly byte[] UInt16ValuesMsgPack = TestSerializer.Serialize<ushort[], Witness>(UInt16Values);
	private static readonly byte[] UInt16ValuesMsgPackMultipleCategory = TestSerializer.Serialize<ushort[], Witness>(UInt16ValuesMultipleCategory);
	private static readonly uint[] UInt32Values = GetRandomValues<uint>(Length);
	private static readonly uint[] UInt32ValuesMultipleCategory = GetRandomUInt32Values4Category(Length);
	private static readonly byte[] UInt32ValuesMsgPack = TestSerializer.Serialize<uint[], Witness>(UInt32Values);
	private static readonly byte[] UInt32ValuesMsgPackMultipleCategory = TestSerializer.Serialize<uint[], Witness>(UInt32ValuesMultipleCategory);
	private static readonly ulong[] UInt64Values = GetRandomValues<ulong>(Length);
	private static readonly ulong[] UInt64ValuesMultipleCategory = GetRandomUInt64Values5Category(Length);
	private static readonly byte[] UInt64ValuesMsgPack = TestSerializer.Serialize<ulong[], Witness>(UInt64Values);
	private static readonly byte[] UInt64ValuesMsgPackMultipleCategory = TestSerializer.Serialize<ulong[], Witness>(UInt64ValuesMultipleCategory);
	private readonly Sequence buffer = new();

	[ParamsAllValues(Priority = 1)]
	public bool Accelerated { get; set; }

	[ParamsAllValues(Priority = 0)]
	public bool MultipleCategory { get; set; }

	public MessagePackSerializer Serializer => this.Accelerated ? AcceleratedSerializer : UnacceleratedSerializer;

	[Benchmark]
	[BenchmarkCategory("sbyte", "deserialize")]
	public sbyte[]? Int8_Deserialize()
	{
		return this.Serializer.Deserialize<sbyte[], Witness>(this.MultipleCategory ? Int8ValuesMsgPackMultipleCategory : Int8ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("short", "deserialize")]
	public short[]? Int16_Deserialize()
	{
		return this.Serializer.Deserialize<short[], Witness>(this.MultipleCategory ? Int16ValuesMsgPackMultipleCategory : Int16ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("int", "deserialize")]
	public int[]? Int32_Deserialize()
	{
		return this.Serializer.Deserialize<int[], Witness>(this.MultipleCategory ? Int32ValuesMsgPackMultipleCategory : Int32ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("long", "deserialize")]
	public long[]? Int64_Deserialize()
	{
		return this.Serializer.Deserialize<long[], Witness>(this.MultipleCategory ? Int64ValuesMsgPackMultipleCategory : Int64ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("ushort", "deserialize")]
	public ushort[]? UInt16_Deserialize()
	{
		return this.Serializer.Deserialize<ushort[], Witness>(this.MultipleCategory ? UInt16ValuesMsgPackMultipleCategory : UInt16ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("uint", "deserialize")]
	public uint[]? UInt32_Deserialize()
	{
		return this.Serializer.Deserialize<uint[], Witness>(this.MultipleCategory ? UInt32ValuesMsgPackMultipleCategory : UInt32ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("ulong", "deserialize")]
	public ulong[]? UInt64_Deserialize()
	{
		return this.Serializer.Deserialize<ulong[], Witness>(this.MultipleCategory ? UInt64ValuesMsgPackMultipleCategory : UInt64ValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("int8", "serialize")]
	public void Int8_Serialize()
	{
		this.Serializer.Serialize<sbyte[], Witness>(this.buffer, this.MultipleCategory ? Int8ValuesMultipleCategory : Int8Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int16", "serialize")]
	public void Int16_Serialize()
	{
		this.Serializer.Serialize<short[], Witness>(this.buffer, this.MultipleCategory ? Int16ValuesMultipleCategory : Int16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize()
	{
		this.Serializer.Serialize<int[], Witness>(this.buffer, this.MultipleCategory ? Int32ValuesMultipleCategory : Int32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int64", "serialize")]
	public void Int64_Serialize()
	{
		this.Serializer.Serialize<long[], Witness>(this.buffer, this.MultipleCategory ? Int64ValuesMultipleCategory : Int64Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint16", "serialize")]
	public void UInt16_Serialize()
	{
		this.Serializer.Serialize<ushort[], Witness>(this.buffer, this.MultipleCategory ? UInt16ValuesMultipleCategory : UInt16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint32", "serialize")]
	public void UInt32_Serialize()
	{
		this.Serializer.Serialize<uint[], Witness>(this.buffer, this.MultipleCategory ? UInt32ValuesMultipleCategory : UInt32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint64", "serialize")]
	public void UInt64_Serialize()
	{
		this.Serializer.Serialize<ulong[], Witness>(this.buffer, this.MultipleCategory ? UInt64ValuesMultipleCategory : UInt64Values);
		this.buffer.Reset();
	}

	private static unsafe T[] GetRandomValues<T>(int length)
		where T : unmanaged
	{
		byte[] random = new byte[length * sizeof(T)];
		new Random(123).NextBytes(random); // use a fixed seed for reproducibility
		var values = new T[length];
		Unsafe.CopyBlock(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(values)), ref MemoryMarshal.GetArrayDataReference(random), (uint)random.Length);
		return values;
	}

	private static ulong[] GetRandomUInt64Values5Category(int length)
	{
		ulong[] random = new ulong[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories); // use a fixed seed for reproducibility
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (categories[(i * 2) + 1] % 5) switch
			{
				0 => ulong.MaxValue - categories[i * 2], // UInt64
				1 => uint.MaxValue - (ulong)categories[i * 2], // UInt32
				2 => ushort.MaxValue - (ulong)categories[i * 2], // UInt16
				3 => (categories[i * 2] & 0x7FU) | 0x80U, // UInt8
				_ => categories[i * 2] & 0x7FU, // Fixed
			};
		}

		return random;
	}

	private static uint[] GetRandomUInt32Values4Category(int length)
	{
		uint[] random = new uint[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories); // use a fixed seed for reproducibility
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (categories[(i * 2) + 1] % 5) switch
			{
				0 => uint.MaxValue - categories[i * 2], // UInt32
				1 => ushort.MaxValue - (uint)categories[i * 2], // UInt16
				2 => (categories[i * 2] & 0x7FU) | 0x80U, // UInt8
				_ => categories[i * 2] & 0x7FU, // Fixed
			};
		}

		return random;
	}

	private static ushort[] GetRandomUInt16Values3Category(int length)
	{
		ushort[] random = new ushort[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories); // use a fixed seed for reproducibility
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (ushort)((categories[(i * 2) + 1] % 5) switch
			{
				0 => ushort.MaxValue - categories[i * 2], // UInt16
				1 => (categories[i * 2] & 0x7F) | 0x80, // UInt8
				_ => categories[i * 2] & 0x7F, // Fixed
			});
		}

		return random;
	}

	private static long[] GetRandomInt64Values9Category(int length)
	{
		long[] random = new long[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories);
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (categories[(i * 2) + 1] % 9) switch
			{
				0 => long.MaxValue - categories[i * 2], // UInt64
				1 => uint.MaxValue - categories[i * 2], // UInt32
				2 => ushort.MaxValue - categories[i * 2], // UInt16
				3 => (categories[i * 2] & 0x7FU) | 0x80U, // UInt8
				4 => categories[i * 2] & 0x7FU, // Fixed
				5 => sbyte.MinValue + (categories[i * 2] & 0x3FL), // Int8
				6 => short.MinValue + categories[i * 2], // Int16
				7 => int.MinValue + categories[i * 2], // Int32
				_ => long.MinValue + categories[i * 2], // Int64
			};
		}

		return random;
	}

	private static int[] GetRandomInt32Values7Category(int length)
	{
		int[] random = new int[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories);
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (int)((categories[(i * 2) + 1] % 9) switch
			{
				0 => int.MaxValue - categories[i * 2], // UInt32
				1 => ushort.MaxValue - categories[i * 2], // UInt16
				2 => (categories[i * 2] & 0x7F) | 0x80, // UInt8
				3 => categories[i * 2] & 0x7F, // Fixed
				4 => sbyte.MinValue + (categories[i * 2] & 0x3F), // Int8
				5 => short.MinValue + categories[i * 2], // Int16
				_ => int.MinValue + categories[i * 2], // Int324
			});
		}

		return random;
	}

	private static short[] GetRandomInt16Values5Category(int length)
	{
		short[] random = new short[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories);
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (short)((categories[(i * 2) + 1] % 9) switch
			{
				0 => ushort.MaxValue - categories[i * 2], // UInt16
				1 => (categories[i * 2] & 0x7F) | 0x80, // UInt8
				2 => categories[i * 2] & 0x7F, // Fixed
				3 => sbyte.MinValue + (categories[i * 2] & 0x3F), // Int8
				_ => short.MinValue + categories[i * 2], // Int16
			});
		}

		return random;
	}

	private static sbyte[] GetRandomInt8Values2Category(int length)
	{
		sbyte[] random = new sbyte[length];
		byte[] categories = new byte[length * 2];
		new Random(123).NextBytes(categories);
		for (int i = 0; i < random.Length; i++)
		{
			random[i] = (sbyte)((categories[(i * 2) + 1] % 9) switch
			{
				0 => categories[i * 2] & 0x7F, // Fixed
				_ => sbyte.MinValue + (categories[i * 2] & 0x3F), // Int8
			});
		}

		return random;
	}

	[GenerateShape<sbyte[]>]
	[GenerateShape<short[]>]
	[GenerateShape<int[]>]
	[GenerateShape<long[]>]
	[GenerateShape<ushort[]>]
	[GenerateShape<uint[]>]
	[GenerateShape<ulong[]>]
	private partial class Witness;
}
