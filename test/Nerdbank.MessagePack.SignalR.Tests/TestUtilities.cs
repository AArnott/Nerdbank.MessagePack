// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;

internal static class TestUtilities
{
	internal static IHubProtocol CreateHubProtocol(ITypeShapeProvider typeShapeProvider, MessagePackSerializer? serializer = null)
	{
		MockSignalRBuilder builder = new();
		builder.AddMessagePackProtocol(typeShapeProvider, serializer);
		ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
		return serviceProvider.GetService<IHubProtocol>() ?? throw new Exception("Missing hub protocol");
	}
}
