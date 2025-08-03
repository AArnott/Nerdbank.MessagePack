// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.SignalR.Client;
using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;

static partial class Client
{
    static async Task BasicConfig()
    {
        #region Basic
        var connection = new HubConnectionBuilder()
            .WithUrl("https://example.com/chatHub")
            .AddMessagePackProtocol(Witness.ShapeProvider)
            .Build();

        await connection.StartAsync();
        #endregion
    }

    static async Task CustomSerializerConfig()
    {
        #region CustomSerializer
        var serializer = new MessagePackSerializer
        {
            // custom configuration
        };

        var connection = new HubConnectionBuilder()
            .WithUrl("https://example.com/chatHub")
            .AddMessagePackProtocol(Witness.ShapeProvider, serializer)
            .Build();
        #endregion

        await connection.StartAsync();
    }

    [GenerateShapeFor<bool>]
    partial class Witness;
}
