// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class VersionSafeSerializationTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Theory, PairwiseData]
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

	[Theory, PairwiseData]
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

	[Theory, PairwiseData]
	public async Task ArrayObject(bool async, bool forceMap)
	{
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

	[Theory, PairwiseData]
	public async Task ArrayObject_NonDefaultCtor(bool async, bool forceMap)
	{
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

	[Fact]
	public void NonExplicitImplementationsThrow()
	{
		// We don't ever want the unused data packet to be serialized as an ordinary member of a class.
		// Our converters should throw when they observe this happening.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(new UnusedDataImplementedNonExplicitly(), TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.ToString());
		Assert.IsType<NotSupportedException>(ex.GetBaseException());
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
			await this.Serializer.SerializeAsync<T>(wrapper, value, TestContext.Current.CancellationToken);
			return ms.ToArray();
		}
		else
		{
			return this.Serializer.Serialize<T>(value, TestContext.Current.CancellationToken);
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
			return await this.Serializer.DeserializeAsync<T>(ms, TestContext.Current.CancellationToken);
		}
		else
		{
			return this.Serializer.Deserialize<T>(data, TestContext.Current.CancellationToken);
		}
	}

	[GenerateShape]
	public partial class MapModelV1 : IVersionSafeObject
	{
		public string? Name { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV2 : IVersionSafeObject
	{
		public string? Name { get; set; }

		public int Age { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV1 : IVersionSafeObject
	{
		[Key(0)]
		public string? Name { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV2 : IVersionSafeObject
	{
		[Key(0)]
		public string? Name { get; set; }

		[Key(1)]
		public int Age { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV1NonDefaultCtor : IVersionSafeObject
	{
		public required string? Name { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class MapModelV2NonDefaultCtor : IVersionSafeObject
	{
		public required string? Name { get; set; }

		public int Age { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV1NonDefaultCtor : IVersionSafeObject
	{
		[Key(0)]
		public required string? Name { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class ArrayModelV2NonDefaultCtor : IVersionSafeObject
	{
		[Key(0)]
		public required string? Name { get; set; }

		[Key(1)]
		public int Age { get; set; }

		[Key(8)]
		public bool ForceMap { get; set; }

		UnusedDataPacket? IVersionSafeObject.UnusedData { get; set; }
	}

	[GenerateShape]
	public partial class UnusedDataImplementedNonExplicitly : IVersionSafeObject
	{
		public string? Name { get; set; }

		public UnusedDataPacket? UnusedData { get; set; }
	}
}
