// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack110
{
    namespace Defective
    {
#pragma warning disable NBMsgPack110
        #region Defective
        [GenerateShape]
        public partial class MyType
        {
            public int Age { get; set; } = 18;
        }
        #endregion
#pragma warning restore NBMsgPack110
    }

    namespace Fixed
    {
        #region Fix
        [GenerateShape]
        public partial class MyType
        {
            [System.ComponentModel.DefaultValue(18)]
            public int Age { get; set; } = 18;
        }
        #endregion
    }
}
