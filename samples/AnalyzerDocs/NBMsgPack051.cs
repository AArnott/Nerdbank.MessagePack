// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

#if NET

namespace Samples.AnalyzerDocs.NBMsgPack051
{
    namespace Defective
    {
#pragma warning disable NBMsgPack051
        #region Defective
        [KnownSubType(typeof(MyDerived))] // NBMsgPack051: Use the generic version of this attribute instead.
        class MyType { }

        [GenerateShape]
        partial class MyDerived : MyType
        {
        }
        #endregion
#pragma warning restore NBMsgPack051
    }

    namespace SwitchFix
    {
        #region SwitchFix
        [KnownSubType<MyDerived>]
        class MyType { }

        [GenerateShape]
        partial class MyDerived : MyType
        {
        }
        #endregion
    }

    namespace MultiTargetingFix
    {
        #region MultiTargetingFix
        #if NET
        [KnownSubType<MyDerived>]
        #else
        [KnownSubType(typeof(MyDerived))]
        #endif
        class MyType { }

        #if NET
        [GenerateShape]
        #endif
        partial class MyDerived : MyType
        {
        }
        #endregion
    }
}

#endif
