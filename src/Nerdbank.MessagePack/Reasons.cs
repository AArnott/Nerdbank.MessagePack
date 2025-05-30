// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Constant strings useful in <see cref="RequiresDynamicCodeAttribute"/>
/// and <see cref="RequiresUnreferencedCodeAttribute"/> annotations.
/// </summary>
internal static class Reasons
{
	/// <summary>
	/// A reference to <see cref="System.Dynamic.DynamicObject"/> is the reason for the dynamic code requirement.
	/// </summary>
	internal const string DynamicObject = "System.Dynamic.DynamicObject requires dynamic code.";
}
