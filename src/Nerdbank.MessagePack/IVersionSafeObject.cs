// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Implemented by types that can be serialized and deserialized in a version-safe manner
/// such that unknown properties are preserved in deserialization for later serialization.
/// </summary>
/// <remarks>
/// This interface should be implemented explicitly to avoid its property from being included in the serialization graph.
/// </remarks>
/// <example>
/// <para>The following data type recognizes its own declared properties and will avoid data loss when round-tripping data from a different declaration of the class that has additional properties.</para>
/// <code source="../../samples/cs/CustomizingSerialization.cs" region="VersionSafeObject" lang="C#" />
/// </example>
public interface IVersionSafeObject
{
	/// <summary>
	/// Gets or sets data that could not be deserialized into a known property,
	/// but that should be preserved for re-serialization.
	/// </summary>
	/// <remarks>
	/// Implementations of this are expected to be trivial auto-properties.
	/// </remarks>
	UnusedDataPacket? UnusedData { get; set; }
}
