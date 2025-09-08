// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Xunit;

/// <summary>
/// Shared fixture for integration tests that provides a default server and client setup.
/// </summary>
public class IntegrationTestFixture : IAsyncLifetime
{
	private HostedSignalR? hosted;

	/// <summary>
	/// Gets the shared test server instance.
	/// </summary>
	public TestServer Server => this.hosted?.Server ?? throw new InvalidOperationException("Test connection is not initialized.");

	public HubConnection Client => this.hosted?.Client ?? throw new InvalidOperationException("Test connection is not initialized.");

	/// <summary>
	/// Initializes a new instance of the <see cref="IntegrationTestFixture"/> class.
	/// </summary>
	public async ValueTask InitializeAsync()
	{
		this.hosted = await HostedSignalR.CreateAsync(IntegrationTests.IntegrationTestWitness.GeneratedTypeShapeProvider);
	}

	/// <summary>
	/// Disposes the shared server resources.
	/// </summary>
	public ValueTask DisposeAsync() => this.hosted?.DisposeAsync() ?? ValueTask.CompletedTask;
}

#endif
