// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Identifies a supported JSON Schema dialect.
/// </summary>
/// <remarks>
/// Values follow publication order: drafts 0 through 7 reserve values 0 through 7,
/// Draft 2019-09 reserves value 8, and Draft 2020-12 uses value 9.
/// Only supported dialects have named members. Larger values identify newer dialects.
/// </remarks>
public enum JsonSchemaDialect
{
	/// <summary>
	/// JSON Schema Draft 4.
	/// </summary>
	Draft4 = 4,

	/// <summary>
	/// JSON Schema Draft 2020-12.
	/// </summary>
	Draft2020_12 = 9,
}
