// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

namespace Samples.AnalyzerDocs.NBMsgPack035
{
    internal record SomeCustomType(int SeedCount);

    namespace Defective
    {
        internal class MyCustomConverter : Converter<SomeCustomType>
        {
            public override bool PreferAsyncSerialization => true;

            public override SomeCustomType? Read(ref Reader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            public override void Write(ref Writer writer, in SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }

#pragma warning disable NBMsgPack035
            #region Defective
            public override async ValueTask<SomeCustomType?> ReadAsync(AsyncReader reader, SerializationContext context)
            {
                StreamingReader streamingReader = reader.CreateStreamingReader();

                int count;
                while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                if (count != 1)
                {
                    throw new SerializationException();
                }

                await reader.BufferNextStructureAsync(context); // OOPS: streamingReader should have been returned first
                Reader bufferedReader = reader.CreateBufferedReader();
                int seedCount = bufferedReader.ReadInt32();

                return new SomeCustomType(seedCount); // OOPS: bufferedReader should have been returned first
            }
            #endregion
#pragma warning restore NBMsgPack035

            public override ValueTask WriteAsync(AsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }

    namespace Fixed
    {
        internal class MyCustomConverter : Converter<SomeCustomType>
        {
            public override bool PreferAsyncSerialization => true;

            public override SomeCustomType? Read(ref Reader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            public override void Write(ref Writer writer, in SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            #region Fix
            public override async ValueTask<SomeCustomType?> ReadAsync(AsyncReader reader, SerializationContext context)
            {
                StreamingReader streamingReader = reader.CreateStreamingReader();

                int count;
                while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                if (count != 1)
                {
                    throw new SerializationException();
                }

                reader.ReturnReader(ref streamingReader);
                await reader.BufferNextStructureAsync(context);
                Reader bufferedReader = reader.CreateBufferedReader();
                int seedCount = bufferedReader.ReadInt32();

                reader.ReturnReader(ref bufferedReader);
                return new SomeCustomType(seedCount);
            }
            #endregion

            public override ValueTask WriteAsync(AsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
