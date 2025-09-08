// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack051

using System.Numerics;

partial class Performance
{
    MessagePackSerializer serializer = new();

    void LoadsJustRight()
    {
        #region LoadsJustRight
        this.serializer.Serialize(
            value: true,
            PolyType.SourceGenerator.TypeShapeProvider_Samples.Default.Boolean);
        #endregion
    }

    #region LoadsTooMany
    void LoadsTooMany()
    {
        this.serializer.Serialize(value: true, Witness.GeneratedTypeShapeProvider);
    }

    [GenerateShapeFor<bool>]
    [GenerateShapeFor<BigInteger>]
    partial class Witness;
    #endregion
}
