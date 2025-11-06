// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8321 // Local function is declared but never used

using Microsoft.AspNetCore.Mvc;
using Nerdbank.MessagePack.AspNetCoreMvcFormatter;
using PolyType;

var builder = WebApplication.CreateBuilder(args);
ConfigureServices(builder.Services);

var app = builder.Build();

app.UseRouting();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

#region Configuration
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

// Generate a shape for ProblemDetails so that error responses can be serialized.
[GenerateShapeFor<ProblemDetails>]
partial class Witness;
#endregion
