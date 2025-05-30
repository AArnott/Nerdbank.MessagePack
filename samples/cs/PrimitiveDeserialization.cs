// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace PrimitiveDeserialization
{
    internal partial class PrimitiveDeserialization
    {
        void DeserializeDynamic(MessagePackSerializer serializer, ref MessagePackReader reader)
        {
            #region DeserializePrimitives
            dynamic? deserialized = serializer.DeserializeDynamic(ref reader);
            string prop1 = deserialized!.Prop1;
            int prop2 = deserialized.Prop2;
            bool deeperBool = deserialized.deeper.IsAdult;
            int age = deserialized.People[3].Age;

            // Did we miss anything? Dump out all the keys and their values.
            foreach (object key in deserialized)
            {
                Console.WriteLine($"{key}: {deserialized[key]}");
            }
            #endregion
        }

#if NET
        partial class DeserializeExpandoObjectNET
        {
            #region DeserializeExpandoObjectNET
            void DeserializeExpandoObject(MessagePackSerializer serializer, byte[] msgpack)
            {
                dynamic? deserialized = serializer.Deserialize<ExpandoObject, Witness>(msgpack);
                string prop1 = deserialized!.Prop1;
                int prop2 = deserialized.Prop2;
                bool deeperBool = deserialized.deeper.IsAdult;
                int age = deserialized.People[3].Age;

                // Did we miss anything? Dump out all the keys and their values.
                foreach (object key in deserialized)
                {
                    Console.WriteLine($"{key}: {deserialized[key]}");
                }
            }

            [GenerateShape<ExpandoObject>]
            partial class Witness;
            #endregion
        }
#else
        partial class DeserializeExpandoObjectNETFX
        {
            #region DeserializeExpandoObjectNETFX
            void DeserializeExpandoObject(MessagePackSerializer serializer, byte[] msgpack)
            {
                dynamic? deserialized = serializer.Deserialize<ExpandoObject>(msgpack, Witness.ShapeProvider);
                string prop1 = deserialized!.Prop1;
                int prop2 = deserialized.Prop2;
                bool deeperBool = deserialized.deeper.IsAdult;
                int age = deserialized.People[3].Age;

                // Did we miss anything? Dump out all the keys and their values.
                foreach (object key in deserialized)
                {
                    Console.WriteLine($"{key}: {deserialized[key]}");
                }
            }

            [GenerateShape<ExpandoObject>]
            partial class Witness;
            #endregion
        }
#endif
    }
}
