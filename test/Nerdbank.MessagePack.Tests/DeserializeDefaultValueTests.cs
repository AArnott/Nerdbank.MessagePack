// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DeserializeDefaultValueTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	private static readonly ReadOnlySequence<byte> NullMessageMsgPackMap = CreateNullMessageObject();
	private static readonly ReadOnlySequence<byte> EmptyMsgPackMap = CreateEmptyMap();

	[Theory, PairwiseData]
	public async Task NullValueRejectedForNonNullableRequiredProperty(bool async)
	{
		await this.ExpectDeserializationThrowsAsync<RequiredNonNullProperty>(NullMessageMsgPackMap, async, MessagePackSerializationException.ErrorCode.DisallowedNullValue);
	}

	[Theory, PairwiseData]
	public async Task NullValueRejectedForNonNullableOptionalProperty(bool async)
	{
		await this.ExpectDeserializationThrowsAsync<OptionalNonNullProperty>(NullMessageMsgPackMap, async, MessagePackSerializationException.ErrorCode.DisallowedNullValue);
	}

	[Theory, PairwiseData]
	public async Task NullValueAllowedForNonNullableOptionalProperty_WithFlag(bool async)
	{
		this.Serializer = this.Serializer with { DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties };
		OptionalNonNullProperty? deserialized = await this.DeserializeMaybeAsync<OptionalNonNullProperty>(NullMessageMsgPackMap, async);
		Assert.NotNull(deserialized);
		Assert.Null(deserialized.Message);
	}

	[Theory, PairwiseData]
	public async Task NullValueAllowedForNonNullableRequiredProperty_WithFlag(bool async)
	{
		this.Serializer = this.Serializer with { DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowNullValuesForNonNullableProperties };
		RequiredNonNullProperty? deserialized = await this.DeserializeMaybeAsync<RequiredNonNullProperty>(NullMessageMsgPackMap, async);
		Assert.NotNull(deserialized);
		Assert.Null(deserialized.Message);
	}

	[Theory, PairwiseData]
	public async Task RejectMissingValueForNonNullableRequiredProperty(bool async)
	{
		await this.ExpectDeserializationThrowsAsync<RequiredNonNullProperty>(EmptyMsgPackMap, async, MessagePackSerializationException.ErrorCode.MissingRequiredProperty);
	}

	[Fact]
	public async Task RejectMissingValueForRequiredPropertyFromVeryLargeType()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(2);
		writer.Write("P5");
		writer.Write(true);
		writer.Write("P55");
		writer.Write(true);
		writer.Flush();

		MessagePackSerializationException ex = await this.ExpectDeserializationThrowsAsync<SharedTestTypes.RecordWith66RequiredProperties>(seq, async: false, MessagePackSerializationException.ErrorCode.MissingRequiredProperty);
		Assert.Contains("P4, P6", ex.Message);
		Assert.Contains("P64, P66", ex.Message);
	}

	[Theory, PairwiseData]
	public async Task MissingValueAllowedForNonNullableOptionalProperty(bool async)
	{
		OptionalNonNullProperty? deserialized = await this.DeserializeMaybeAsync<OptionalNonNullProperty>(EmptyMsgPackMap, async);
		Assert.NotNull(deserialized);
		Assert.Equal(string.Empty, deserialized.Message); // When omitted, the default value is the empty string for this particular property.
	}

	[Theory, PairwiseData]
	public async Task MissingValueAllowedForNonNullableRequiredProperty_WithFlag(bool async)
	{
		this.Serializer = this.Serializer with { DeserializeDefaultValues = DeserializeDefaultValuesPolicy.AllowMissingValuesForRequiredProperties };
		RequiredNonNullProperty? deserialized = await this.DeserializeMaybeAsync<RequiredNonNullProperty>(EmptyMsgPackMap, async);
		Assert.NotNull(deserialized);
		Assert.Null(deserialized.Message);
	}

	private static ReadOnlySequence<byte> CreateNullMessageObject()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(1);
		writer.Write(nameof(OptionalNonNullProperty.Message));
		writer.WriteNil();
		writer.Flush();
		return seq;
	}

	private static ReadOnlySequence<byte> CreateEmptyMap()
	{
		Sequence<byte> seq = new();
		MessagePackWriter writer = new(seq);
		writer.WriteMapHeader(0);
		writer.Flush();
		return seq;
	}

	private async ValueTask<T?> DeserializeMaybeAsync<T>(ReadOnlySequence<byte> msgpack, bool async)
#if NET
		where T : IShapeable<T>
#endif
	{
		return async
			? await this.Serializer.DeserializeAsync<T>(new MemoryStream(msgpack.ToArray()), TestContext.Current.CancellationToken)
			: this.Serializer.Deserialize<T>(msgpack, TestContext.Current.CancellationToken);
	}

	private async ValueTask<MessagePackSerializationException> ExpectDeserializationThrowsAsync<T>(ReadOnlySequence<byte> msgpack, bool async, MessagePackSerializationException.ErrorCode expectedCause)
#if NET
		where T : IShapeable<T>
#endif
	{
		MessagePackSerializationException ex = async
			? await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.DeserializeAsync<T>(new MemoryStream(msgpack.ToArray()), TestContext.Current.CancellationToken).AsTask())
			: Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<T>(msgpack, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.GetBaseException().Message);

		MessagePackSerializationException rootCauseException = Assert.IsType<MessagePackSerializationException>(ex.GetBaseException());
		Assert.Equal(expectedCause, rootCauseException.Code);

		return rootCauseException;
	}

	[GenerateShape]
	public partial record RequiredNonNullProperty(string Message);

	[GenerateShape]
	public partial record OptionalNonNullProperty(string Message = "");
}
