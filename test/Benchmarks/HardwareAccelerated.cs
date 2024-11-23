// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public partial class HardwareAccelerated
{
	private const int Length = 10_000;
	private static readonly MessagePackSerializer AcceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = false };
	private static readonly MessagePackSerializer UnacceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = true };
	private static readonly bool[] BoolValues = GetRandomBools(Length);
	private static readonly byte[] BoolValuesMsgPack = UnacceleratedSerializer.Serialize<bool[], Witness>(BoolValues);
	private static readonly sbyte[] Int8Values = GetRandomValues<sbyte>(Length);
	private static readonly short[] Int16Values = GetRandomValues<short>(Length);
	private static readonly int[] Int32Values = GetRandomValues<int>(Length);
	private static readonly long[] Int64Values = GetRandomValues<long>(Length);
	private static readonly ushort[] UInt16Values = GetRandomValues<ushort>(Length);
	private static readonly uint[] UInt32Values = GetRandomValues<uint>(Length);
	private static readonly ulong[] UInt64Values = GetRandomValues<ulong>(Length);
	private static readonly float[] SingleValues = GetRandomFloats(Length);
	private static readonly double[] DoubleValues = GetRandomDoubles(Length);
	private readonly Sequence buffer = new();

	[ParamsAllValues]
	public bool Accelerated { get; set; }

	public MessagePackSerializer Serializer => this.Accelerated ? AcceleratedSerializer : UnacceleratedSerializer;

	[Benchmark]
	[BenchmarkCategory("bool", "deserialize")]
	public void Bool_Deserialize()
	{
		this.Serializer.Deserialize<bool[], Witness>(BoolValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("bool", "serialize")]
	public void Bool_Serialize()
	{
		this.Serializer.Serialize<bool[], Witness>(this.buffer, BoolValues);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int8", "serialize")]
	public void Int8_Serialize()
	{
		this.Serializer.Serialize<sbyte[], Witness>(this.buffer, Int8Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int16", "serialize")]
	public void Int16_Serialize()
	{
		this.Serializer.Serialize<short[], Witness>(this.buffer, Int16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize()
	{
		this.Serializer.Serialize<int[], Witness>(this.buffer, Int32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int64", "serialize")]
	public void Int64_Serialize()
	{
		this.Serializer.Serialize<long[], Witness>(this.buffer, Int64Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint16", "serialize")]
	public void UInt16_Serialize()
	{
		this.Serializer.Serialize<ushort[], Witness>(this.buffer, UInt16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint32", "serialize")]
	public void UInt32_Serialize()
	{
		this.Serializer.Serialize<uint[], Witness>(this.buffer, UInt32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint64", "serialize")]
	public void UInt64_Serialize()
	{
		this.Serializer.Serialize<ulong[], Witness>(this.buffer, UInt64Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("float", "serialize")]
	public void Single_Serialize()
	{
		this.Serializer.Serialize<float[], Witness>(this.buffer, SingleValues);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("double", "serialize")]
	public void Double_Serialize()
	{
		this.Serializer.Serialize<double[], Witness>(this.buffer, DoubleValues);
		this.buffer.Reset();
	}

	private static unsafe T[] GetRandomValues<T>(int length)
		where T : unmanaged
	{
		byte[] random = new byte[length * sizeof(T)];
		new Random(123).NextBytes(random); // use a fixed seed for reproducibility
		T[] values = new T[length];
		fixed (byte* pSource = random)
		{
			fixed (T* pTarget = values)
			{
				Buffer.MemoryCopy(pSource, pTarget, values.Length * sizeof(T), random.Length);
			}
		}

		return values;
	}

	private static float[] GetRandomFloats(int length)
	{
		Random random = new Random(123); // use a fixed seed for reproducibility
		float[] values = new float[length];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = random.NextSingle();
		}

		return values;
	}

	private static double[] GetRandomDoubles(int length)
	{
		Random random = new Random(123); // use a fixed seed for reproducibility
		double[] values = new double[length];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = random.NextDouble();
		}

		return values;
	}

	private static bool[] GetRandomBools(int length)
	{
		byte[] random = new byte[length];
		new Random(123).NextBytes(random); // use a fixed seed for reproducibility
		bool[] values = random.Select(b => b % 2 == 0).ToArray();
		return values;
	}

	[GenerateShape<bool[]>]
	[GenerateShape<sbyte[]>]
	[GenerateShape<sbyte[]>]
	[GenerateShape<short[]>]
	[GenerateShape<int[]>]
	[GenerateShape<long[]>]
	[GenerateShape<ushort[]>]
	[GenerateShape<uint[]>]
	[GenerateShape<ulong[]>]
	[GenerateShape<float[]>]
	[GenerateShape<double[]>]
	private partial class Witness;
}
