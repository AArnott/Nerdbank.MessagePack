// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class VersionSafeSerializationTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task MapObject(bool async)
	{
		MapModelV2 originalV2 = new()
		{
			Name = "Test",
			Age = 42,
		};

		// Serialize V2 and deserialize as V1 (with fewer properties)
		byte[] serializedV2 = await this.SerializeMaybeAsync(originalV2, async);
		this.LogMsgPack(serializedV2);
		MapModelV1? deserializedV1 = await this.DeserializeMaybeAsync<MapModelV1>(serializedV2, async);
		Assert.NotNull(deserializedV1);

		// Verify the basic properties were preserved
		Assert.Equal(originalV2.Name, deserializedV1.Name);

		// Make a simple change.
		deserializedV1.Name += "!";

		// Re-serialize V1 (which contains unused data) and deserialize back to V2
		byte[] serializedV1 = await this.SerializeMaybeAsync(deserializedV1, async);
		this.LogMsgPack(serializedV1);
		MapModelV2? restoredV2 = await this.DeserializeMaybeAsync<MapModelV2>(serializedV1, async);
		Assert.NotNull(restoredV2);

		// Verify all data was preserved including the extra property
		Assert.Equal(originalV2.Name + "!", restoredV2.Name);
		Assert.Equal(originalV2.Age, restoredV2.Age);
	}

	[Test, MatrixDataSource]
	public async Task MapObject_NonDefaultCtor(bool async)
	{
		MapModelV2NonDefaultCtor originalV2 = new()
		{
			Name = "Test",
			Age = 42,
		};

		// Serialize V2 and deserialize as V1 (with fewer properties)
		byte[] serializedV2 = await this.SerializeMaybeAsync(originalV2, async);
		this.LogMsgPack(serializedV2);
		MapModelV1NonDefaultCtor? deserializedV1 = await this.DeserializeMaybeAsync<MapModelV1NonDefaultCtor>(serializedV2, async);
		Assert.NotNull(deserializedV1);

		// Verify the basic properties were preserved
		Assert.Equal(originalV2.Name, deserializedV1.Name);

		// Make a simple change.
		deserializedV1.Name += "!";

		// Re-serialize V1 (which contains unused data) and deserialize back to V2
		byte[] serializedV1 = await this.SerializeMaybeAsync(deserializedV1, async);
		this.LogMsgPack(serializedV1);
		MapModelV2NonDefaultCtor? restoredV2 = await this.DeserializeMaybeAsync<MapModelV2NonDefaultCtor>(serializedV1, async);
		Assert.NotNull(restoredV2);

		// Verify all data was preserved including the extra property
		Assert.Equal(originalV2.Name + "!", restoredV2.Name);
		Assert.Equal(originalV2.Age, restoredV2.Age);
	}

	[Test, MatrixDataSource]
	public async Task ArrayObject(bool async, bool forceMap)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Required };

		ArrayModelV2 originalV2 = new()
		{
			Name = "Test",
			Age = 42,
			ForceMap = forceMap,
		};

		// Serialize V2 and deserialize as V1 (with fewer properties)
		byte[] serializedV2 = await this.SerializeMaybeAsync(originalV2, async);
		this.LogMsgPack(serializedV2);
		Assert.Equal(forceMap, IsMap(serializedV2));
		ArrayModelV1? deserializedV1 = await this.DeserializeMaybeAsync<ArrayModelV1>(serializedV2, async);
		Assert.NotNull(deserializedV1);

		// Verify the basic properties were preserved
		Assert.Equal(originalV2.Name, deserializedV1.Name);

		// Make a simple change.
		deserializedV1.Name += "!";

		// Re-serialize V1 (which contains unused data) and deserialize back to V2
		byte[] serializedV1 = await this.SerializeMaybeAsync(deserializedV1, async);
		this.LogMsgPack(serializedV1);
		Assert.Equal(forceMap, IsMap(serializedV1));
		ArrayModelV2? restoredV2 = await this.DeserializeMaybeAsync<ArrayModelV2>(serializedV1, async);
		Assert.NotNull(restoredV2);

		// Verify all data was preserved including the extra property
		Assert.Equal(originalV2.Name + "!", restoredV2.Name);
		Assert.Equal(originalV2.Age, restoredV2.Age);
		Assert.Equal(originalV2.ForceMap, restoredV2.ForceMap);
	}

	[Test, MatrixDataSource]
	public async Task ArrayObject_NonDefaultCtor(bool async, bool forceMap)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Required };

		ArrayModelV2NonDefaultCtor originalV2 = new()
		{
			Name = "Test",
			Age = 42,
			ForceMap = forceMap,
		};

		// Serialize V2 and deserialize as V1 (with fewer properties)
		byte[] serializedV2 = await this.SerializeMaybeAsync(originalV2, async);
		this.LogMsgPack(serializedV2);
		Assert.Equal(forceMap, IsMap(serializedV2));
		ArrayModelV1NonDefaultCtor? deserializedV1 = await this.DeserializeMaybeAsync<ArrayModelV1NonDefaultCtor>(serializedV2, async);
		Assert.NotNull(deserializedV1);

		// Verify the basic properties were preserved
		Assert.Equal(originalV2.Name, deserializedV1.Name);

		// Make a simple change.
		deserializedV1.Name += "!";

		// Re-serialize V1 (which contains unused data) and deserialize back to V2
		byte[] serializedV1 = await this.SerializeMaybeAsync(deserializedV1, async);
		this.LogMsgPack(serializedV1);
		Assert.Equal(forceMap, IsMap(serializedV1));
		ArrayModelV2NonDefaultCtor? restoredV2 = await this.DeserializeMaybeAsync<ArrayModelV2NonDefaultCtor>(serializedV1, async);
		Assert.NotNull(restoredV2);

		// Verify all data was preserved including the extra property
		Assert.Equal(originalV2.Name + "!", restoredV2.Name);
		Assert.Equal(originalV2.Age, restoredV2.Age);
		Assert.Equal(originalV2.ForceMap, restoredV2.ForceMap);
	}

	[Test]
	public void StructWithVersionSafety()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(2);
		writer.Write(nameof(VersionSafeStruct.Name));
		writer.Write("Andrew");
		writer.Write("Age");
		writer.Write(18);
		writer.Flush();

		RawMessagePack result = this.ReverseRoundtrip<VersionSafeStruct>((RawMessagePack)seq.AsReadOnlySequence);

		MessagePackReader reader = new(result);
		Assert.Equal(2, reader.ReadMapHeader());
	}

	private static bool IsMap(byte[] msgpack)
	{
		MessagePackReader reader = new(msgpack);
		return reader.NextMessagePackType == MessagePackType.Map;
	}

	private async ValueTask<byte[]> SerializeMaybeAsync<T>(T value, bool async)
