// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Benchmarks;

[JsonSerializable(typeof(PocoMap))]
[JsonSerializable(typeof(PocoMapInit))]
[JsonSerializable(typeof(PocoAsArray))]
[JsonSerializable(typeof(PocoAsArrayInit))]
internal partial class STJSourceGenerationContext : JsonSerializerContext;
