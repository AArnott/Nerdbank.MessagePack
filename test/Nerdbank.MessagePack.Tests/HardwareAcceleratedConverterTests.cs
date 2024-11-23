// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Reflection;

public partial class HardwareAcceleratedConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Theory, PairwiseData]
	public void BoolArray([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(byte))] int length)
	{
		byte[] random = new byte[length];
		Random.Shared.NextBytes(random);
		bool[] values = random.Select(b => b % 2 == 0).ToArray();
		this.Roundtrip<bool[], Witness>(values);
	}

	[Theory, PairwiseData]
	public void Boolean([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(byte))] int length)
	{
		byte[] random = new byte[length];
		Random.Shared.NextBytes(random);
		bool[] values = random.Select(b => b % 2 == 0).ToArray();
		this.Roundtrip<Memory<bool>, Witness>(values);
	}

	[Theory, PairwiseData]
	public void Int8([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(sbyte))] int length)
		=> this.Roundtrip<Memory<sbyte>, Witness>(GetRandomValues<sbyte>(length));

	[Theory, PairwiseData]
	public void Int16([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(short))] int length)
		=> this.Roundtrip<Memory<short>, Witness>(GetRandomValues<short>(length));

	[Theory, PairwiseData]
	public void Int32([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(int))] int length)
		=> this.Roundtrip<Memory<int>, Witness>(GetRandomValues<int>(length));

	[Theory, PairwiseData]
	public void Int64([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(long))] int length)
		=> this.Roundtrip<Memory<long>, Witness>(GetRandomValues<long>(length));

	[Theory, PairwiseData]
	public void UInt8([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(byte))] int length)
		=> this.Roundtrip<Memory<byte>, Witness>(GetRandomValues<byte>(length));

	[Theory, PairwiseData]
	public void UInt16([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(ushort))] int length)
		=> this.Roundtrip<Memory<ushort>, Witness>(GetRandomValues<ushort>(length));

	[Theory, PairwiseData]
	public void UInt32([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(uint))] int length)
		=> this.Roundtrip<Memory<uint>, Witness>(GetRandomValues<uint>(length));

	[Theory, PairwiseData]
	public void UInt64([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(ulong))] int length)
		=> this.Roundtrip<Memory<ulong>, Witness>(GetRandomValues<ulong>(length));

	[Theory, PairwiseData]
	public void Single([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(float))] int length)
	{
		float[] values = new float[length];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = Random.Shared.NextSingle();
		}

		this.Roundtrip<Memory<float>, Witness>(values);
	}

	[Theory, PairwiseData]
	public void Double([CombinatorialMemberData(nameof(GetInterestingLengths), typeof(double))] int length)
	{
		double[] values = new double[length];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = Random.Shared.NextDouble();
		}

		this.Roundtrip<Memory<double>, Witness>(values);
	}

	private static unsafe T[] GetRandomValues<T>(int length)
		where T : unmanaged
	{
		byte[] random = new byte[length * sizeof(T)];
		Random.Shared.NextBytes(random);
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

	private static int[] GetInterestingLengths(Type type) => (int[])typeof(HardwareAcceleratedConverterTests).GetMethod(nameof(GetInterestingLengthsHelper), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type).Invoke(null, null)!;

	private static int[] GetInterestingLengthsHelper<T>() => [0, 4, Vector<T>.Count - 1, Vector<T>.Count, Vector<T>.Count + 1, (Vector<T>.Count * 2) + 2, 10_000];

	[GenerateShape<bool[]>]
	[GenerateShape<Memory<bool>>]
	[GenerateShape<Memory<sbyte>>]
	[GenerateShape<Memory<short>>]
	[GenerateShape<Memory<int>>]
	[GenerateShape<Memory<long>>]
	[GenerateShape<Memory<byte>>]
	[GenerateShape<Memory<ushort>>]
	[GenerateShape<Memory<uint>>]
	[GenerateShape<Memory<ulong>>]
	[GenerateShape<Memory<float>>]
	[GenerateShape<Memory<double>>]
	private partial class Witness;
}
