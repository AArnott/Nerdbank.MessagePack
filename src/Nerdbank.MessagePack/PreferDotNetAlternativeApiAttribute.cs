// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Indicates that the decorated type or member has a .NET alternative that is preferred over the decorated API.
/// </summary>
/// <param name="advice">The message for the diagnostic to be created.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
#pragma warning disable CS9113 // Parameter is unread.
internal class PreferDotNetAlternativeApiAttribute(string advice) : Attribute;
#pragma warning restore CS9113 // Parameter is unread.
