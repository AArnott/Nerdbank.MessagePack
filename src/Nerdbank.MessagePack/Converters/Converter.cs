// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.Converters;

public abstract class Converter
{
	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="WriteObjectAsync"/> and/or <see cref="ReadObjectAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public abstract bool PreferAsyncSerialization { get; }
}
