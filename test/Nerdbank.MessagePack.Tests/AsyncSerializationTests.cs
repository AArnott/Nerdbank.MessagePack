// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Trait("AsyncSerialization", "true")]
public partial class AsyncSerializationTests : MessagePackSerializerTestBase
{
	private static readonly ReadOnlyMemory<byte> BigDataBlob = Enumerable.Range(0, 100).Select(n => (byte)(n % 256)).ToArray();

	[Fact]
	public async Task RoundtripPoco() => await this.AssertRoundtripAsync(new Poco(1, 2));

	[Fact]
	public async Task RoundtripPocoWithDefaultCtor() => await this.AssertRoundtripAsync(new PocoWithDefaultCtor { X = 1, Y = 2 });

	[Fact]
	public async Task PocoDictionary()
	{
		DictionaryOfPocos value = new(new Dictionary<string, Poco>
		{
			["one"] = new Poco(1, 1),
			["two"] = new Poco(2, 2),
		}.ToImmutableDictionary());

		this.Serializer = this.Serializer with { Converters = [new AsyncDictionaryOfPocosConverter()] };
		await this.AssertRoundtripAsync(value);
	}

	[Fact]
	public async Task PrimitivesDictionary()
	{
		DictionaryOfPrimitives value = new(new Dictionary<string, int>
		{
			["one"] = 1,
			["two"] = 2,
		}.ToImmutableDictionary());

		this.Serializer = this.Serializer with { Converters = [new AsyncDictionaryOfPrimitivesConverter()] };
		await this.AssertRoundtripAsync(value);
	}

