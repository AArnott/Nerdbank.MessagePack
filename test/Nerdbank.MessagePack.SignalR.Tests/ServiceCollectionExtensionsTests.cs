// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using PolyType;
using Xunit;

namespace Nerdbank.MessagePack.SignalR.Tests;

public partial class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddMessagePackProtocol_RegistersProtocol()
	{
		MockSignalRBuilder builder = new();

		builder.AddMessagePackProtocol(Witness.ShapeProvider);

		ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
		IEnumerable<IHubProtocol> protocols = serviceProvider.GetServices<IHubProtocol>();

		Assert.Contains(protocols, p => p.Name == "messagepack");
	}

	[Fact]
	public void AddMessagePackProtocol_WithSerializer_RegistersProtocol()
	{
		MockSignalRBuilder builder = new();
		var serializer = new MessagePackSerializer();

		builder.AddMessagePackProtocol(Witness.ShapeProvider, serializer);

		ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
		IEnumerable<IHubProtocol> protocols = serviceProvider.GetServices<IHubProtocol>();

		Assert.Contains(protocols, p => p.Name == "messagepack");
	}

	[GenerateShapeFor<bool>]
	private partial class Witness;
}
