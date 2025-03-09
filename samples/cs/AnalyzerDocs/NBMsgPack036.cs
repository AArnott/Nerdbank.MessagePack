// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

namespace Samples.AnalyzerDocs.NBMsgPack036
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

#pragma warning disable NBMsgPack036
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

                reader.ReturnReader(ref streamingReader); // streamingReader returned here

                int seedCount;
                while (streamingReader.TryRead(out seedCount).NeedsMoreBytes()) // OOPS: streamingReader reused here
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                return new SomeCustomType(seedCount);
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

                int seedCount;
                while (streamingReader.TryRead(out seedCount).NeedsMoreBytes())
                {
                    streamingReader = new(await streamingReader.FetchMoreBytesAsync());
                }

                reader.ReturnReader(ref streamingReader);
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
