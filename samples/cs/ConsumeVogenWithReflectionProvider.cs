// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.ReflectionProvider;
using Vogen;

namespace ConsumeVogenWithMarshalers
{
    #region DataTypes
    [ValueObject<int>]
    public partial struct CustomerId;

    public record Customer
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
            // Use the reflection-based type shape provider to handle Vogen-generated types
            // because the PolyType source generator cannot see Vogen's source generated code.
            // Alternatively, define the data types in another project and reference that project
            // from here, then PolyType source generated type shapes will be available.
            byte[] msgpack = serializer.Serialize(customer, ReflectionTypeShapeProvider.Default);
        }
    }
    #endregion
}
