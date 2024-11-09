// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <inheritdoc cref="ByValueEqualityComparer{T, TProvider}"/>
public static class ByValueEqualityComparer<T>
	where T : IShapeable<T>
{
	/// <inheritdoc cref="ByValueEqualityComparer{T, TProvider}.Default"/>
	public static IEqualityComparer<T> Default => ByValueEqualityComparer<T, T>.Default;

	/// <inheritdoc cref="ByValueEqualityComparer{T, TProvider}.HashResistant"/>
	public static IEqualityComparer<T> HashResistant => ByValueEqualityComparer<T, T>.HashResistant;
}
