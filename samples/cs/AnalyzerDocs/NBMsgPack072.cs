// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack072
{
    namespace Defective
    {
#pragma warning disable NBMsgPack072
        #region Defective
        [GenerateShape]
        public partial class MyType
        {
            #region Defective
            [UseComparer(typeof(NotAComparer))]
            public HashSet<string> MyHashSet { get; set; } = new();
            #endregion
        }

        public class NotAComparer
        {
        }
        #endregion
#pragma warning restore NBMsgPack072
    }

    namespace Fixed
    {
        #region Fix
        [GenerateShape]
        public partial class MyType
        {
            #region Fix
            [UseComparer(typeof(StringComparer))]
            public HashSet<string> MyHashSet { get; set; } = new(new StringComparer());
            #endregion
        }

        public class StringComparer : IEqualityComparer<string>
        {
            public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
            public int GetHashCode(string obj) => obj?.ToUpperInvariant().GetHashCode() ?? 0;
        }
        #endregion
    }
}
