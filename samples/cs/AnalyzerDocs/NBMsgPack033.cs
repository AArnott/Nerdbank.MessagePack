// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack033
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

            public override ValueTask<SomeCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

#pragma warning disable NBMsgPack033
            #region Defective
            public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                MessagePackWriter syncWriter = writer.CreateWriter();
                if (value is null)
                {
                    syncWriter.WriteNil();
                    return; // OOPS: exit without returning writer
                }

                syncWriter.Write(value.SeedCount);

                // OOPS: await without returning writer
                await writer.FlushIfAppropriateAsync(context);
            }
            #endregion
#pragma warning restore NBMsgPack033
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

            public override ValueTask<SomeCustomType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            #region Fix
            public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                MessagePackWriter syncWriter = writer.CreateWriter();
                if (value is null)
                {
                    syncWriter.WriteNil();
                    writer.ReturnWriter(ref syncWriter);
                    return;
                }

                syncWriter.Write(value.SeedCount);

                writer.ReturnWriter(ref syncWriter);
                await writer.FlushIfAppropriateAsync(context);
            }
            #endregion
        }
    }
}
