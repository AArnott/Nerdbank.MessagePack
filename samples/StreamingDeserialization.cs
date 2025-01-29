// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace StreamingDeserialization
{
    partial class TopLevelStreamingEnumeration
    {
#if NET
        #region TopLevelStreamingEnumerationNET
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadListAsync(PipeReader reader)
        {
            await foreach (Person? item in Serializer.DeserializeEnumerableAsync<Person>(reader))
            {
                // Process item here.
            }
        }

        [GenerateShape]
        internal partial class Person
        {
            public int Age { get; set; }
        }
        #endregion
#else
        #region TopLevelStreamingEnumerationNETFX
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadListAsync(PipeReader reader)
        {
            await foreach (Person? item in Serializer.DeserializeEnumerableAsync<Person>(reader, Witness.ShapeProvider))
            {
                // Process item here.
            }
        }

        internal class Person
        {
            public int Age { get; set; }
        }

        [GenerateShape<Person>]
        partial class Witness;
        #endregion
#endif
    }
}
