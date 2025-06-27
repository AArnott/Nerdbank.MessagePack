// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class SimpleUsage
{
    #region SimpleRecord
    [GenerateShape]
    public partial record ARecord(string AString, bool ABoolean);
    #endregion

#if NET
    #region SimpleRecordRoundtripNET
    void Roundtrip()
    {
        // Construct a value.
        var value = new ARecord("hello", true);

        // Create a serializer instance.
        MessagePackSerializer serializer = new();

        // Serialize the value to the buffer.
        byte[] msgpack = serializer.Serialize(value);

        // Deserialize it back.
        ARecord? deserialized = serializer.Deserialize<ARecord>(msgpack);
    }
    #endregion
#else
    #region SimpleRecordRoundtripNETFX
    void Roundtrip()
    {
        // Construct a value.
        var value = new ARecord("hello", true);

        // Create a serializer instance.
        MessagePackSerializer serializer = new();

        // Serialize the value to the buffer.
        byte[] msgpack = serializer.Serialize(value, Witness.ShapeProvider);

        // Deserialize it back.
        ARecord? deserialized = serializer.Deserialize<ARecord>(msgpack, Witness.ShapeProvider);
    }

    [GenerateShapeFor<ARecord>]
    internal partial class Witness;
    #endregion
#endif
}
