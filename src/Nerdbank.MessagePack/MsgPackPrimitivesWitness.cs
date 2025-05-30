// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// A shape provider for all msgpack primitive types.
/// </summary>
[GenerateShape<string>]
[GenerateShape<sbyte>]
[GenerateShape<byte>]
[GenerateShape<short>]
[GenerateShape<ushort>]
[GenerateShape<uint>]
[GenerateShape<int>]
[GenerateShape<long>]
[GenerateShape<ulong>]
[GenerateShape<float>]
[GenerateShape<double>]
[GenerateShape<byte[]>]
[GenerateShape<DateTime>]
[GenerateShape<Extension>]
internal partial class MsgPackPrimitivesWitness;
