// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack073
{
    namespace Defective
    {
#pragma warning disable NBMsgPack073
        #region Defective
        [GenerateShape]
        public partial class MyType
        {
            [UseComparer(typeof(AbstractComparer))]
            public HashSet<string> MyHashSet { get; set; } = new();
        }

        public abstract class AbstractComparer : IEqualityComparer<string>
        {
            public abstract bool Equals(string? x, string? y);
            public abstract int GetHashCode(string obj);
        }
        #endregion
#pragma warning restore NBMsgPack073
    }

    namespace Fixed
    {
        #region Fix
        [GenerateShape]
        public partial class MyType
        {
            // Option 1: Use a concrete type
            [UseComparer(typeof(ConcreteComparer))]
            public HashSet<string> MyHashSet1 { get; set; } = new(new ConcreteComparer());

            // Option 2: Use a static member from an abstract type
            [UseComparer(typeof(AbstractComparerProvider), nameof(AbstractComparerProvider.Default))]
            public HashSet<string> MyHashSet2 { get; set; } = new(AbstractComparerProvider.Default);
        }

        public abstract class AbstractComparerProvider
        {
            public static IEqualityComparer<string> Default => StringComparer.OrdinalIgnoreCase;
        }

        public class ConcreteComparer : IEqualityComparer<string>
        {
            public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            public int GetHashCode(string obj) => obj?.ToUpperInvariant().GetHashCode() ?? 0;
        }
        #endregion
    }
}
