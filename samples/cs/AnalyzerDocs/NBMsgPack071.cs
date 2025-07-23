// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack071
{
    namespace Defective
    {
#pragma warning disable NBMsgPack071
        #region Defective
        [GenerateShape]
        public partial class MyType
        {
            [UseComparer(typeof(MyComparerProvider), "Comparer")]
            public HashSet<string> MyHashSet { get; set; } = new();
        }

        internal class MyComparerProvider
        {
            private static IEqualityComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;
        }
        #endregion
#pragma warning restore NBMsgPack071
    }

    namespace Fixed
    {
        #region Fix
        [GenerateShape]
        public partial class MyType
        {
            [UseComparer(typeof(MyComparerProvider), nameof(MyComparerProvider.Comparer))]
            public HashSet<string> MyHashSet { get; set; } = new(MyComparerProvider.Comparer);
        }

        internal class MyComparerProvider
        {
            public static IEqualityComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;
        }
        #endregion
    }
}
