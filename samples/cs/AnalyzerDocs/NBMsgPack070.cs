// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8767
#endif

using System.Diagnostics.CodeAnalysis;

namespace Samples.AnalyzerDocs.NBMsgPack070
{
    public class MyComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y) => EqualityComparer<T?>.Default.Equals(x, y);
        public int GetHashCode([DisallowNull] T obj) => EqualityComparer<T>.Default.GetHashCode(obj);
    }

    namespace Defective
    {
#pragma warning disable NBMsgPack070
        [GenerateShape]
        public partial class MyType
        {
            #region Defective
            [UseComparer(typeof(MyComparer<>))]
            public HashSet<string> MyHashSet { get; set; } = new();
            #endregion
        }
#pragma warning restore NBMsgPack070
    }

    namespace Fixed
    {
        [GenerateShape]
        public partial class MyType
        {
            #region Fix
            [UseComparer(typeof(MyComparer<string>))]
            public HashSet<string> MyHashSet { get; set; } = new(new MyComparer<string>());
            #endregion
        }
    }
}
