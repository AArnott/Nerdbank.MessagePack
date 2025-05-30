// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Switches that determine whether specific features are enabled (for trimming purposes).
/// </summary>
internal static class Features
{
	/// <summary>
	/// Gets a value indicating whether converters for System.Text.Json types are built-in.
	/// </summary>
	[FeatureSwitchDefinition("Feature.MessagePack.SystemTextJsonConverters")]
	internal static bool SystemTextJsonConverters => AppContext.TryGetSwitch("Feature.MessagePack.SystemTextJsonConverters", out bool isEnabled) ? isEnabled : true;
}
