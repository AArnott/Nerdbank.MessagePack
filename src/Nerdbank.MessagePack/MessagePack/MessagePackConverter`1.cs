// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft;

namespace ShapeShift.MessagePack;

internal abstract class MessagePackConverter<T> : Converter<T>
{
	public override void VerifyCompatibility(Formatter formatter, StreamingDeformatter deformatter) => VerifyFormat(formatter, deformatter);

	protected internal static void VerifyFormat(Formatter formatter, StreamingDeformatter deformatter) => Verify.Operation(formatter is MsgPackFormatter && deformatter is MsgPackStreamingDeformatter, "This converter is specific to msgpack.");

	protected static JsonObject CreateMsgPackExtensionSchema(sbyte extensionCode) => new()
	{
		// TODO: Review callers and revise JSON schema method to accept Formatter. Then pass the buck on the Formatter to reveal the schema.
		["type"] = "string",
		["pattern"] = FormattableString.Invariant($"^msgpack extension {extensionCode} as base64: "),
	};
}
