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
        internal partial record Person(int Age);
        #endregion
#else
        #region TopLevelStreamingEnumerationNETFX
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadListAsync(PipeReader reader)
        {
            await foreach (Person? item in Serializer.DeserializeEnumerableAsync<Person>(reader, Witness.GeneratedTypeShapeProvider))
            {
                // Process item here.
            }
        }

        internal record Person(int Age);

        [GenerateShapeFor<Person>]
        partial class Witness;
        #endregion
#endif
    }

    partial class StreamingEnumerationWithEnvelope
    {
#if NET
        #region StreamingEnumerationWithEnvelopeNET
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadFamilyMembersAsync(PipeReader reader)
        {
            MessagePackSerializer.StreamingEnumerationOptions<Family, Person> options = new(f => f.Members);
            await foreach (Person? item in Serializer.DeserializeEnumerableAsync(reader, options))
            {
                // Process item here.
            }
        }

        [GenerateShape]
        internal partial record Family(Person[] Members);

        internal record Person(int Age);
        #endregion
#else
        #region StreamingEnumerationWithEnvelopeNETFX
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadListAsync(PipeReader reader)
        {
            MessagePackSerializer.StreamingEnumerationOptions<Family, Person> options = new(f => f.Members);
            await foreach (Person? item in Serializer.DeserializeEnumerableAsync(reader, Witness.GeneratedTypeShapeProvider, options))
            {
                // Process item here.
            }
        }

        internal record Family(Person[] Members);

        internal record Person(int Age);

        [GenerateShapeFor<Family>]
        partial class Witness;
        #endregion
#endif
    }
}
