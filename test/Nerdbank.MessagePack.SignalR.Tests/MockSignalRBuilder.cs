// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

internal class MockSignalRBuilder : ISignalRBuilder
{
	public IServiceCollection Services { get; } = new ServiceCollection();
}
