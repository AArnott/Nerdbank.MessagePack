// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using PolyType;

/// <summary>
/// Enum for testing enum serialization.
/// </summary>
public enum TestStatus
{
	Unknown,
	Active,
	Inactive,
	Pending,
}

/// <summary>
/// Value type for testing struct serialization.
/// </summary>
[GenerateShape]
public partial struct TestPoint(double x, double y)
{
	public double X => x;

	public double Y => y;
}

/// <summary>
/// Simple model for testing serialization.
/// </summary>
[GenerateShape]
public partial record TestUser
{
	public required string Name { get; init; }

	public required int Age { get; init; }

	public string? Email { get; init; }
}

/// <summary>
/// Model with nested properties.
/// </summary>
[GenerateShape]
public partial record TestMessage
{
	public required string Content { get; init; }

	public required TestUser Sender { get; init; }

	public DateTime Timestamp { get; init; } = DateTime.UtcNow;

	public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Model for testing error scenarios.
/// </summary>
[GenerateShape]
public partial record TestError
{
	public required string Code { get; init; }

	public required string Message { get; init; }

	public Dictionary<string, object?> Details { get; init; } = new();
}
