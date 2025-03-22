// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace PrimitiveDeserialization
{
    internal class PrimitiveDeserialization
    {
        void Deserialize(ref MessagePackReader reader)
        {
            #region DeserializePrimitives
            dynamic? deserialized = MessagePackSerializer.DeserializeDynamic(ref reader);
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
    }
}
