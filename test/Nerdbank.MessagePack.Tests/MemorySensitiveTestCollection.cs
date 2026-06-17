// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

/// <summary>
/// Defines a test collection for tests that must not run in parallel with each other.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class MemorySensitiveTestCollection
{
	/// <summary>
	/// The name of this collection.
	/// </summary>
	public const string Name = nameof(MemorySensitiveTestCollection);
}
