// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Test hub that provides various method signatures for integration testing.
/// </summary>
public class IntegrationTestHub : Hub
{
	/// <summary>
	/// Simple method that returns void.
	/// </summary>
	/// <param name="message">The message to send.</param>
	public async Task SendMessage(string message)
	{
		await this.Clients.All.SendAsync("ReceiveMessage", message);
	}

	/// <summary>
	/// Method that returns a value.
	/// </summary>
	/// <param name="input">The input string to echo.</param>
	/// <returns>The echoed string.</returns>
	public string Echo(string input) => $"Echo: {input}";

	/// <summary>
	/// Method that returns a primitive value.
	/// </summary>
	/// <param name="a">First number.</param>
	/// <param name="b">Second number.</param>
	/// <returns>The sum of the two numbers.</returns>
	public int Add(int a, int b) => a + b;

	/// <summary>
	/// Method that returns a boolean.
	/// </summary>
	/// <param name="number">The number to check.</param>
	/// <returns>True if the number is even, false otherwise.</returns>
	public bool IsEven(int number) => number % 2 == 0;

	/// <summary>
	/// Method that works with custom types.
	/// </summary>
	/// <param name="name">The user's name.</param>
	/// <param name="age">The user's age.</param>
	/// <param name="email">The user's email address.</param>
	/// <returns>A new test user.</returns>
	public TestUser CreateUser(string name, int age, string? email = null)
	{
		return new TestUser { Name = name, Age = age, Email = email };
	}

	/// <summary>
	/// Method that takes and returns complex types.
	/// </summary>
	/// <param name="message">The message to process.</param>
	/// <returns>The processed message.</returns>
	public TestMessage ProcessMessage(TestMessage message)
	{
		return message with { Content = $"Processed: {message.Content}" };
	}

	/// <summary>
	/// Method that returns a collection.
	/// </summary>
	/// <returns>A list of test users.</returns>
	public List<TestUser> GetUsers()
	{
		return new List<TestUser>
		{
			new() { Name = "Alice", Age = 30, Email = "alice@example.com" },
			new() { Name = "Bob", Age = 25 },
			new() { Name = "Charlie", Age = 35, Email = "charlie@example.com" },
		};
	}

	/// <summary>
	/// Method that throws an exception.
	/// </summary>
	/// <param name="errorType">The type of error to throw.</param>
	/// <returns>Never returns, always throws.</returns>
	public string ThrowError(string errorType)
	{
		throw errorType switch
		{
			"ArgumentException" => new ArgumentException("Test argument exception"),
			"InvalidOperationException" => new InvalidOperationException("Test invalid operation exception"),
			_ => new Exception("Generic test exception"),
		};
	}

	/// <summary>
	/// Method that returns nullable types.
	/// </summary>
	/// <param name="name">The name to search for.</param>
	/// <returns>The user if found, null otherwise.</returns>
	public TestUser? FindUser(string name)
	{
		return name.ToLowerInvariant() switch
		{
			"alice" => new TestUser { Name = "Alice", Age = 30, Email = "alice@example.com" },
			_ => null,
		};
	}

	/// <summary>
	/// Method that works with enums.
	/// </summary>
	/// <param name="entityId">The entity ID to get status for.</param>
	/// <returns>The status based on entity ID length.</returns>
	public TestStatus GetStatus(string entityId)
	{
		return (entityId.Length % 3) switch
		{
			0 => TestStatus.Active,
			1 => TestStatus.Pending,
			_ => TestStatus.Inactive,
		};
	}

	/// <summary>
	/// Method that works with value types.
	/// </summary>
	/// <param name="point">The point to move.</param>
	/// <param name="deltaX">The X offset.</param>
	/// <param name="deltaY">The Y offset.</param>
	/// <returns>The moved point.</returns>
	public TestPoint MovePoint(TestPoint point, double deltaX, double deltaY)
	{
		return new TestPoint(point.X + deltaX, point.Y + deltaY);
	}

	/// <summary>
	/// Method that returns a stream of data.
	/// </summary>
	/// <param name="count">The number of items to stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream of numbers.</returns>
	public async IAsyncEnumerable<int> StreamNumbers(int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		for (int i = 1; i <= count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return i;

			// Don't delay after the last item.
			if (i < count)
			{
				await Task.Delay(30, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Method that returns a stream of complex objects.
	/// </summary>
	/// <param name="prefix">The prefix for messages.</param>
	/// <param name="count">The number of messages to stream.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A stream of test messages.</returns>
	public async IAsyncEnumerable<TestMessage> StreamMessages(string prefix, int count, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var sender = new TestUser { Name = "StreamBot", Age = 0, Email = "bot@example.com" };

		for (int i = 1; i <= count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();

			yield return new TestMessage
			{
				Content = $"{prefix} Message {i}",
				Sender = sender,
				Tags = new List<string> { "stream", "test", $"item-{i}" },
			};

			if (i < count)
			{
				await Task.Delay(50, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Method for sending messages to a specific group.
	/// </summary>
	/// <param name="groupName">The target group.</param>
	/// <param name="message">The message to send.</param>
	public async Task SendToGroup(string groupName, string message)
	{
		await this.Clients.Group(groupName).SendAsync("GroupMessage", this.Context.ConnectionId, message);
	}

	/// <summary>
	/// Connection event handler.
	/// </summary>
	public override async Task OnConnectedAsync()
	{
		await this.Clients.All.SendAsync("UserConnected", this.Context.ConnectionId);
		await base.OnConnectedAsync();
	}

	/// <summary>
	/// Disconnection event handler.
	/// </summary>
	/// <param name="exception">The exception that caused disconnection, if any.</param>
	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		await this.Clients.All.SendAsync("UserDisconnected", this.Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}
}
