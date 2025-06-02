// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Contains extension methods to add optional converters.
/// </summary>
/// <remarks>
/// The library comes with many converters.
/// Some are not enabled by default to avoid unnecessary dependencies
/// and to keep a trimmed application size small when it doesn't require them.
/// The extension methods in this class can be used to turn these optional converters on.
/// </remarks>
public static class OptionalConverters
{
	/// <summary>
	/// Adds converters for common System.Text.Json types, including:
	/// <see cref="JsonNode"/>, <see cref="JsonElement"/>, and <see cref="JsonDocument"/> to the specified serializer.
	/// </summary>
	/// <param name="serializer">The serializer to add converters to.</param>
	/// <returns>The modified serializer.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="serializer"/> is null.</exception>
	public static MessagePackSerializer WithSystemTextJsonConverters(this MessagePackSerializer serializer)
	{
		Requires.NotNull(serializer, nameof(serializer));

		return serializer with
		{
			Converters = [
				..serializer.Converters,
				new JsonNodeConverter(),
				new JsonElementConverter(),
				new JsonDocumentConverter(),
			],
		};
	}
}
