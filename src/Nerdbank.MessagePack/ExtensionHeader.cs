// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Describes a msgpack extension. This precedes the extension payload itself in the msgpack encoded format.
/// </summary>
/// <param name="TypeCode">A value that uniquely identifies the extension type. Negative values are reserved for official msgpack extensions. See <see cref="ReservedMessagePackExtensionTypeCode"/> for values already assigned from the reserved range.</param>
/// <param name="Length">The length of the extension's data payload.</param>
public record struct ExtensionHeader(sbyte TypeCode, uint Length);