#if NET
		where T : IShapeable<T>
#endif
	{
		if (async)
		{
			// Wrap the MemoryStream to hide its type to avoid the library taking a 'fast path'.
			using MemoryStream ms = new();
			using MonitoringStream wrapper = new(ms);
			await this.Serializer.SerializeAsync<T>(wrapper, value, this.TimeoutToken);
			return ms.ToArray();
		}
		else
		{
			return this.Serializer.Serialize<T>(value, this.TimeoutToken);
		}
	}

	private async ValueTask<T?> DeserializeMaybeAsync<T>(byte[] data, bool async)
#if NET
		where T : IShapeable<T>
#endif
	{
		if (async)
		{
			// Wrap the MemoryStream to hide its type to avoid the library taking a 'fast path'.
			using MemoryStream ms = new(data);
			using MonitoringStream wrapper = new(ms);
			return await this.Serializer.DeserializeAsync<T>(ms, this.TimeoutToken);
		}
		else
		{
			return this.Serializer.Deserialize<T>(data, this.TimeoutToken);
		}
	}

	private RawMessagePack ReverseRoundtrip<T>(RawMessagePack raw)
#if NET
		where T : IShapeable<T>
#endif
	{
		this.LogMsgPack(raw);
		T? value = this.Serializer.Deserialize<T>(raw, this.TimeoutToken);
		RawMessagePack result = (RawMessagePack)this.Serializer.Serialize(value, this.TimeoutToken);
		this.LogMsgPack(result);
		return result;
	}

	[GenerateShape]
	public partial struct VersionSafeStruct
	{
		public string? Name { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV1
	{
		public string? Name { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV2
	{
		public string? Name { get; set; }

		public int Age { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV1
	{
		[Key(0)]
		public string? Name { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV2
	{
		[Key(0)]
		public string? Name { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }

		[Key(1)]
#pragma warning disable SA1202 // Elements should be ordered by access - We want to test the special property not being the last one.
		public int Age { get; set; }
#pragma warning restore SA1202 // Elements should be ordered by access

		[Key(8)]
		public bool ForceMap { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV1NonDefaultCtor
	{
		public required string? Name { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV2NonDefaultCtor
	{
		public required string? Name { get; set; }

		public int Age { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV1NonDefaultCtor
	{
		[Key(0)]
		public required string? Name { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }

		[Key(8)]
#pragma warning disable SA1202 // Elements should be ordered by access - We want to test the special property not being the last one.
		public bool ForceMap { get; set; }
#pragma warning restore SA1202 // Elements should be ordered by access
	}

	[GenerateShape]
	public partial class ArrayModelV2NonDefaultCtor
	{
		[Key(0)]
		public required string? Name { get; set; }

		[Key(1)]
		public int Age { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		[PropertyShape]
		private UnusedDataPacket? UnusedData { get; set; }
	}
}
