// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Nerdbank.MessagePack;
using PolyType;

namespace Samples;

internal partial class JsonSchemaExport
{
    internal static void Export()
    {
        #region SelectDialect
        MessagePackSerializer serializer = new();
        JsonObject schema = serializer.GetJsonSchema<Person>(new JsonSchemaOptions
        {
            Dialect = JsonSchemaDialect.Draft4,
        });
        #endregion
    }

    [GenerateShape]
    internal partial class Person
    {
        public string? Name { get; set; }
    }
}
