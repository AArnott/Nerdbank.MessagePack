// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

#if NET

/// <summary>
/// Measures the cost of the converter lookup that precedes every (de)serialization,
/// particularly when an application alternates between several types.
/// </summary>
[MemoryDiagnoser]
public class ConverterCacheLookup
{
	private const int OperationsPerIteration = 8;

	private readonly MessagePackSerializer serializer = new();
	private readonly ArrayBufferWriter<byte> buffer = new();

	private readonly CacheProbe1 value1 = new() { Value = 1 };
	private readonly CacheProbe2 value2 = new() { Value = 2 };
	private readonly CacheProbe3 value3 = new() { Value = 3 };
	private readonly CacheProbe4 value4 = new() { Value = 4 };
	private readonly CacheProbe5 value5 = new() { Value = 5 };
	private readonly CacheProbe6 value6 = new() { Value = 6 };
	private readonly CacheProbe7 value7 = new() { Value = 7 };
	private readonly CacheProbe8 value8 = new() { Value = 8 };

	private byte[] encoded1 = null!;
	private byte[] encoded2 = null!;
	private byte[] encoded3 = null!;
	private byte[] encoded4 = null!;

	[GlobalSetup]
	public void Setup()
	{
		this.encoded1 = this.serializer.Serialize(this.value1);
		this.encoded2 = this.serializer.Serialize(this.value2);
		this.encoded3 = this.serializer.Serialize(this.value3);
		this.encoded4 = this.serializer.Serialize(this.value4);
	}

	/// <summary>
	/// The best case for a single-entry converter cache: the same type over and over.
	/// </summary>
	/// <returns>The number of bytes written by the last operation.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Serialize")]
	public int Serialize_1Type()
	{
		int written = 0;
		for (int i = 0; i < OperationsPerIteration; i++)
		{
			written = this.Serialize(this.value1);
		}

		return written;
	}

	/// <summary>
	/// Alternating types defeat a single-entry converter cache on every operation.
	/// </summary>
	/// <returns>The number of bytes written by the last operation.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Serialize")]
	public int Serialize_2Types()
	{
		int written = 0;
		for (int i = 0; i < OperationsPerIteration / 2; i++)
		{
			written = this.Serialize(this.value1);
			written = this.Serialize(this.value2);
		}

		return written;
	}

	/// <inheritdoc cref="Serialize_2Types"/>
	/// <returns>The number of bytes written by the last operation.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Serialize")]
	public int Serialize_4Types()
	{
		int written = 0;
		for (int i = 0; i < OperationsPerIteration / 4; i++)
		{
			written = this.Serialize(this.value1);
			written = this.Serialize(this.value2);
			written = this.Serialize(this.value3);
			written = this.Serialize(this.value4);
		}

		return written;
	}

	/// <inheritdoc cref="Serialize_2Types"/>
	/// <returns>The number of bytes written by the last operation.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Serialize")]
	public int Serialize_8Types()
	{
		int written = this.Serialize(this.value1);
		written = this.Serialize(this.value2);
		written = this.Serialize(this.value3);
		written = this.Serialize(this.value4);
		written = this.Serialize(this.value5);
		written = this.Serialize(this.value6);
		written = this.Serialize(this.value7);
		written = this.Serialize(this.value8);
		return written;
	}

	/// <inheritdoc cref="Serialize_1Type"/>
	/// <returns>A checksum of the deserialized values.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Deserialize")]
	public int Deserialize_1Type()
	{
		int checksum = 0;
		for (int i = 0; i < OperationsPerIteration; i++)
		{
			checksum += this.serializer.Deserialize<CacheProbe1>(this.encoded1)!.Value;
		}

		return checksum;
	}

	/// <inheritdoc cref="Serialize_2Types"/>
	/// <returns>A checksum of the deserialized values.</returns>
	[Benchmark(OperationsPerInvoke = OperationsPerIteration)]
	[BenchmarkCategory("Deserialize")]
	public int Deserialize_4Types()
	{
		int checksum = 0;
		for (int i = 0; i < OperationsPerIteration / 4; i++)
		{
			checksum += this.serializer.Deserialize<CacheProbe1>(this.encoded1)!.Value;
			checksum += this.serializer.Deserialize<CacheProbe2>(this.encoded2)!.Value;
			checksum += this.serializer.Deserialize<CacheProbe3>(this.encoded3)!.Value;
			checksum += this.serializer.Deserialize<CacheProbe4>(this.encoded4)!.Value;
		}

		return checksum;
	}

	private int Serialize<T>(T value)
		where T : IShapeable<T>
	{
		this.serializer.Serialize(this.buffer, value);
		int written = this.buffer.WrittenCount;
		this.buffer.Clear();
		return written;
	}
}

/// <summary>A tiny type whose only purpose is to occupy a converter cache slot.</summary>
[GenerateShape]
public partial class CacheProbe1
{
	/// <summary>Gets or sets an arbitrary value.</summary>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe2
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe3
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe4
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe5
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe6
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe7
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

/// <inheritdoc cref="CacheProbe1"/>
[GenerateShape]
public partial class CacheProbe8
{
	/// <inheritdoc cref="CacheProbe1.Value"/>
	public int Value { get; set; }
}

#endif
