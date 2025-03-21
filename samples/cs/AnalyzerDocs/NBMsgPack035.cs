﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

namespace Samples.AnalyzerDocs.NBMsgPack035
{
    internal record SomeCustomType(int SeedCount);

    namespace Defective
    {
        internal class MyCustomConverter : MessagePackConverter<SomeCustomType>
        {
            public override bool PreferAsyncSerialization => true;

            public override SomeCustomType? Read(ref MessagePackReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            public override void Write(ref MessagePackWriter writer, in SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }

#pragma warning disable NBMsgPack035
            #region Defective
            public override async ValueTask<SomeCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
            {
                MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

                int count;
                while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                if (count != 1)
                {
                    throw new MessagePackSerializationException();
                }

                await reader.BufferNextStructureAsync(context); // OOPS: streamingReader should have been returned first
                MessagePackReader bufferedReader = reader.CreateBufferedReader();
                int seedCount = bufferedReader.ReadInt32();

                return new SomeCustomType(seedCount); // OOPS: bufferedReader should have been returned first
            }
            #endregion
#pragma warning restore NBMsgPack035

            public override ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }

    namespace Fixed
    {
        internal class MyCustomConverter : MessagePackConverter<SomeCustomType>
        {
            public override bool PreferAsyncSerialization => true;

            public override SomeCustomType? Read(ref MessagePackReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            public override void Write(ref MessagePackWriter writer, in SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            #region Fix
            public override async ValueTask<SomeCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
            {
                MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

                int count;
                while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                if (count != 1)
                {
                    throw new MessagePackSerializationException();
                }

                reader.ReturnReader(ref streamingReader);
                await reader.BufferNextStructureAsync(context);
                MessagePackReader bufferedReader = reader.CreateBufferedReader();
                int seedCount = bufferedReader.ReadInt32();

                reader.ReturnReader(ref bufferedReader);
                return new SomeCustomType(seedCount);
            }
            #endregion

            public override ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
