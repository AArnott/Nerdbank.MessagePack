// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

namespace Samples.AnalyzerDocs.NBMsgPack034
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

            public override ValueTask<SomeCustomType?> ReadAsync(AsyncReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

#pragma warning disable NBMsgPack034
            #region Defective
            public override async ValueTask WriteAsync(AsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                Writer syncWriter = writer.CreateWriter();
                if (value is null)
                {
                    syncWriter.WriteNull();
                    writer.ReturnWriter(ref syncWriter);
                    return;
                }

                syncWriter.WriteStartVector(2);
                syncWriter.Write(value.SeedCount);

                writer.ReturnWriter(ref syncWriter); // syncWriter returned here

                syncWriter.Write("Hi"); // OOPS: reused here

                await writer.FlushIfAppropriateAsync(context);
            }
            #endregion
#pragma warning restore NBMsgPack034
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

            public override ValueTask<SomeCustomType?> ReadAsync(AsyncReader reader, SerializationContext context)
            {
                throw new NotImplementedException();
            }

            #region Fix
            public override async ValueTask WriteAsync(AsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                Writer syncWriter = writer.CreateWriter();
                if (value is null)
                {
                    syncWriter.WriteNull();
                    writer.ReturnWriter(ref syncWriter);
                    return;
                }

                syncWriter.WriteStartVector(2);
                syncWriter.Write(value.SeedCount);
                syncWriter.Write("Hi");

                writer.ReturnWriter(ref syncWriter);
                await writer.FlushIfAppropriateAsync(context);
            }
            #endregion
        }
    }
}
