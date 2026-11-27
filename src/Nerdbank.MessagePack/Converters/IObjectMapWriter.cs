// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Exposes the serialized property names an object-map converter writes.
/// </summary>
internal interface IObjectMapWriter
{
	/// <summary>
	/// Determines whether the converter includes a property with the given serialized name.
	/// </summary>
	/// <param name="serializedName">The name written on the wire.</param>
	/// <returns><see langword="true"/> when the property is part of the map this converter writes.</returns>
	bool WritesProperty(string serializedName);
}
