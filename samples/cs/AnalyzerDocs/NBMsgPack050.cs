// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack050
{
    namespace Defective
    {
#pragma warning disable NBMsgPack050
        internal class SomeClass
        {
            #region Defective
            void SomeMethod(MessagePackReader reader)
            {
            }
            #endregion
        }
#pragma warning restore NBMsgPack050
    }

    namespace Fixed
    {
        internal class SomeClass
        {
            #region Fix
            void SomeMethod(ref MessagePackReader reader)
            {
            }
            #endregion
        }
    }
}
