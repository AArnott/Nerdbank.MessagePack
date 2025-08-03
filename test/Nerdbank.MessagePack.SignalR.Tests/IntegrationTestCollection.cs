// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using Xunit;

/// <summary>
/// Defines the test collection for integration tests that share the same fixture.
/// </summary>
[CollectionDefinition("IntegrationTestCollection")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
	// This class has no code, and is never created. Its purpose is simply
	// to be the place to apply [CollectionDefinition] and all the
	// ICollectionFixture<> interfaces.
}

#endif
