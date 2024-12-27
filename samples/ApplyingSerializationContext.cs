// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class ApplyingSerializationContext
{
    void ApplyingContext()
    {
        #region ApplyingStartingContext
        MessagePackSerializer serializer = new()
        {
            StartingContext = new SerializationContext
            {
                MaxDepth = 128,
            },
        };
        #endregion
        #region ModifyingStartingContext
        serializer = serializer with
        {
            StartingContext = serializer.StartingContext with
            {
                MaxDepth = 256,
            },
        };
        #endregion
        #region ModifyingStartingContextState
        SerializationContext context = serializer.StartingContext;
        context["MyState"] = "IsValue";
        serializer = serializer with
        {
            StartingContext = context,
        };
        #endregion
    }
}
