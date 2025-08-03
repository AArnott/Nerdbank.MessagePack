// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.ReflectionProvider;
using Xunit;

[assembly: TypeShapeExtension(typeof(CancelInvocationMessage), Marshaller = typeof(CancelInvocationMessage))]

internal class CancelInvocationMarshaller : IMarshaller<CancelInvocationMessage, CancelInvocationMarshaller.CancelInvocationSurrogate?>
{
	public CancelInvocationMessage? FromSurrogate(CancelInvocationSurrogate? surrogate)
		=> surrogate is null ? null : new(surrogate.Value.InvocationId!) { Headers = surrogate.Value.Headers };

	public CancelInvocationSurrogate? ToSurrogate(CancelInvocationMessage? value)
		=> value is null ? null : new(value.Headers, value.InvocationId);

	internal struct CancelInvocationSurrogate(IDictionary<string, string>? headers, string? invocationId)
	{
		[Key(0)]
		public int MessageType => HubProtocolConstants.CancelInvocationMessageType;

		/// <summary>
		/// Gets the headers associated with this message.
		/// </summary>
		[Key(1)]
		public IDictionary<string, string>? Headers { get; } = headers;

		/// <summary>
		/// Gets the invocation ID of the invocation to cancel.
		/// </summary>
		[Key(2)]
		public string? InvocationId { get; } = invocationId;
	}
}

public partial class SurrogateTesting
{
	[Fact]
	public void TestSurrogateSerialization()
	{
		CancelInvocationMessage message = new("123")
		{
			Headers = new Dictionary<string, string> { { "key", "value" } },
		};

		ITypeShapeProvider provider = ReflectionTypeShapeProvider.Default;
		MessagePackSerializer serializer = new();
		byte[] msgpack = serializer.Serialize(message, provider, TestContext.Current.CancellationToken);
		TestContext.Current.TestOutputHelper?.WriteLine(MessagePackSerializer.ConvertToJson(msgpack));
		CancelInvocationMessage? result = serializer.Deserialize<CancelInvocationMessage>(msgpack, provider, TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.Equal(message.InvocationId, result.InvocationId);
	}

	[GenerateShapeFor<CancelInvocationMessage>]
	private partial class Witness;
}
