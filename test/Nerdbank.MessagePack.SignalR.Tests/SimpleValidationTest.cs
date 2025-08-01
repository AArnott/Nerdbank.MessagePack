// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;
using Xunit;

/// <summary>
/// Simple validation that our SignalR integration compiles and basic functionality works.
/// </summary>
public partial class SimpleValidationTest
{
	[Fact]
	public void BasicInstantiation_Works()
	{
		IHubProtocol protocol = CreateProtocol();
		Assert.NotNull(protocol);
		Assert.Equal("messagepack", protocol.Name);
		Assert.Equal(2, protocol.Version);
		Assert.Equal(TransferFormat.Binary, protocol.TransferFormat);
	}

	[Fact]
	public void ServiceRegistration_Works()
	{
		MockSignalRBuilder builder = new();
		builder.AddMessagePackProtocol(Witness.ShapeProvider);
		ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
		IEnumerable<IHubProtocol> registeredProtocols = serviceProvider.GetServices<IHubProtocol>();

		Assert.Contains(registeredProtocols, p => p.Name == "messagepack");
	}

	private static IHubProtocol CreateProtocol(MessagePackSerializer? serializer = null)
		=> TestUtilities.CreateHubProtocol(Witness.ShapeProvider, serializer);

	[GenerateShapeFor<bool>]
	private partial class Witness;
}
