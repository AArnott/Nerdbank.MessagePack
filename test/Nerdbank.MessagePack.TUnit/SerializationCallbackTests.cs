// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SerializationCallbackTests : MessagePackSerializerTestBase
{
	private static readonly byte[] ObjectAsArrayMsgPack = new MessagePackSerializer().Serialize(new BusyClassArray { Name = "Alice" });
	private static readonly byte[] ObjectAsMapMsgPack = new MessagePackSerializer().Serialize(new BusyClassMap { Name = "Alice" });
	private readonly BusyClassArray objectAsArray = new() { Name = "Alice" };
	private readonly BusyClassMap objectAsMap = new() { Name = "Alice" };

	[Test, MatrixDataSource]
	public async Task Serialize_ObjectAsArray(bool async)
	{
		if (async)
		{
			await this.Serializer.SerializeAsync(Stream.Null, this.objectAsArray, this.TimeoutToken);
		}
		else
		{
			this.Serializer.Serialize(this.objectAsArray, this.TimeoutToken);
		}

		Assert.Equal(1, this.objectAsArray.OnBeforeSerializeCounter);
		Assert.Equal(1, this.objectAsArray.OnAfterSerializeCounter);
		Assert.Equal(0, this.objectAsArray.OnBeforeDeserializeCounter);
		Assert.Equal(0, this.objectAsArray.OnAfterDeserializeCounter);
	}

	[Test, MatrixDataSource]
	public async Task Serialize_ObjectAsMap(bool async)
	{
		if (async)
		{
			await this.Serializer.SerializeAsync(Stream.Null, this.objectAsMap, this.TimeoutToken);
		}
		else
		{
			this.Serializer.Serialize(this.objectAsMap, this.TimeoutToken);
		}

		Assert.Equal(1, this.objectAsMap.OnBeforeSerializeCounter);
		Assert.Equal(1, this.objectAsMap.OnAfterSerializeCounter);
		Assert.Equal(0, this.objectAsMap.OnBeforeDeserializeCounter);
		Assert.Equal(0, this.objectAsMap.OnAfterDeserializeCounter);
	}

	[Test, MatrixDataSource]
	public async Task Deserialize_ObjectAsArray(bool async)
	{
		BusyClassArray? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassArray>(PipeReader.Create(new(ObjectAsArrayMsgPack)), this.TimeoutToken)
			: this.Serializer.Deserialize<BusyClassArray>(ObjectAsArrayMsgPack, this.TimeoutToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(0, obj.OnAfterSerializeCounter);
		Assert.Equal(1, obj.OnBeforeDeserializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Test, MatrixDataSource]
	public async Task Deserialize_ObjectAsMap(bool async)
	{
		BusyClassMap? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassMap>(PipeReader.Create(new(ObjectAsMapMsgPack)), this.TimeoutToken)
			: this.Serializer.Deserialize<BusyClassMap>(ObjectAsMapMsgPack, this.TimeoutToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(0, obj.OnAfterSerializeCounter);
		Assert.Equal(1, obj.OnBeforeDeserializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Test, MatrixDataSource]
	public async Task Deserialize_ObjectAsArray_Init(bool async)
	{
		BusyClassArray? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassArrayInit>(PipeReader.Create(new(ObjectAsArrayMsgPack)), this.TimeoutToken)
			: this.Serializer.Deserialize<BusyClassArrayInit>(ObjectAsArrayMsgPack, this.TimeoutToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(0, obj.OnAfterSerializeCounter);
		Assert.Equal(0, obj.OnBeforeDeserializeCounter); // Because PolyType constructs these objects all at once, so we can't intercept to invoke a callback.
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Test, MatrixDataSource]
	public async Task Deserialize_ObjectAsMap_Init(bool async)
	{
		BusyClassMap? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassMapInit>(PipeReader.Create(new(ObjectAsMapMsgPack)), this.TimeoutToken)
			: this.Serializer.Deserialize<BusyClassMapInit>(ObjectAsMapMsgPack, this.TimeoutToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(0, obj.OnAfterSerializeCounter);
		Assert.Equal(0, obj.OnBeforeDeserializeCounter); // Because PolyType constructs these objects all at once, so we can't intercept to invoke a callback.
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[GenerateShape]
	public partial class BusyClassMap : IMessagePackSerializationCallbacks, IEquatable<BusyClassMap>
	{
		public string? Name { get; set; }

		internal int OnBeforeSerializeCounter { get; private set; }

		internal int OnAfterSerializeCounter { get; private set; }

		internal int OnBeforeDeserializeCounter { get; private set; }

		internal int OnAfterDeserializeCounter { get; private set; }

		void IMessagePackSerializationCallbacks.OnBeforeSerialize() => this.OnBeforeSerializeCounter++;

		void IMessagePackSerializationCallbacks.OnAfterSerialize() => this.OnAfterSerializeCounter++;

		void IMessagePackSerializationCallbacks.OnBeforeDeserialize() => this.OnBeforeDeserializeCounter++;

		void IMessagePackSerializationCallbacks.OnAfterDeserialize() => this.OnAfterDeserializeCounter++;

		public bool Equals(BusyClassMap? other) => other != null && this.Name == other.Name;
	}

	[GenerateShape]
	public partial class BusyClassArray : IMessagePackSerializationCallbacks, IEquatable<BusyClassArray>
	{
		[Key(0)]
		public string? Name { get; set; }

		internal int OnBeforeSerializeCounter { get; private set; }

		internal int OnAfterSerializeCounter { get; private set; }

		internal int OnBeforeDeserializeCounter { get; private set; }

		internal int OnAfterDeserializeCounter { get; private set; }

		void IMessagePackSerializationCallbacks.OnBeforeSerialize() => this.OnBeforeSerializeCounter++;

		void IMessagePackSerializationCallbacks.OnAfterSerialize() => this.OnAfterSerializeCounter++;

		void IMessagePackSerializationCallbacks.OnBeforeDeserialize() => this.OnBeforeDeserializeCounter++;

		void IMessagePackSerializationCallbacks.OnAfterDeserialize() => this.OnAfterDeserializeCounter++;

		public bool Equals(BusyClassArray? other) => other != null && this.Name == other.Name;
	}

	[GenerateShape]
	public partial class BusyClassMapInit : BusyClassMap
	{
		public bool AdditionalProperty { get; init; }
	}

	[GenerateShape]
	public partial class BusyClassArrayInit : BusyClassArray
	{
		[Key(1)]
		public bool AdditionalProperty { get; init; }
	}
}
