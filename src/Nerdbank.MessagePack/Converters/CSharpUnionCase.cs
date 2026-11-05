// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Describes the payload encoding of a C# union case while its converter is being constructed.
/// </summary>
internal sealed class CSharpUnionCase
{
	/// <summary>Gets or sets the payload converter, before marshaling to the union type.</summary>
	internal MessagePackConverter? PayloadConverter { get; set; }

	/// <summary>Gets or sets a value indicating whether integer writes must preserve width and signedness.</summary>
	internal bool UseFixedWidthIntegers { get; set; }

	/// <summary>Gets or sets the test for a null payload in a reference-typed union.</summary>
	internal Func<object, bool>? IsNullPayload { get; set; }
}
