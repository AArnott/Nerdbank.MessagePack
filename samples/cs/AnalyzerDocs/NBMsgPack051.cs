// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPackAsync

namespace Samples.AnalyzerDocs.NBMsgPack051
{
#if NET

    namespace Defective
    {
#pragma warning disable NBMsgPack051
        #region Defective
        [GenerateShape]
        partial class MyType { }

        [GenerateShape]
        partial class MyDerivedType : MyType { }

        [GenerateShapeFor<MyType>]
        partial class Witness;

        class Foo
        {
            internal DerivedShapeMapping<MyType> ConstructUnionMapping()
            {
                DerivedShapeMapping<MyType> mapping = new();
                mapping.AddSourceGenerated<MyDerivedType>(1);
                return mapping;
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

        [GenerateShape]
        partial class MyDerivedType : MyType { }

        [GenerateShapeFor<MyType>]
        partial class Witness;

        class Foo
        {
            internal DerivedShapeMapping<MyType> ConstructUnionMapping()
            {
                DerivedShapeMapping<MyType> mapping = new();
                mapping.Add<MyDerivedType>(1);
                return mapping;
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

        [GenerateShape]
        partial class MyDerivedType : MyType { }

        [GenerateShapeFor<MyType>]
        partial class Witness;

        class Foo
        {
            internal DerivedShapeMapping<MyType> ConstructUnionMapping()
            {
                DerivedShapeMapping<MyType> mapping = new();
#if NET
                mapping.Add<MyDerivedType>(1);
#else
                mapping.AddSourceGenerated<MyDerivedType>(1);
#endif
                return mapping;
            }
        }
        #endregion
    }
}
