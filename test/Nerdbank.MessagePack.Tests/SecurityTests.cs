// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class SecurityTests
{
	/// <summary>
	/// Verifies that the deserializer will guard against stack overflow attacks.
	/// </summary>
	[Fact(Skip = "Not yet implemented.")]
	public void StackGuard()
	{
		// The max depth should be configurable on the MessagePackSerializer object.
	}

	/// <summary>
	/// Verifies that the dictionaries created by the deserializer use collision resistant key hashes.
	/// </summary>
	[Fact(Skip = "Not yet implemented.")]
	public void CollisionResistantHashMaps()
	{
	}
}
