// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Nerdbank.MessagePack.SignalR;
using PolyType;

internal class HostedSignalR : IAsyncDisposable
{
	private HostedSignalR(TestServer server, HubConnection client)
	{
		this.Server = server;
		this.Client = client;
	}

	internal TestServer Server { get; }

	internal HubConnection Client { get; }

	public async ValueTask DisposeAsync()
	{
		await this.Client.DisposeAsync();
		this.Server.Dispose();
	}

	/// <summary>
	/// Sets up a test server and client connection.
	/// </summary>
	/// <param name="typeShapeProvider">The type shape provider to use for the Nerdbank.MessagePack hub protocol.</param>
	/// <param name="useNerdbankMessagePackForServer"><see langword="true" /> to use the Nerdbank.MessagePack implementation for the server; <see langword="false" /> to use MessagePack-CSharp.</param>
	/// <param name="useNerdbankMessagePackForClient"><see langword="true" /> to use the Nerdbank.MessagePack implementation for the client; <see langword="false" /> to use MessagePack-CSharp.</param>
	/// <param name="onSetupConnection">Optional callback to configure the connection before starting.</param>
	internal static async Task<HostedSignalR> CreateAsync(ITypeShapeProvider typeShapeProvider, bool useNerdbankMessagePackForServer = true, bool useNerdbankMessagePackForClient = true, Action<HubConnection>? onSetupConnection = null)
	{
		// Create server
		IWebHostBuilder hostBuilder = new WebHostBuilder()
			.ConfigureServices(services =>
			{
				ISignalRServerBuilder signalRBuilder = services.AddSignalR();

				if (useNerdbankMessagePackForServer)
				{
					signalRBuilder.AddMessagePackProtocol(typeShapeProvider);
				}
				else
				{
					signalRBuilder.AddMessagePackProtocol();
				}
			})
			.Configure(app =>
			{
				app.UseRouting();
				app.UseEndpoints(endpoints =>
				{
					endpoints.MapHub<IntegrationTestHub>("/testHub");
				});
			});

		TestServer server = new(hostBuilder);

		// Create client
		IHubConnectionBuilder connectionBuilder = new HubConnectionBuilder()
			.WithUrl("http://localhost/testHub", options =>
			{
				options.HttpMessageHandlerFactory = _ => server.CreateHandler();
			});

		if (useNerdbankMessagePackForClient)
		{
			connectionBuilder.AddMessagePackProtocol(typeShapeProvider);
		}
		else
		{
			connectionBuilder.AddMessagePackProtocol();
		}

		HubConnection client = connectionBuilder.Build();

		// Configure the connection if callback is provided
		onSetupConnection?.Invoke(client);

		await client.StartAsync();

		return new(server, client);
	}
}

#endif
