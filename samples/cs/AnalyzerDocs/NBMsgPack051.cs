// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

using System.Buffers;

namespace Samples.AnalyzerDocs.NBMsgPack051
{
#if NET

    namespace Defective
    {
#pragma warning disable NBMsgPack051
        #region Defective
        class MyType { }

        [GenerateShapeFor<MyType>]
        partial class Witness;

        class Foo
        {
            private readonly MessagePackSerializer serializer = new();

            internal void Serialize(IBufferWriter<byte> writer, MyType value)
            {
                this.serializer.Serialize(writer, value, Witness.ShapeProvider); // NBMsgPack051: Use an overload that takes a constrained type instead.
            }
        }
        #endregion
#pragma warning restore NBMsgPack051
    }

    namespace SwitchFix
    {
        #region SwitchFix
        [GenerateShape]
        partial class MyType { }

        class Foo
        {
            private readonly MessagePackSerializer serializer = new();

            internal void Serialize(IBufferWriter<byte> writer, MyType value)
            {
                this.serializer.Serialize(writer, value);
            }
        }
        #endregion
    }
#endif

    namespace MultiTargetingFix
    {
        #region MultiTargetingFix
        [GenerateShape]
        partial class MyType { }

        [GenerateShapeFor<MyType>]
        partial class Witness;

        class Foo
        {
            private readonly MessagePackSerializer serializer = new();

            internal void Serialize(IBufferWriter<byte> writer, MyType value)
            {
#if NET
                this.serializer.Serialize(writer, value);
#else
                this.serializer.Serialize(writer, value, Witness.ShapeProvider);
#endif
            }
        }
        #endregion
    }
}
