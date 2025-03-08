// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public partial class ArraysOfPrimitives
{
	private const int Length = 10_000;
	private static readonly MessagePackSerializer AcceleratedSerializer = new() { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always, DisableHardwareAcceleration = false };
	private static readonly MessagePackSerializer UnacceleratedSerializer = new() { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always, DisableHardwareAcceleration = true };
	private static readonly MessagePackSerializer TestSerializer = new() { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always };
	private static readonly bool[] BoolValues = GetRandomBools(Length);
	private static readonly byte[] BoolValuesMsgPack = TestSerializer.Serialize<bool[], Witness>(BoolValues);
	private static readonly float[] SingleValues = GetRandomFloats(Length);
	private static readonly byte[] SingleValuesMsgPack = TestSerializer.Serialize<float[], Witness>(SingleValues);
	private static readonly double[] DoubleValues = GetRandomDoubles(Length);
	private static readonly byte[] DoubleValuesMsgPack = TestSerializer.Serialize<double[], Witness>(DoubleValues);
	private readonly Sequence buffer = new();

	[ParamsAllValues]
	public bool Accelerated { get; set; }

	public MessagePackSerializer Serializer => this.Accelerated ? AcceleratedSerializer : UnacceleratedSerializer;

	[Benchmark]
	[BenchmarkCategory("bool", "deserialize")]
	public bool[]? Bool_Deserialize()
	{
		return this.Serializer.Deserialize<bool[], Witness>(BoolValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("float", "deserialize")]
	public float[]? Single_Deserialize()
	{
		return this.Serializer.Deserialize<float[], Witness>(SingleValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("double", "deserialize")]
	public double[]? Double_Deserialize()
	{
		return this.Serializer.Deserialize<double[], Witness>(DoubleValuesMsgPack);
	}

	[Benchmark]
	[BenchmarkCategory("bool", "serialize")]
	public void Bool_Serialize()
	{
		this.Serializer.Serialize<bool[], Witness>(this.buffer, BoolValues);
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
	[GenerateShape<float[]>]
	[GenerateShape<double[]>]
	private partial class Witness;
}
