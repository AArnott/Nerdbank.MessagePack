// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

global using System.Buffers;
global using BenchmarkDotNet.Attributes;
global using PolyType;
global using ShapeShift;
global using ShapeShift.MessagePack;
global using MsgPackCSharp = global::MessagePack;
global using Sequence = Nerdbank.Streams.Sequence<byte>;
