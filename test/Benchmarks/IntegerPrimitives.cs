// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using BenchmarkDotNet.Diagnosers;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3)]
[HardwareCounters(HardwareCounter.BranchInstructions, HardwareCounter.BranchMispredictions)]
public class IntegerPrimitives
{
	// This must exceed branch predictor history so the mixed distribution remains unpredictable.
	private const int Count = 100_000;

	private int[] values = null!;
	private byte[] encodedValues = null!;
	private byte[] writeBuffer = null!;

	[Params("Small", "Mixed", "Large")]
	public string Distribution { get; set; } = "Mixed";

	[GlobalSetup]
	public void Setup()
	{
		this.values = CreateValues(this.Distribution);
		this.writeBuffer = new byte[(Count * sizeof(int)) + Count];
		this.encodedValues = EncodeValues(this.values);
		ValidateEncodedValues(this.values, this.encodedValues);
	}

	[Benchmark(OperationsPerInvoke = Count)]
	[BenchmarkCategory("integer", "write")]
	public int WriteInt32()
	{
		int checksum = 0;
		int offset = 0;
		foreach (int value in this.values)
		{
			MessagePackPrimitives.TryWrite(this.writeBuffer.AsSpan(offset), value, out int tokenSize);
			offset += tokenSize;
			checksum += tokenSize;
		}

		return checksum;
	}

	[Benchmark(OperationsPerInvoke = Count)]
	[BenchmarkCategory("integer", "read")]
	public int ReadInt32()
	{
		int checksum = 0;
		int offset = 0;
		for (int i = 0; i < this.values.Length; i++)
		{
			MessagePackPrimitives.TryRead(this.encodedValues.AsSpan(offset), out int decodedValue, out int tokenSize);
			offset += tokenSize;
			checksum += decodedValue;
		}

		return checksum;
	}

	private static int[] CreateValues(string distribution)
	{
		Random random = new(42);
		int[] result = new int[Count];
		for (int i = 0; i < result.Length; i++)
		{
			result[i] = distribution switch
			{
				"Small" => random.Next(MessagePackRange.MinFixNegativeInt, MessagePackCode.MaxFixInt + 1),
				"Large" => random.Next(2) == 0 ? random.Next(ushort.MaxValue + 1, int.MaxValue) : random.Next(int.MinValue, short.MinValue),
				"Mixed" => CreateMixedValue(random),
				_ => throw new ArgumentOutOfRangeException(nameof(distribution), distribution, "Unsupported integer distribution."),
			};
		}

		return result;
	}

	private static int CreateMixedValue(Random random)
	{
		return random.Next(8) switch
		{
			0 => random.Next(0, MessagePackCode.MaxFixInt + 1),
			1 => random.Next(MessagePackRange.MinFixNegativeInt, 0),
			2 => random.Next(MessagePackCode.MaxFixInt + 1, byte.MaxValue + 1),
			3 => random.Next(sbyte.MinValue, MessagePackRange.MinFixNegativeInt),
			4 => random.Next(byte.MaxValue + 1, ushort.MaxValue + 1),
			5 => random.Next(short.MinValue, sbyte.MinValue),
			6 => random.Next(ushort.MaxValue + 1, int.MaxValue),
			_ => random.Next(int.MinValue, short.MinValue),
		};
	}

	private static byte[] EncodeValues(int[] source)
	{
		byte[] destination = new byte[source.Length * (sizeof(int) + 1)];
		int offset = 0;
		foreach (int value in source)
		{
			if (!MessagePackPrimitives.TryWrite(destination.AsSpan(offset), value, out int tokenSize))
			{
				throw new InvalidOperationException("The benchmark buffer must be large enough for every integer.");
			}

			offset += tokenSize;
		}

		return destination.AsSpan(0, offset).ToArray();
	}

	private static void ValidateEncodedValues(int[] expectedValues, byte[] encodedValues)
	{
		int offset = 0;
		foreach (int expectedValue in expectedValues)
		{
			if (MessagePackPrimitives.TryRead(encodedValues.AsSpan(offset), out int actualValue, out int tokenSize) != MessagePackPrimitives.DecodeResult.Success || actualValue != expectedValue)
			{
				throw new InvalidOperationException("The benchmark data did not round-trip through the primitive integer codec.");
			}

			offset += tokenSize;
		}

		if (offset != encodedValues.Length)
		{
			throw new InvalidOperationException("The benchmark data contains trailing bytes.");
		}
	}
}

#endif
