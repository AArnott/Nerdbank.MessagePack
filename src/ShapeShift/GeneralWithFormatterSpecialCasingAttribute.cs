// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift;

/// <summary>
/// An attribute that may appear on a type that is of general applicability
/// in serialization, but may contain special casing for a specific formatter.
/// </summary>
/// <remarks>
/// This attribute prevents the type from being flagged for disallowed dependencies
/// when running architecture enforcement tests.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
internal class GeneralWithFormatterSpecialCasingAttribute : Attribute;
