// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
using Nerdbank.MessagePack.SignalR;
using PolyType;

class Program
{
    static void Main(string[] args) => BasicSample(args);

    static void BasicSample(string[] args)
    {
        #region BasicSample
        var builder = WebApplication.CreateBuilder(args);

        // Add SignalR with MessagePack protocol
        builder.Services.AddSignalR()
            .AddMessagePackProtocol(Witness.ShapeProvider);

        var app = builder.Build();

        // Configure hub endpoint
        app.MapHub<ChatHub>("/chatHub");
        #endregion
    }

    static void Customized(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        #region CustomizedSerializer
        var customSerializer = new MessagePackSerializer
        {
            // Your custom configuration
        };

        builder.Services.AddSignalR()
            .AddMessagePackProtocol(Witness.ShapeProvider, customSerializer);
        #endregion
    }
}

#region Witness
[GenerateShapeFor<bool>] // add as many attributes as necessary for each RPC parameter and return type.
partial class Witness;
#endregion
