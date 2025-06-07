// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class RawMessagePackTests : MessagePackSerializerTestBase
{
	[Fact]
	public void DefaultCtor()
	{
		RawMessagePack raw = default;
		Assert.True(raw.MsgPack.IsEmpty);
	}

	[Fact]
	public void CtorWithSequence()
	{
		Sequence<byte> bytes = new();
		bytes.Write((Span<byte>)[1, 2, 3]);
		RawMessagePack raw = new(bytes);
		Assert.Equal(bytes, raw.MsgPack);
	}

	[Fact]
	public void DeferredSerialization()
	{
		DeferredData userData = new() { UserString = "Hello, World!" };
		Envelope envelope = new() { Deferred = (RawMessagePack)this.Serializer.Serialize(userData, TestContext.Current.CancellationToken) };

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
		Envelope envelope = new() { Deferred = (RawMessagePack)this.Serializer.Serialize(userData, TestContext.Current.CancellationToken) };

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
		RawMessagePack empty1 = default;
		Assert.True(empty1.Equals(empty1));

		Sequence<byte> shortContiguousSequence = new();
		shortContiguousSequence.Write((Span<byte>)[1, 2]);
		RawMessagePack shortContiguousMsgPack = new(shortContiguousSequence);
		Assert.False(empty1.Equals(shortContiguousMsgPack));
		Assert.False(shortContiguousMsgPack.Equals(empty1));

		Sequence<byte> fragmentedSequence1 = new();
		fragmentedSequence1.Append(new byte[] { 1, 2 });
		fragmentedSequence1.Append(new byte[] { 3, 4, 5 });
		RawMessagePack fragmentedMsgPack1 = new(fragmentedSequence1);
		Assert.False(shortContiguousMsgPack.Equals(fragmentedMsgPack1));
		Assert.False(empty1.Equals(fragmentedMsgPack1));

		Sequence<byte> fragmentedSequence2 = new();
		fragmentedSequence2.Append(new byte[] { 1, 2, 3 });
		fragmentedSequence2.Append(new byte[] { 4, 5 });
		RawMessagePack fragmentedMsgPack2 = new(fragmentedSequence2);
		Assert.True(fragmentedMsgPack1.Equals(fragmentedMsgPack2));
	}

	[Fact]
	public void ToOwned_Empty()
	{
		RawMessagePack msgpack = new(default);
		Assert.False(msgpack.IsOwned);
		RawMessagePack owned = msgpack.ToOwned();
		Assert.True(owned.IsOwned);
	}

	[Fact]
	public void ToOwned_NonEmpty()
	{
		// Verify that we consider an initial version to be borrowed.
		RawMessagePack msgpack = (RawMessagePack)new byte[] { 1, 2, 3 };
		Assert.False(msgpack.IsOwned);
		ReadOnlySequence<byte> borrowedSequence = msgpack.MsgPack;

		// Verify that owning the data makes a copy.
		RawMessagePack owned = msgpack.ToOwned();
		Assert.True(owned.IsOwned);
		Assert.True(msgpack.IsOwned);
		Assert.NotEqual(borrowedSequence, owned.MsgPack);
		Assert.Equal(msgpack.MsgPack, owned.MsgPack);

		// Verify that owning again doesn't allocate new buffers.
		RawMessagePack reowned = owned.ToOwned();
		Assert.Equal(owned.MsgPack, reowned.MsgPack);
	}

	[Fact]
	public void ToString_Overridden()
	{
		Assert.Equal("<raw msgpack>", default(RawMessagePack).ToString());
	}

	[GenerateShape]
	public partial record Envelope
	{
		public RawMessagePack Deferred { get; set; }
	}

	[GenerateShape]
	public partial record DeferredData
	{
		public string? UserString { get; set; }
	}
}
