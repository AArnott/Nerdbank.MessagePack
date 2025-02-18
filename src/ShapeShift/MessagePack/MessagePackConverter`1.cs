// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace ShapeShift.MessagePack;

/// <summary>
/// An abstract base class for messagepack-specific converters.
/// </summary>
/// <typeparam name="T">The data type to be converted.</typeparam>
internal abstract class MessagePackConverter<T> : Converter<T>
{
	/// <inheritdoc/>
	public override void VerifyCompatibility(Formatter formatter, StreamingDeformatter deformatter) => VerifyFormat(formatter, deformatter);

	/// <summary>
	/// Throws if the given formatter and deformatter are not for messagepack.
	/// </summary>
	/// <param name="formatter">The formatter.</param>
	/// <param name="deformatter">The deformatter.</param>
	protected internal static void VerifyFormat(Formatter formatter, StreamingDeformatter deformatter) => Verify.Operation(formatter is MessagePackFormatter && deformatter is MessagePackStreamingDeformatter, "This converter is specific to msgpack.");

	/// <summary>
	/// Creates a JSON object that describes the schema for a messagepack extension type.
	/// </summary>
	/// <param name="extensionCode">The extension type code.</param>
	/// <returns>A JSON object.</returns>
	protected static JsonObject CreateMsgPackExtensionSchema(sbyte extensionCode) => new()
	{
		// TODO: Review callers and revise JSON schema method to accept Formatter. Then pass the buck on the Formatter to reveal the schema.
		["type"] = "string",
		["pattern"] = FormattableString.Invariant($"^msgpack extension {extensionCode} as base64: "),
	};
}
