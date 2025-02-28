// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public abstract partial class PreformattedRawBytesTests(SerializerBase serializer) : SerializerTestBase(serializer)
{
	[Fact]
	public void DefaultCtor()
	{
		PreformattedRawBytes raw = default;
		Assert.True(raw.RawBytes.IsEmpty);
	}

	[Fact]
	public void CtorWithSequence()
	{
		Sequence<byte> bytes = new();
		bytes.Write((Span<byte>)[1, 2, 3]);
		PreformattedRawBytes raw = new(bytes);
		Assert.Equal(bytes, raw.RawBytes);
	}

	[Fact]
	public void DeferredSerialization()
	{
		DeferredData userData = new() { UserString = "Hello, World!" };
		Sequence<byte> formattedBytes = new();
		this.Serializer.Serialize(formattedBytes, userData, TestContext.Current.CancellationToken);
		Envelope envelope = new() { Deferred = (PreformattedRawBytes)formattedBytes.AsReadOnlySequence };

		Envelope? deserializedEnvelope = this.Roundtrip(envelope);

		Assert.NotNull(deserializedEnvelope);
		Assert.Equal(envelope, deserializedEnvelope);
		Assert.True(deserializedEnvelope.Deferred.IsOwned);
		DeferredData? deserializedUserData = this.Serializer.Deserialize<DeferredData>(deserializedEnvelope.Deferred, TestContext.Current.CancellationToken);
		Assert.Equal(userData, deserializedUserData);
	}

	[Fact]
	public async Task DeferredSerializationAsync()
	{
		DeferredData userData = new() { UserString = "Hello, World!" };
		Sequence<byte> formattedBytes = new();
		this.Serializer.Serialize(formattedBytes, userData, TestContext.Current.CancellationToken);
		Envelope envelope = new() { Deferred = (PreformattedRawBytes)formattedBytes.AsReadOnlySequence };

		Envelope? deserializedEnvelope = await this.RoundtripAsync(envelope);

		Assert.NotNull(deserializedEnvelope);
		Assert.Equal(envelope, deserializedEnvelope);
		Assert.True(deserializedEnvelope.Deferred.IsOwned);
		DeferredData? deserializedUserData = this.Serializer.Deserialize<DeferredData>(deserializedEnvelope.Deferred, TestContext.Current.CancellationToken);
		Assert.Equal(userData, deserializedUserData);
	}

	[Fact]
	public void Equality()
	{
		PreformattedRawBytes empty1 = default;
		Assert.True(empty1.Equals(empty1));

		Sequence<byte> shortContiguousSequence = new();
		shortContiguousSequence.Write((Span<byte>)[1, 2]);
		PreformattedRawBytes shortContiguousMsgPack = new(shortContiguousSequence);
		Assert.False(empty1.Equals(shortContiguousMsgPack));
		Assert.False(shortContiguousMsgPack.Equals(empty1));

		Sequence<byte> fragmentedSequence1 = new();
		fragmentedSequence1.Append(new byte[] { 1, 2 });
		fragmentedSequence1.Append(new byte[] { 3, 4, 5 });
		PreformattedRawBytes fragmentedMsgPack1 = new(fragmentedSequence1);
		Assert.False(shortContiguousMsgPack.Equals(fragmentedMsgPack1));
		Assert.False(empty1.Equals(fragmentedMsgPack1));

		Sequence<byte> fragmentedSequence2 = new();
		fragmentedSequence2.Append(new byte[] { 1, 2, 3 });
		fragmentedSequence2.Append(new byte[] { 4, 5 });
		PreformattedRawBytes fragmentedMsgPack2 = new(fragmentedSequence2);
		Assert.True(fragmentedMsgPack1.Equals(fragmentedMsgPack2));
	}

	[Fact]
	public void ToOwned_Empty()
	{
		PreformattedRawBytes msgpack = new(default);
		Assert.False(msgpack.IsOwned);
		PreformattedRawBytes owned = msgpack.ToOwned();
		Assert.True(owned.IsOwned);
	}

	[Fact]
	public void ToOwned_NonEmpty()
	{
		// Verify that we consider an initial version to be borrowed.
		PreformattedRawBytes msgpack = (PreformattedRawBytes)new byte[] { 1, 2, 3 };
		Assert.False(msgpack.IsOwned);
		ReadOnlySequence<byte> borrowedSequence = msgpack.RawBytes;

		// Verify that owning the data makes a copy.
		PreformattedRawBytes owned = msgpack.ToOwned();
		Assert.True(owned.IsOwned);
		Assert.True(msgpack.IsOwned);
		Assert.NotEqual(borrowedSequence, owned.RawBytes);
		Assert.Equal(msgpack.RawBytes, owned.RawBytes);

		// Verify that owning again doesn't allocate new buffers.
		PreformattedRawBytes reowned = owned.ToOwned();
		Assert.Equal(owned.RawBytes, reowned.RawBytes);
	}

	public class Json() : PreformattedRawBytesTests(CreateJsonSerializer());

	public class MsgPack() : PreformattedRawBytesTests(CreateMsgPackSerializer());

	[GenerateShape]
	public partial record Envelope
	{
		public PreformattedRawBytes Deferred { get; set; }
	}

	[GenerateShape]
	public partial record DeferredData
	{
		public string? UserString { get; set; }
	}
}
