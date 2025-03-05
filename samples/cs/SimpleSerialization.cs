// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.Streams;
using PolyType.ReflectionProvider;

namespace Samples
{
    partial class TypelessSerialization
    {
        #region NonGenericSerializeDeserialize
        // Possible implementations of ITypeShapeProvider instances.
        static readonly ITypeShapeProvider ReflectionBased = ReflectionTypeShapeProvider.Default;
        static readonly ITypeShapeProvider SourceGenerated = Witness.ShapeProvider;

        object TypelessSerializeRoundtrip(object value, ITypeShapeProvider provider)
        {
            // Acquire the type's shape. For this sample, we'll use the runtime type of the object.
            ITypeShape shape = provider.GetShape(value.GetType()) ?? throw new ArgumentException("No shape available for runtime type", nameof(provider));

            MessagePackSerializer serializer = new();
            Sequence<byte> sequence = new();

            // Serialize the object.
            MessagePackWriter writer = new(sequence);
            serializer.SerializeObject(ref writer, value, shape);
            writer.Flush();

            // Deserialize the object.
            MessagePackReader reader = new(sequence);
            return serializer.DeserializeObject(ref reader, shape)!;
        }

        [GenerateShape<Fruit>]
        partial class Witness;

        [GenerateShape]
        internal partial record Fruit(int Seeds);
        #endregion
    }
}
