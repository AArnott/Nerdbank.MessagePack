// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VogenDataTypes;

#region Sample
partial class VogenConsumer
{
    static MessagePackSerializer serializer = new();

#pragma warning disable NBMsgPack051 // We cannot use the type constraint overload because Vogen requires that we use Witness types.
    void Serialize(Customer customer)
    {
        byte[] msgpack = serializer.Serialize(customer, Witness.ShapeProvider);
    }

    [GenerateShapeFor<Customer>]
    partial class Witness;
}
#endregion
