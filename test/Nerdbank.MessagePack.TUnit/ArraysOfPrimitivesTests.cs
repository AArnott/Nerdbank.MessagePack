// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

public partial class ArraysOfPrimitivesTests : MessagePackSerializerTestBase
{
#if NET
	private static readonly Random Random = Random.Shared;
#else
	private static readonly Random Random = new Random();
#endif

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<byte>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void BoolArray(int length)
	{
		bool[]? values = null;
		if (length >= 0)
		{
			byte[] random = new byte[length];
			Random.NextBytes(random);
			values = random.Select(b => b % 2 == 0).ToArray();
		}

		this.Roundtrip<bool[], Witness>(values);
	}

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<byte>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Boolean(int length)
	{
		bool[]? values = null;
		if (length >= 0)
		{
			byte[]? random = new byte[length];
			Random.NextBytes(random);
			values = random.Select(b => b % 2 == 0).ToArray();
		}

		this.Roundtrip<Memory<bool>, Witness>(values);
	}

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<sbyte>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Int8(int length)
		=> this.Roundtrip<Memory<sbyte>, Witness>(GetRandomValues<sbyte>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<short>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Int16(int length)
		=> this.Roundtrip<Memory<short>, Witness>(GetRandomValues<short>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<int>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Int32(int length)
		=> this.Roundtrip<Memory<int>, Witness>(GetRandomValues<int>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<long>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Int64(int length)
		=> this.Roundtrip<Memory<long>, Witness>(GetRandomValues<long>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<byte>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void UInt8(int length)
		=> this.Roundtrip<Memory<byte>, Witness>(GetRandomValues<byte>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<ushort>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void UInt16(int length)
		=> this.Roundtrip<Memory<ushort>, Witness>(GetRandomValues<ushort>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<uint>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void UInt32(int length)
		=> this.Roundtrip<Memory<uint>, Witness>(GetRandomValues<uint>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<ulong>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void UInt64(int length)
		=> this.Roundtrip<Memory<ulong>, Witness>(GetRandomValues<ulong>(length));

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<float>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Single(int length)
	{
		float[]? values = null;
		if (length >= 0)
		{
			values = new float[length];
			for (int i = 0; i < values.Length; i++)
			{
#if NET
				values[i] = Random.NextSingle();
#else
				values[i] = (float)Random.NextDouble();
#endif
			}
		}

		this.Roundtrip<Memory<float>, Witness>(values);
	}

	[Test, MethodDataSource(typeof(GetInterestingLengthsHelper<double>), nameof(GetInterestingLengthsHelper<>.Helper))]
	public void Double(int length)
	{
		double[]? values = null;
		if (length >= 0)
		{
			values = new double[length];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = Random.NextDouble();
			}
		}

		this.Roundtrip<Memory<double>, Witness>(values);
	}

	[Test]
	public void LargeEnumerableOfInt()
	{
		// A very large enumerable so that it exceeds any default buffer sizes.
		IEnumerable<int> model = Enumerable.Range(0, 1600);
		this.Roundtrip<IEnumerable<int>, Witness>(model);
	}

	private static unsafe T[]? GetRandomValues<T>(int length)
		where T : unmanaged
	{
		if (length < 0)
		{
			return null;
		}

		byte[] random = new byte[length * sizeof(T)];
		Random.NextBytes(random);
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

	public static class GetInterestingLengthsHelper<T>
	{
#if NET
		public static int[] Helper() => [-1, 0, 4, Vector<T>.Count - 1, Vector<T>.Count, Vector<T>.Count + 1, (Vector<T>.Count * 2) + 2, 10_000];
#else
		public static int[] Helper() => [-1, 0, 4, 100, 10_000];
#endif
	}

	[GenerateShapeFor<bool[]>]
	[GenerateShapeFor<Memory<bool>>]
	[GenerateShapeFor<Memory<sbyte>>]
	[GenerateShapeFor<Memory<short>>]
	[GenerateShapeFor<Memory<int>>]
	[GenerateShapeFor<Memory<long>>]
	[GenerateShapeFor<Memory<byte>>]
	[GenerateShapeFor<Memory<ushort>>]
	[GenerateShapeFor<Memory<uint>>]
	[GenerateShapeFor<Memory<ulong>>]
	[GenerateShapeFor<Memory<float>>]
	[GenerateShapeFor<Memory<double>>]
	[GenerateShapeFor<IEnumerable<int>>]
	private partial class Witness;
}
