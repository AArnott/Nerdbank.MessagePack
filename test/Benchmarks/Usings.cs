// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

global using System.Buffers;
global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Jobs;
global using Nerdbank.PolySerializer;
global using Nerdbank.PolySerializer.MessagePack;
global using Nerdbank.Streams;
global using PolyType;
global using MsgPackCSharp = global::MessagePack;
global using Sequence = Nerdbank.Streams.Sequence<byte>;
