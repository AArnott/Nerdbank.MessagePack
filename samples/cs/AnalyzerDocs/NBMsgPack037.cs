// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack037
{
    internal class SomeCustomType;

    namespace Defective
    {
#pragma warning disable NBMsgPack037
        #region Defective
        internal class MyCustomConverter : MessagePackConverter<SomeCustomType>
        {
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
                return base.ReadAsync(reader, context);
            }

            public override ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                return base.WriteAsync(writer, value, context);
            }
        }
        #endregion
#pragma warning restore NBMsgPack037
    }

    namespace Fixed
    {
        internal class MyCustomConverter : MessagePackConverter<SomeCustomType>
        {
            #region Fix
            public override bool PreferAsyncSerialization => true;
            #endregion

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
                return base.ReadAsync(reader, context);
            }

            public override ValueTask WriteAsync(MessagePackAsyncWriter writer, SomeCustomType? value, SerializationContext context)
            {
                return base.WriteAsync(writer, value, context);
            }
        }
    }
}
