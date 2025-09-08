// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8321 // Local function is declared but never used

#region Configuration
using Nerdbank.MessagePack.AspNetCoreMvcFormatter;
using PolyType;

void ConfigureServices(IServiceCollection services)
{
    services.AddMvc().AddMvcOptions(option =>
    {
        option.OutputFormatters.Clear();
        option.OutputFormatters.Add(new MessagePackOutputFormatter(Witness.GeneratedTypeShapeProvider));
        option.InputFormatters.Clear();
        option.InputFormatters.Add(new MessagePackInputFormatter(Witness.GeneratedTypeShapeProvider));
    });
}

[GenerateShapeFor<bool>] // add an attribute for each top-level type that must be serializable
partial class Witness;
#endregion
