// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Vogen;

namespace VogenDataTypes;

[ValueObject<int>]
public partial struct CustomerId;

public record Customer
{
    public required CustomerId Id { get; set; }

    public required string Name { get; set; }
}
