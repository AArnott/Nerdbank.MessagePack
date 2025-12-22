// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

internal static class MessagePackWriterExtensions
{
	internal static void WriteByte(ref this MessagePackWriter writer, byte value) => writer.WriteUInt8(value);

	internal static void WriteSByte(ref this MessagePackWriter writer, sbyte value) => writer.WriteInt8(value);
}
