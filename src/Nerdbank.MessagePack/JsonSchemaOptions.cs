// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Options for JSON Schema generation.
/// </summary>
public sealed record JsonSchemaOptions
{
	/// <summary>
	/// Gets the shared immutable default options, which select <see cref="JsonSchemaDialect.Draft2020_12"/>.
	/// </summary>
	public static JsonSchemaOptions Default { get; } = new();

	/// <summary>
	/// Gets the JSON Schema dialect to emit. The default is <see cref="JsonSchemaDialect.Draft2020_12"/>.
	/// </summary>
	/// <remarks>The default is fixed and will not change when support for newer dialects is added.</remarks>
	public JsonSchemaDialect Dialect { get; init; } = JsonSchemaDialect.Draft2020_12;
}
