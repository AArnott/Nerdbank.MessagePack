// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;

namespace StreamingSerialization
{
    partial class TopLevelStreamingSerialization
    {
        #region TopLevelStreamingSerialization
        private static readonly MessagePackSerializer Serializer = new();

        async Task WritePeopleAsync(PipeWriter writer, IAsyncEnumerable<Person?> people)
        {
            // Each person is one top-level MessagePack value. There is no array around them.
            await Serializer.SerializeEnumerableAsync(writer, people);
            await writer.FlushAsync();
        }

        [GenerateShape]
        internal partial record Person(int Age);
        #endregion
    }

    partial class ArrayStreamingSerialization
    {
        #region ArrayStreamingSerialization
        private static readonly MessagePackSerializer Serializer = new();

        async Task WriteFamilyAsync(PipeWriter writer, IAsyncEnumerable<Person?> members, int count)
        {
            // MessagePack stores the array length first, so the caller supplies count.
            await Serializer.SerializeArrayAsync(writer, members, count);
            await writer.FlushAsync();
        }

        [GenerateShape]
        internal partial record Person(int Age);
        #endregion
    }
}