	[Fact]
	public async Task LargeArray() => await this.AssertRoundtripAsync(new ArrayOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i)).ToArray()));

	/// <summary>
	/// Verifies that the array converter can handle async serialization when its elements are not async capable.
	/// </summary>
	[Fact]
	public async Task LargeArrayWithBigElements()
	{
		this.Serializer = this.Serializer with { Converters = [.. this.Serializer.Converters, new PocoNonAsyncConverter()] };

		await this.AssertRoundtripAsync(new ArrayOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i) { DataBlob = BigDataBlob }).ToArray()));
	}

	[Fact]
	public async Task LargeList() => await this.AssertRoundtripAsync(new ListOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i)).ToList()));

	[Fact]
	public async Task LargeImmutableArray() => await this.AssertRoundtripAsync(new ImmutableArrayOfPocos(Enumerable.Range(0, 1000).Select(i => new Poco(i, i)).ToImmutableArray()));

	[Fact]
	public async Task Null_Array() => await this.AssertRoundtripAsync(new ArrayOfPocos(null));

	[Fact]
	public async Task Null() => await this.AssertRoundtripAsync<Poco>(null);

	[Fact]
	public async Task ArrayOfInts() => await this.AssertRoundtripAsync(new ArrayOfPrimitives([1, 2, 3]));

	[Fact]
	public async Task ObjectAsArrayOfValues() => await this.AssertRoundtripAsync(new PocoAsArray(42));

	[Fact]
	public async Task ObjectAsArrayOfValues_Null() => await this.AssertRoundtripAsync<PocoAsArray>(null);

	[Fact]
	public async Task ObjectAsArrayOfValues_DefaultCtor() => await this.AssertRoundtripAsync(new PocoAsArrayWithDefaultCtor { Value = 42 });

	[Fact]
	public async Task ObjectAsArrayOfValues_DefaultCtor_Null() => await this.AssertRoundtripAsync<PocoAsArrayWithDefaultCtor>(null);

	[Fact]
	public async Task WithPreBuffering()
	{
		SpecialRecordConverter converter = new();
		this.Serializer = this.Serializer with { Converters = [converter] };
		var msgpack = new ReadOnlySequence<byte>(
			this.Serializer.Serialize(new SpecialRecord { Property = 446 }, TestContext.Current.CancellationToken));

		// Verify that with a sufficiently low async buffer, the async paths are taken.
		this.Serializer = new()
		{
			MaxAsyncBuffer = 1,
			Converters = [converter],
		};
		await this.Serializer.DeserializeAsync<SpecialRecord>(new FragmentedPipeReader(msgpack), TestContext.Current.CancellationToken);
		Assert.Equal(1, converter.AsyncDeserializationCounter);

		// Verify that with a sufficiently high async buffer, the sync paths are taken.
		converter.AsyncDeserializationCounter = 0;
		this.Serializer = new()
		{
			MaxAsyncBuffer = 15,
			Converters = [converter],
		};
		await this.Serializer.DeserializeAsync<SpecialRecord>(new FragmentedPipeReader(msgpack), TestContext.Current.CancellationToken);
		Assert.Equal(0, converter.AsyncDeserializationCounter);
	}

	[Fact]
	public async Task DecodeLargeString()
	{
		string expected = new string('a', 100 * 1024);
		ReadOnlySequence<byte> msgpack = new(this.Serializer.Serialize<string, Witness>(expected, TestContext.Current.CancellationToken));
		FragmentedPipeReader pipeReader = new(msgpack, msgpack.GetPosition(0), msgpack.GetPosition(1), msgpack.GetPosition(512), msgpack.GetPosition(6000), msgpack.GetPosition(32 * 1024));
		string? actual = await this.Serializer.DeserializeAsync<string>(pipeReader, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal(expected, actual);
	}

	[Fact]
	public async Task DecodeEmptyString()
	{
		string expected = string.Empty;
		ReadOnlySequence<byte> msgpack = new(this.Serializer.Serialize<string, Witness>(expected, TestContext.Current.CancellationToken));
		FragmentedPipeReader pipeReader = new(msgpack, msgpack.GetPosition(0));
		string? actual = await this.Serializer.DeserializeAsync<string>(pipeReader, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal(expected, actual);
	}

	[Theory, PairwiseData]
	public async Task DeserializeAsyncAdvancesPipeReader(bool forceAsync)
	{
		this.Serializer = this.Serializer with { MaxAsyncBuffer = forceAsync ? 0 : 1024 };
		using Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.Write(42);
		writer.Flush();
		sequence.Write("a"u8);

		PipeReader reader = PipeReader.Create(sequence);

		// Deserialize a value. It should advance the reader exactly across the msgpack structure.
		int number = await this.Serializer.DeserializeAsync<int>(reader, Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
		Assert.Equal(42, number);

		// Verify that the reader is now positioned at the next byte.
		ReadResult readResult = await reader.ReadAsync(TestContext.Current.CancellationToken);
		Assert.True(readResult.IsCompleted);
		Assert.Equal("a"u8, readResult.Buffer.ToArray());
	}

	[GenerateShapeFor<string>]
	[GenerateShapeFor<int>]
	private partial class Witness;

	[GenerateShape]
	public partial record Poco(int X, int Y)
	{
		public ReadOnlyMemory<byte> DataBlob { get; init; }

		public virtual bool Equals(Poco? other)
		{
			return other is not null
				&& this.X == other.X
				&& this.Y == other.Y
				&& this.DataBlob.Span.SequenceEqual(other.DataBlob.Span);
		}

		public override int GetHashCode() => HashCode.Combine(this.X, this.Y);
	}

	[GenerateShape]
	public partial record PocoWithDefaultCtor
	{
		public int X { get; set; }

		public int Y { get; set; }
	}

	[GenerateShape]
	public partial class DictionaryOfPrimitives(ImmutableDictionary<string, int>? pocos) : IEquatable<DictionaryOfPrimitives>
	{
		public ImmutableDictionary<string, int>? Pocos => pocos;

		public bool Equals(DictionaryOfPrimitives? other) => other is not null && StructuralEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class DictionaryOfPocos(ImmutableDictionary<string, Poco>? pocos) : IEquatable<DictionaryOfPocos>
	{
		public ImmutableDictionary<string, Poco>? Pocos => pocos;

		public bool Equals(DictionaryOfPocos? other) => other is not null && StructuralEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class ArrayOfPocos(Poco[]? pocos) : IEquatable<ArrayOfPocos>
	{
		public Poco[]? Pocos => pocos;

		public bool Equals(ArrayOfPocos? other) => other is not null && StructuralEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class ListOfPocos(List<Poco>? pocos) : IEquatable<ListOfPocos>
	{
		public List<Poco>? Pocos => pocos;

		public bool Equals(ListOfPocos? other) => other is not null && StructuralEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class ImmutableArrayOfPocos(ImmutableArray<Poco>? pocos) : IEquatable<ImmutableArrayOfPocos>
	{
		public ImmutableArray<Poco>? Pocos => pocos;

		public bool Equals(ImmutableArrayOfPocos? other) => other is not null && StructuralEquality.Equal(this.Pocos, other.Pocos);
	}

	[GenerateShape]
	public partial class ArrayOfPrimitives(int[]? values) : IEquatable<ArrayOfPrimitives>
	{
		public int[]? Values => values;

		public bool Equals(ArrayOfPrimitives? other) => other is not null && StructuralEquality.Equal(this.Values, other.Values);
	}

	[GenerateShape]
	public partial record PocoAsArray([property: Key(0)] int Value);

	[GenerateShape]
	public partial record PocoAsArrayWithDefaultCtor
	{
		[Key(0)]
		public int Value { get; set; }
	}

	[GenerateShape]
	internal partial record SpecialRecord
	{
		internal int Property { get; set; }
	}

	internal class SpecialRecordConverter : MessagePackConverter<SpecialRecord>
	{
		public override bool PreferAsyncSerialization => true;

		internal int AsyncDeserializationCounter { get; set; }

		public override SpecialRecord? Read(ref MessagePackReader reader, SerializationContext context)
		{
			return new SpecialRecord { Property = reader.ReadInt32() };
		}

		public override void Write(ref MessagePackWriter writer, in SpecialRecord? value, SerializationContext context)
		{
			writer.Write(value!.Property);
		}

		public override ValueTask<SpecialRecord?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
		{
			this.AsyncDeserializationCounter++;
			return base.ReadAsync(reader, context);
		}
	}

	private class PocoNonAsyncConverter : MessagePackConverter<Poco>
	{
		public override Poco? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int len = reader.ReadArrayHeader();
			Assert.Equal(3, len);
			int x = reader.ReadInt32();
			int y = reader.ReadInt32();
			ReadOnlyMemory<byte> blob = reader.ReadBytes()?.ToArray() ?? default;
			return new Poco(x, y) { DataBlob = blob };
		}

		public override void Write(ref MessagePackWriter writer, in Poco? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(3);
			writer.Write(value.X);
			writer.Write(value.Y);
			writer.Write(value.DataBlob.Span);
		}
	}

	private class AsyncDictionaryOfPrimitivesConverter : MessagePackConverter<DictionaryOfPrimitives>
	{
		public override bool PreferAsyncSerialization => true;

		public override DictionaryOfPrimitives? Read(ref MessagePackReader reader, SerializationContext context) => throw new NotImplementedException();

		public override void Write(ref MessagePackWriter writer, in DictionaryOfPrimitives? value, SerializationContext context) => throw new NotImplementedException();

		public override async ValueTask<DictionaryOfPrimitives?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
			=> new DictionaryOfPrimitives(await context.GetConverter<ImmutableDictionary<string, int>>(context.TypeShapeProvider).ReadAsync(reader, context));

		public override ValueTask WriteAsync(MessagePackAsyncWriter writer, DictionaryOfPrimitives? value, SerializationContext context)
			=> context.GetConverter<ImmutableDictionary<string, int>>(context.TypeShapeProvider).WriteAsync(writer, value?.Pocos, context);
	}

	private class AsyncDictionaryOfPocosConverter : MessagePackConverter<DictionaryOfPocos>
	{
		public override bool PreferAsyncSerialization => true;

		public override DictionaryOfPocos? Read(ref MessagePackReader reader, SerializationContext context) => throw new NotImplementedException();

		public override void Write(ref MessagePackWriter writer, in DictionaryOfPocos? value, SerializationContext context) => throw new NotImplementedException();

		public override async ValueTask<DictionaryOfPocos?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
			=> new DictionaryOfPocos(await context.GetConverter<ImmutableDictionary<string, Poco>>(context.TypeShapeProvider).ReadAsync(reader, context));

		public override ValueTask WriteAsync(MessagePackAsyncWriter writer, DictionaryOfPocos? value, SerializationContext context)
			=> context.GetConverter<ImmutableDictionary<string, Poco>>(context.TypeShapeProvider).WriteAsync(writer, value?.Pocos, context);
	}
}
