// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Nerdbank.MessagePack;

/// <summary>
/// A shape provider for all msgpack primitive types.
/// </summary>
[GenerateShapeFor<string>]
[GenerateShapeFor<sbyte>]
[GenerateShapeFor<byte>]
[GenerateShapeFor<short>]
[GenerateShapeFor<ushort>]
[GenerateShapeFor<uint>]
[GenerateShapeFor<int>]
[GenerateShapeFor<long>]
[GenerateShapeFor<ulong>]
[GenerateShapeFor<float>]
[GenerateShapeFor<double>]
[GenerateShapeFor<byte[]>]
[GenerateShapeFor<DateTime>]
[GenerateShapeFor<Extension>]
[GenerateShapeFor<decimal>]
[GenerateShapeFor<BigInteger>]
[GenerateShapeFor<Guid>]
#if NET
[GenerateShapeFor<Int128>]
[GenerateShapeFor<UInt128>]
#endif
internal partial class MsgPackPrimitivesWitness;
