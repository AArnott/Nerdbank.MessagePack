// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// An interface for all message pack converters.
/// </summary>
/// <typeparam name="T">The data type that can be converted by this object.</typeparam>
/// <remarks>
/// <para>
/// Authors of derived types should review <see href="https://aarnott.github.io/Nerdbank.MessagePack/docs/custom-converters.html">this documentation</see>
/// for important guidance on implementing a converter.
/// </para>
/// <para>
/// Key points to remember about each <see cref="Write"/> or <see cref="Read"/> method (or their async equivalents):
/// <list type="bullet">
/// <item>Read or write exactly one msgpack structure. Use an array or map header for multiple values.</item>
/// <item>Call <see cref="SerializationContext.DepthStep"/> before any significant work.</item>
/// <item>Delegate serialization of sub-values to a converter obtained using <see cref="SerializationContext.GetConverter{T}(ITypeShapeProvider)"/> rather than making a top-level call back to <see cref="MessagePackSerializer"/>.</item>
/// </list>
/// </para>
/// <para>
/// Implementations are encouraged to override <see cref="GetJsonSchema(Nerdbank.MessagePack.JsonSchemaContext, ITypeShape)"/> in order to support
/// <see cref="MessagePackSerializer.GetJsonSchema(ITypeShape)"/>.
/// </para>
/// </remarks>
internal static class MessagePackConverter
{
	internal static void VerifyFormat(Formatter formatter, StreamingDeformatter deformatter) => Verify.Operation(formatter is MsgPackFormatter && deformatter is MsgPackStreamingDeformatter, "This converter is specific to msgpack.");

	/// <summary>
	/// Creates a JSON schema fragment that provides a cursory description of a MessagePack extension.
	/// </summary>
	/// <param name="extensionCode">The extension code used.</param>
	/// <returns>A JSON schema fragment.</returns>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	internal static JsonObject CreateMsgPackExtensionSchema(sbyte extensionCode) => new()
	{
		// TODO: Review callers and revise JSON schema method to accept Formatter. Then pass the buck on the Formatter to reveal the schema.
		["type"] = "string",
		["pattern"] = FormattableString.Invariant($"^msgpack extension {extensionCode} as base64: "),
	};
}
