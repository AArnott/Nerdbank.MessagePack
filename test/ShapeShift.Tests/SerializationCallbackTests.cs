﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract partial class SerializationCallbackTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	private readonly byte[] objectAsArrayFormatted = serializer.Serialize(new BusyClassArray { Name = "Alice" });
	private readonly byte[] objectAsMapFormatted = serializer.Serialize(new BusyClassMap { Name = "Alice" });
	private readonly BusyClassArray objectAsArray = new() { Name = "Alice" };
	private readonly BusyClassMap objectAsMap = new() { Name = "Alice" };

	[Theory, PairwiseData]
	public async Task Serialize_ObjectAsArray(bool async)
	{
		if (async)
		{
			await this.Serializer.SerializeAsync(Stream.Null, this.objectAsArray, TestContext.Current.CancellationToken);
		}
		else
		{
			this.Serializer.Serialize(this.objectAsArray, TestContext.Current.CancellationToken);
		}

		Assert.Equal(1, this.objectAsArray.OnBeforeSerializeCounter);
		Assert.Equal(0, this.objectAsArray.OnAfterDeserializeCounter);
	}

	[Theory, PairwiseData]
	public async Task Serialize_ObjectAsMap(bool async)
	{
		if (async)
		{
			await this.Serializer.SerializeAsync(Stream.Null, this.objectAsMap, TestContext.Current.CancellationToken);
		}
		else
		{
			this.Serializer.Serialize(this.objectAsMap, TestContext.Current.CancellationToken);
		}

		Assert.Equal(1, this.objectAsMap.OnBeforeSerializeCounter);
		Assert.Equal(0, this.objectAsMap.OnAfterDeserializeCounter);
	}

	[Theory, PairwiseData]
	public async Task Deserialize_ObjectAsArray(bool async)
	{
		BusyClassArray? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassArray>(PipeReader.Create(new(this.objectAsArrayFormatted)), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<BusyClassArray>(this.objectAsArrayFormatted, TestContext.Current.CancellationToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Theory, PairwiseData]
	public async Task Deserialize_ObjectAsMap(bool async)
	{
		BusyClassMap? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassMap>(PipeReader.Create(new(this.objectAsMapFormatted)), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<BusyClassMap>(this.objectAsMapFormatted, TestContext.Current.CancellationToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Theory, PairwiseData]
	public async Task Deserialize_ObjectAsArray_Init(bool async)
	{
		BusyClassArray? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassArrayInit>(PipeReader.Create(new(this.objectAsArrayFormatted)), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<BusyClassArrayInit>(this.objectAsArrayFormatted, TestContext.Current.CancellationToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	[Theory, PairwiseData]
	public async Task Deserialize_ObjectAsMap_Init(bool async)
	{
		BusyClassMap? obj = async
			? await this.Serializer.DeserializeAsync<BusyClassMapInit>(PipeReader.Create(new(this.objectAsMapFormatted)), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<BusyClassMapInit>(this.objectAsMapFormatted, TestContext.Current.CancellationToken);
		Assert.NotNull(obj);

		Assert.Equal(0, obj.OnBeforeSerializeCounter);
		Assert.Equal(1, obj.OnAfterDeserializeCounter);
	}

	public class Json() : SerializationCallbackTests(CreateJsonSerializer());

	public class MsgPack() : SerializationCallbackTests(CreateMsgPackSerializer());

	[GenerateShape]
	public partial class BusyClassMap : ISerializationCallbacks, IEquatable<BusyClassMap>
	{
		public string? Name { get; set; }

		internal int OnBeforeSerializeCounter { get; private set; }

		internal int OnAfterDeserializeCounter { get; private set; }

		void ISerializationCallbacks.OnBeforeSerialize() => this.OnBeforeSerializeCounter++;

		void ISerializationCallbacks.OnAfterDeserialize() => this.OnAfterDeserializeCounter++;

		public bool Equals(BusyClassMap? other) => other != null && this.Name == other.Name;
	}

	[GenerateShape]
	public partial class BusyClassArray : ISerializationCallbacks, IEquatable<BusyClassArray>
	{
		[Key(0)]
		public string? Name { get; set; }

		internal int OnBeforeSerializeCounter { get; private set; }

		internal int OnAfterDeserializeCounter { get; private set; }

		void ISerializationCallbacks.OnBeforeSerialize() => this.OnBeforeSerializeCounter++;

		void ISerializationCallbacks.OnAfterDeserialize() => this.OnAfterDeserializeCounter++;

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
