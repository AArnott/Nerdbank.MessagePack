// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Vogen;

namespace ConsumeVogenWithMarshalers
{
    #region DataTypes
    [ValueObject<int>]
    [TypeShape(Marshaler = typeof(Marshaler), Kind = TypeShapeKind.None)]
    public partial struct CustomerId
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public class Marshaler : IMarshaler<CustomerId, int>
        {
            int IMarshaler<CustomerId, int>.Marshal(CustomerId value) => value.Value;
            CustomerId IMarshaler<CustomerId, int>.Unmarshal(int value) => From(value);
        }
    }

    [GenerateShape]
    public partial record Customer
    {
        public required CustomerId Id { get; set; }

        public required string Name { get; set; }
    }
    #endregion

    #region SerializeVogen
    class VogenConsumer
    {
        static MessagePackSerializer serializer = new();

        void Serialize(Customer customer)
        {
            byte[] msgpack = serializer.Serialize(customer);
        }
    }
    #endregion
}
