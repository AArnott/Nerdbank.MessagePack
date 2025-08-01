// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

#pragma warning disable SA1402 // File may only contain a single type

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;
using Xunit;

/// <summary>
/// Integration tests that involve a real Kestrel server and .NET SignalR client.
/// </summary>
public partial class IntegrationTests : IAsyncDisposable
{
	private TestServer? server;
	private HubConnection? connection;

	/// <summary>
	/// Test basic method invocation that returns a value.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_ReturnsValue_Success()
	{
		await this.SetupServerAndClientAsync();

		// Test string echo
		string result = await this.connection.InvokeAsync<string>("Echo", "Hello World", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("Echo: Hello World", result);

		// Test integer addition
		int sum = await this.connection.InvokeAsync<int>("Add", 5, 3, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(8, sum);

		// Test boolean
		bool isEven = await this.connection.InvokeAsync<bool>("IsEven", 4, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(isEven);
	}

	/// <summary>
	/// Test method invocation with custom types.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_CustomTypes_Success()
	{
		await this.SetupServerAndClientAsync();

		// Test creating a user
		TestUser user = await this.connection.InvokeAsync<TestUser>("CreateUser", "Alice", 30, "alice@example.com", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("Alice", user.Name);
		Assert.Equal(30, user.Age);
		Assert.Equal("alice@example.com", user.Email);

		// Test processing a message
		var message = new TestMessage
		{
			Content = "Test message",
			Sender = user,
			Tags = new List<string> { "test", "integration" },
		};

		TestMessage processedMessage = await this.connection.InvokeAsync<TestMessage>("ProcessMessage", message, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("Processed: Test message", processedMessage.Content);
		Assert.Equal(user.Name, processedMessage.Sender.Name);
	}

	/// <summary>
	/// Test method invocation that returns collections.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_Collections_Success()
	{
		await this.SetupServerAndClientAsync();

		List<TestUser> users = await this.connection.InvokeAsync<List<TestUser>>("GetUsers", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(3, users.Count);
		Assert.Contains(users, u => u.Name == "Alice");
		Assert.Contains(users, u => u.Name == "Bob");
		Assert.Contains(users, u => u.Name == "Charlie");
	}

	/// <summary>
	/// Test method invocation with nullable return types.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_NullableTypes_Success()
	{
		await this.SetupServerAndClientAsync();

		// Test finding existing user
		TestUser? alice = await this.connection.InvokeAsync<TestUser?>("FindUser", "alice", cancellationToken: TestContext.Current.CancellationToken);
		Assert.NotNull(alice);
		Assert.Equal("Alice", alice!.Name);

		// Test finding non-existing user
		TestUser? notFound = await this.connection.InvokeAsync<TestUser?>("FindUser", "nonexistent", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Null(notFound);
	}

	/// <summary>
	/// Test method invocation with enums and value types.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_EnumsAndValueTypes_Success()
	{
		await this.SetupServerAndClientAsync();

		// Test enum
		TestStatus status = await this.connection.InvokeAsync<TestStatus>("GetStatus", "test", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(TestStatus.Pending, status); // "test".Length % 3 == 1

		// Test value type (struct)
		var point = new TestPoint(10, 20);
		TestPoint movedPoint = await this.connection.InvokeAsync<TestPoint>("MovePoint", point, 5, -3, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(15, movedPoint.X);
		Assert.Equal(17, movedPoint.Y);
	}

	/// <summary>
	/// Test method invocation that throws exceptions.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_ThrowsException_PropagatesCorrectly()
	{
		await this.SetupServerAndClientAsync();

		// Test ArgumentException - SignalR masks detailed error info for security
		HubException ex1 = await Assert.ThrowsAsync<HubException>(
			() => this.connection.InvokeAsync<string>("ThrowError", "ArgumentException", cancellationToken: TestContext.Current.CancellationToken));
		Assert.Contains("An unexpected error occurred invoking 'ThrowError'", ex1.Message);

		// Test InvalidOperationException
		HubException ex2 = await Assert.ThrowsAsync<HubException>(
			() => this.connection.InvokeAsync<string>("ThrowError", "InvalidOperationException", cancellationToken: TestContext.Current.CancellationToken));
		Assert.Contains("An unexpected error occurred invoking 'ThrowError'", ex2.Message);
	}

	/// <summary>
	/// Test void method invocation (SendAsync).
	/// </summary>
	[Fact]
	public async Task SendAsync_VoidMethod_Success()
	{
		await this.SetupServerAndClientAsync();

		bool messageReceived = false;
		string? receivedMessage = null;

		this.connection.On<string>("ReceiveMessage", (message) =>
		{
			receivedMessage = message;
			messageReceived = true;
		});

		await this.connection.SendAsync("SendMessage", "Hello from test", cancellationToken: TestContext.Current.CancellationToken);

		// Wait a bit for the message to be processed
		await Task.Delay(100, TestContext.Current.CancellationToken);

		Assert.True(messageReceived);
		Assert.Equal("Hello from test", receivedMessage);
	}

	/// <summary>
	/// Test streaming method with primitive types.
	/// </summary>
	[Fact]
	public async Task StreamAsync_PrimitiveTypes_Success()
	{
		await this.SetupServerAndClientAsync();

		var receivedNumbers = new List<int>();

		await foreach (int number in this.connection.StreamAsync<int>("StreamNumbers", 5, cancellationToken: TestContext.Current.CancellationToken))
		{
			receivedNumbers.Add(number);
		}

		Assert.Equal(new[] { 1, 2, 3, 4, 5 }, receivedNumbers);
	}

	/// <summary>
	/// Test streaming method with complex types.
	/// </summary>
	[Fact]
	public async Task StreamAsync_ComplexTypes_Success()
	{
		await this.SetupServerAndClientAsync();

		var receivedMessages = new List<TestMessage>();

		await foreach (TestMessage message in this.connection.StreamAsync<TestMessage>("StreamMessages", "Test", 3, cancellationToken: TestContext.Current.CancellationToken))
		{
			receivedMessages.Add(message);
		}

		Assert.Equal(3, receivedMessages.Count);
		Assert.All(receivedMessages, m => Assert.StartsWith("Test Message", m.Content));
		Assert.All(receivedMessages, m => Assert.Equal("StreamBot", m.Sender.Name));
	}

	/// <summary>
	/// Test streaming cancellation.
	/// </summary>
	[Fact]
	public async Task StreamAsync_Cancellation_Success()
	{
		await this.SetupServerAndClientAsync();

		using var cts = new CancellationTokenSource();
		var receivedNumbers = new List<int>();

		// Cancel after receiving first item
		IAsyncEnumerable<int> stream = this.connection.StreamAsync<int>("StreamNumbers", 10, cts.Token);

		try
		{
			await foreach (int number in stream)
			{
				receivedNumbers.Add(number);
				if (number == 2)
				{
					cts.Cancel();
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancellation token is triggered
		}

		Assert.True(receivedNumbers.Count < 10); // Should have stopped early
	}

	/// <summary>
	/// Test compatibility between MessagePack-CSharp and Nerdbank.MessagePack implementations.
	/// </summary>
	[Theory, PairwiseData]
	public async Task ProtocolComparison_NerdbankVsCSharp_BothWork(bool nerdbankClient, bool nerdbankServer)
	{
		await this.SetupServerAndClientAsync(useNerdbankMessagePackForClient: nerdbankClient, useNerdbankMessagePackForServer: nerdbankServer);
		string messagePackResult = await this.connection.InvokeAsync<string>("Echo", "test", cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal("Echo: test", messagePackResult);
	}

	/// <summary>
	/// Test connection events.
	/// </summary>
	[Fact]
	public async Task ConnectionEvents_Work()
	{
		bool connectionReceived = false;
		string? connectionId = null;

		// Set up the server and client, but register the event handler before starting
		await this.SetupServerAndClientAsync(onSetupConnection: (connection) =>
		{
			connection.On<string>("UserConnected", (id) =>
			{
				connectionId = id;
				connectionReceived = true;
			});
		});

		// Wait a bit for the connection event to fire
		await Task.Delay(500, TestContext.Current.CancellationToken);

		// The connection event should have been received during setup
		Assert.True(connectionReceived, "UserConnected event was not received");
		Assert.NotNull(connectionId);
	}

	/// <summary>
	/// Disposes the test resources.
	/// </summary>
	public async ValueTask DisposeAsync()
	{
		await this.DisposeServerAndClientAsync();
	}

	/// <summary>
	/// Sets up a test server and client connection.
	/// </summary>
	/// <param name="useNerdbankMessagePackForServer"><see langword="true" /> to use the Nerdbank.MessagePack implementation for the server; <see langword="false" /> to use MessagePack-CSharp.</param>
	/// <param name="useNerdbankMessagePackForClient"><see langword="true" /> to use the Nerdbank.MessagePack implementation for the client; <see langword="false" /> to use MessagePack-CSharp.</param>
	/// <param name="onSetupConnection">Optional callback to configure the connection before starting.</param>
	[MemberNotNull(nameof(connection))]
	private async Task SetupServerAndClientAsync(bool useNerdbankMessagePackForServer = true, bool useNerdbankMessagePackForClient = true, Action<HubConnection>? onSetupConnection = null)
	{
		// Create server
		IWebHostBuilder hostBuilder = new WebHostBuilder()
			.ConfigureServices(services =>
			{
				ISignalRServerBuilder signalRBuilder = services.AddSignalR();

				if (useNerdbankMessagePackForServer)
				{
					signalRBuilder.AddMessagePackProtocol(IntegrationTestWitness.ShapeProvider);
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

		this.server = new TestServer(hostBuilder);

		// Create client
		IHubConnectionBuilder connectionBuilder = new HubConnectionBuilder()
			.WithUrl("http://localhost/testHub", options =>
			{
				options.HttpMessageHandlerFactory = _ => this.server.CreateHandler();
			});

		if (useNerdbankMessagePackForClient)
		{
			connectionBuilder.AddMessagePackProtocol(IntegrationTestWitness.ShapeProvider);
		}
		else
		{
			connectionBuilder.AddMessagePackProtocol();
		}

		this.connection = connectionBuilder.Build();

		// Configure the connection if callback is provided
		onSetupConnection?.Invoke(this.connection);

		await this.connection.StartAsync();
	}

	/// <summary>
	/// Disposes the server and client.
	/// </summary>
	private async Task DisposeServerAndClientAsync()
	{
		if (this.connection != null)
		{
			await this.connection.DisposeAsync();
			this.connection = null;
		}

		this.server?.Dispose();
		this.server = null;
	}
}

/// <summary>
/// Type shape witness for integration tests.
/// </summary>
[GenerateShapeFor<string>]
[GenerateShapeFor<int>]
[GenerateShapeFor<bool>]
[GenerateShapeFor<double>]
[GenerateShapeFor<DateTime>]
[GenerateShapeFor<TestUser>]
[GenerateShapeFor<TestMessage>]
[GenerateShapeFor<TestError>]
[GenerateShapeFor<TestStatus>]
[GenerateShapeFor<TestPoint>]
[GenerateShapeFor<List<string>>]
[GenerateShapeFor<List<TestUser>>]
[GenerateShapeFor<Dictionary<string, string>>] // Changed from object? to string for metadata compatibility
public partial class IntegrationTestWitness
{
}

#endif
