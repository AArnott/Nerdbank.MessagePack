// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace PrimitiveDeserialization
{
    internal partial class PrimitiveDeserialization
    {
        void DeserializeDynamicPrimitives(MessagePackSerializer serializer, byte[] msgpack)
        {
            #region DeserializeDynamicPrimitives
            MessagePackReader reader = new(msgpack);
            dynamic? deserialized = serializer.DeserializeDynamicPrimitives(ref reader);
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

        void DeserializePrimitives(MessagePackSerializer serializer, byte[] msgpack)
        {
            #region DeserializePrimitives
            MessagePackReader reader = new(msgpack);
            object? deserialized = serializer.DeserializePrimitives(ref reader);
            var map = (IReadOnlyDictionary<object, object?>)deserialized!;
            string prop1 = (string)map!["Prop1"]!;
            int prop2 = (int)map!["Prop2"]!;
            bool deeperBool = (bool)((IReadOnlyDictionary<object, object?>)map["deeper"]!)["IsAdult"]!;
            int age = (int)((IReadOnlyDictionary<object, object?>)((IReadOnlyDictionary<object, object?>)map["People"]!)[3]!)["Age"]!;

            // Did we miss anything? Dump out all the keys and their values.
            foreach (KeyValuePair<object, object?> pair in map)
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
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

            [GenerateShapeFor<ExpandoObject>]
            partial class Witness;
            #endregion
        }
#else
        partial class DeserializeExpandoObjectNETFX
        {
            #region DeserializeExpandoObjectNETFX
            void DeserializeExpandoObject(MessagePackSerializer serializer, byte[] msgpack)
            {
                dynamic? deserialized = serializer.Deserialize<ExpandoObject>(msgpack, Witness.GeneratedTypeShapeProvider);
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

            [GenerateShapeFor<ExpandoObject>]
            partial class Witness;
            #endregion
        }
#endif
    }
}
