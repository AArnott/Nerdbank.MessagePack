// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Samples.AnalyzerDocs.NBMsgPack120
{
    namespace Defective
    {
#pragma warning disable NBMsgPack120
        #region Defective
        struct Counter : IMessagePackSerializationCallbacks
        {
            public int Mutations;

            public void OnBeforeSerialize() => this.Mutations++;

            public void OnAfterSerialize() => this.Mutations++;

            public void OnBeforeDeserialize() => this.Mutations++;

            public void OnAfterDeserialize() => this.Mutations++;
        }

        record struct RecordCounter : IMessagePackSerializationCallbacks
        {
            public int Mutations { get; set; }

            public void OnBeforeSerialize() => this.Mutations++;

            public void OnAfterSerialize() => this.Mutations++;

            public void OnBeforeDeserialize() => this.Mutations++;

            public void OnAfterDeserialize() => this.Mutations++;
        }
        #endregion
#pragma warning restore NBMsgPack120
    }

    namespace Fixed
    {
        #region Fix
        class Counter : IMessagePackSerializationCallbacks
        {
            public int Mutations { get; set; }

            public void OnBeforeSerialize() => this.Mutations++;

            public void OnAfterSerialize() => this.Mutations++;

            public void OnBeforeDeserialize() => this.Mutations++;

            public void OnAfterDeserialize() => this.Mutations++;
        }
        #endregion
    }
}
