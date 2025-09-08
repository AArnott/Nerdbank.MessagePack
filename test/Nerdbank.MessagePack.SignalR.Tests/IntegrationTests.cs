// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using PolyType;
using Xunit;

/// <summary>
/// Integration tests that involve a real Kestrel server and .NET SignalR client.
/// </summary>
[Collection("IntegrationTestCollection")]
public partial class IntegrationTests(IntegrationTestFixture fixture)
{
	/// <summary>
	/// Test basic method invocation that returns a value.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_ReturnsValue_Success()
	{
		// Test string echo
		string result = await fixture.Client.InvokeAsync<string>("Echo", "Hello World", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("Echo: Hello World", result);

		// Test integer addition
		int sum = await fixture.Client.InvokeAsync<int>("Add", 5, 3, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(8, sum);

		// Test boolean
		bool isEven = await fixture.Client.InvokeAsync<bool>("IsEven", 4, cancellationToken: TestContext.Current.CancellationToken);
		Assert.True(isEven);
	}

	/// <summary>
	/// Test method invocation with custom types.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_CustomTypes_Success()
	{
		// Test creating a user
		TestUser user = await fixture.Client.InvokeAsync<TestUser>("CreateUser", "Alice", 30, "alice@example.com", cancellationToken: TestContext.Current.CancellationToken);
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

		TestMessage processedMessage = await fixture.Client.InvokeAsync<TestMessage>("ProcessMessage", message, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal("Processed: Test message", processedMessage.Content);
		Assert.Equal(user.Name, processedMessage.Sender.Name);
	}

	/// <summary>
	/// Test method invocation that returns collections.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_Collections_Success()
	{
		List<TestUser> users = await fixture.Client.InvokeAsync<List<TestUser>>("GetUsers", cancellationToken: TestContext.Current.CancellationToken);
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
		// Test finding existing user
		TestUser? alice = await fixture.Client.InvokeAsync<TestUser?>("FindUser", "alice", cancellationToken: TestContext.Current.CancellationToken);
		Assert.NotNull(alice);
		Assert.Equal("Alice", alice!.Name);

		// Test finding non-existing user
		TestUser? notFound = await fixture.Client.InvokeAsync<TestUser?>("FindUser", "nonexistent", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Null(notFound);
	}

	/// <summary>
	/// Test method invocation with enums and value types.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_EnumsAndValueTypes_Success()
	{
		// Test enum
		TestStatus status = await fixture.Client.InvokeAsync<TestStatus>("GetStatus", "test", cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(TestStatus.Pending, status); // "test".Length % 3 == 1

		// Test value type (struct)
		var point = new TestPoint(10, 20);
		TestPoint movedPoint = await fixture.Client.InvokeAsync<TestPoint>("MovePoint", point, 5, -3, cancellationToken: TestContext.Current.CancellationToken);
		Assert.Equal(15, movedPoint.X);
		Assert.Equal(17, movedPoint.Y);
	}

	/// <summary>
	/// Test method invocation that throws exceptions.
	/// </summary>
	[Fact]
	public async Task InvokeMethod_ThrowsException_PropagatesCorrectly()
	{
		// Test ArgumentException - SignalR masks detailed error info for security
		HubException ex1 = await Assert.ThrowsAsync<HubException>(
			() => fixture.Client.InvokeAsync<string>("ThrowError", "ArgumentException", cancellationToken: TestContext.Current.CancellationToken));
		Assert.Contains("An unexpected error occurred invoking 'ThrowError'", ex1.Message);

		// Test InvalidOperationException
		HubException ex2 = await Assert.ThrowsAsync<HubException>(
			() => fixture.Client.InvokeAsync<string>("ThrowError", "InvalidOperationException", cancellationToken: TestContext.Current.CancellationToken));
		Assert.Contains("An unexpected error occurred invoking 'ThrowError'", ex2.Message);
	}

	/// <summary>
	/// Test void method invocation (SendAsync).
	/// </summary>
	[Fact]
	public async Task SendAsync_VoidMethod_Success()
	{
		bool messageReceived = false;
		string? receivedMessage = null;

		fixture.Client.On<string>("ReceiveMessage", (message) =>
		{
			receivedMessage = message;
			messageReceived = true;
		});

		await fixture.Client.SendAsync("SendMessage", "Hello from test", cancellationToken: TestContext.Current.CancellationToken);

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
		var receivedNumbers = new List<int>();

		await foreach (int number in fixture.Client.StreamAsync<int>("StreamNumbers", 5, cancellationToken: TestContext.Current.CancellationToken))
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
		var receivedMessages = new List<TestMessage>();

		await foreach (TestMessage message in fixture.Client.StreamAsync<TestMessage>("StreamMessages", "Test", 3, cancellationToken: TestContext.Current.CancellationToken))
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
		using var cts = new CancellationTokenSource();
		var receivedNumbers = new List<int>();

		// Cancel after receiving first item
		IAsyncEnumerable<int> stream = fixture.Client.StreamAsync<int>("StreamNumbers", 10, cts.Token);

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
	public async Task ProtocolInterop_NerdbankVsCSharp(bool nerdbankClient, bool nerdbankServer)
	{
		await using HostedSignalR hosted = await HostedSignalR.CreateAsync(IntegrationTestWitness.GeneratedTypeShapeProvider, useNerdbankMessagePackForClient: nerdbankClient, useNerdbankMessagePackForServer: nerdbankServer);
		string messagePackResult = await hosted.Client.InvokeAsync<string>("Echo", "test", cancellationToken: TestContext.Current.CancellationToken);

		Assert.Equal("Echo: test", messagePackResult);
	}

	/// <summary>
	/// Test connection events.
	/// </summary>
	[Fact]
	public async Task ConnectionEvents_Work()
	{
		// Set up the server and client, but register the event handler before starting
		TaskCompletionSource<string> connectionIdSource = new();
		await using HostedSignalR hosted = await HostedSignalR.CreateAsync(IntegrationTestWitness.GeneratedTypeShapeProvider, onSetupConnection: (connection) =>
		{
			connection.On<string>("UserConnected", (id) =>
			{
				connectionIdSource.SetResult(id);
			});
		});

		string connectionId = await connectionIdSource.Task.WaitAsync(TestContext.Current.CancellationToken);
		Assert.NotNull(connectionId);
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
	internal partial class IntegrationTestWitness;
}

#endif
