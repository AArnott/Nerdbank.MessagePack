// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Resolves a custom identifier for a case type within a particular C# union.
/// </summary>
/// <param name="unionType">The containing union type.</param>
/// <param name="caseType">The payload type of the union case.</param>
/// <returns>The custom identifier, or <see langword="null"/> to use the normal identifier.</returns>
public delegate DerivedTypeIdentifier? UnionCaseAliasResolver(Type unionType, Type caseType);
