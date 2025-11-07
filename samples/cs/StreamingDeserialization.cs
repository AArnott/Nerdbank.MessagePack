// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace StreamingDeserialization
{
    partial class TopLevelStreamingEnumeration
    {
        #region TopLevelStreamingEnumeration
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
    }

    partial class StreamingEnumerationWithEnvelope
    {
        #region StreamingEnumerationWithEnvelope
        private static readonly MessagePackSerializer Serializer = new();

        async Task ReadFamilyMembersAsync(PipeReader reader)
        {
            MessagePackSerializer.StreamingEnumerationOptions<Family, Person> options = new(f => f.Members);
            await foreach (Person? item in Serializer.DeserializePathEnumerableAsync(reader, options))
            {
                // Process item here.
            }
        }

        [GenerateShape]
        internal partial record Family(Person[] Members);

        internal record Person(int Age);
        #endregion
    }
}
