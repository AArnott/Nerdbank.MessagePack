// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public partial class HardwareAccelerated
{
	private static readonly MessagePackSerializer AcceleratedSerializer = new() { SerializeDefaultValues = true };
	private static readonly MessagePackSerializer UnacceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = true };
	private static readonly sbyte[] Int8Values = GetRandomValues<sbyte>(10_000);
	private static readonly short[] Int16Values = GetRandomValues<short>(10_000);
	private static readonly int[] Int32Values = GetRandomValues<int>(10_000);
	private static readonly long[] Int64Values = GetRandomValues<long>(10_000);
	private static readonly ushort[] UInt16Values = GetRandomValues<ushort>(10_000);
	private static readonly uint[] UInt32Values = GetRandomValues<uint>(10_000);
	private static readonly ulong[] UInt64Values = GetRandomValues<ulong>(10_000);
	private static readonly float[] SingleValues = GetRandomFloats(10_000);
	private static readonly double[] DoubleValues = GetRandomDoubles(10_000);
	private readonly Sequence buffer = new();

	[Benchmark]
	[BenchmarkCategory("int8", "serialize")]
	public void Int8_Serialize()
	{
		AcceleratedSerializer.Serialize<sbyte[], Witness>(this.buffer, Int8Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("int8", "serialize")]
	public void Int8_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<sbyte[], Witness>(this.buffer, Int8Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int16", "serialize")]
	public void Int16_Serialize()
	{
		AcceleratedSerializer.Serialize<short[], Witness>(this.buffer, Int16Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("int16", "serialize")]
	public void Int16_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<short[], Witness>(this.buffer, Int16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize()
	{
		AcceleratedSerializer.Serialize<int[], Witness>(this.buffer, Int32Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<int[], Witness>(this.buffer, Int32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("int64", "serialize")]
	public void Int64_Serialize()
	{
		AcceleratedSerializer.Serialize<long[], Witness>(this.buffer, Int64Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("int64", "serialize")]
	public void Int64_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<long[], Witness>(this.buffer, Int64Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint16", "serialize")]
	public void UInt16_Serialize()
	{
		AcceleratedSerializer.Serialize<ushort[], Witness>(this.buffer, UInt16Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("uint16", "serialize")]
	public void UInt16_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<ushort[], Witness>(this.buffer, UInt16Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint32", "serialize")]
	public void UInt32_Serialize()
	{
		AcceleratedSerializer.Serialize<uint[], Witness>(this.buffer, UInt32Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("uint32", "serialize")]
	public void UInt32_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<uint[], Witness>(this.buffer, UInt32Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("uint64", "serialize")]
	public void UInt64_Serialize()
	{
		AcceleratedSerializer.Serialize<ulong[], Witness>(this.buffer, UInt64Values);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("uint64", "serialize")]
	public void UInt64_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<ulong[], Witness>(this.buffer, UInt64Values);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("float", "serialize")]
	public void Single_Serialize()
	{
		AcceleratedSerializer.Serialize<float[], Witness>(this.buffer, SingleValues);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("float", "serialize")]
	public void Single_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<float[], Witness>(this.buffer, SingleValues);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("double", "serialize")]
	public void Double_Serialize()
	{
		AcceleratedSerializer.Serialize<double[], Witness>(this.buffer, DoubleValues);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("double", "serialize")]
	public void Double_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<double[], Witness>(this.buffer, DoubleValues);
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
