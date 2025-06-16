// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.Converters;

#region Converter
/// <summary>
/// Converts a <see cref="BinaryData"/> object to and from MessagePack.
/// </summary>
/// <remarks>
/// <see cref="BinaryData"/> is declared in the "System.Memory.Data" package.
/// </remarks>
internal class BinaryDataConverter : MessagePackConverter<BinaryData>
{
    public override void Write(ref MessagePackWriter writer, in BinaryData? value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(value.ToMemory().Span);
    }

    public override BinaryData? Read(ref MessagePackReader reader, SerializationContext context)
    {
        return reader.ReadBytes() is { } bytes ? new BinaryData(bytes) : null;
    }
}
#endregion
