// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public partial class HardwareAccelerated
{
	private static readonly MessagePackSerializer AcceleratedSerializer = new() { SerializeDefaultValues = true };
	private static readonly MessagePackSerializer UnacceleratedSerializer = new() { SerializeDefaultValues = true, DisableHardwareAcceleration = true };
	private static readonly int[] IntValues = GetRandomValues<int>(10_000);
	private readonly Sequence buffer = new();

	[Benchmark]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize()
	{
		AcceleratedSerializer.Serialize<int[], Witness>(this.buffer, IntValues);
		this.buffer.Reset();
	}

	[Benchmark(Baseline = true)]
	[BenchmarkCategory("int32", "serialize")]
	public void Int32_Serialize_Unoptimized()
	{
		UnacceleratedSerializer.Serialize<int[], Witness>(this.buffer, IntValues);
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

	[GenerateShape<int[]>]
	private partial class Witness;
}
