// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.MessagePack.Converters;

/// <summary>
/// Gets a messagepack-specific <see cref="IReferencePreservingManager"/>.
/// </summary>
internal class MsgPackReferencePreservingManager : IReferencePreservingManager
{
	/// <summary>
	/// Gets the singleton instance.
	/// </summary>
	internal static readonly MsgPackReferencePreservingManager Instance = new();

	private MsgPackReferencePreservingManager()
	{
	}

	/// <inheritdoc/>
	public Converter<T> UnwrapFromReferencePreservingConverter<T>(Converter<T> inner) => inner is ReferencePreservingConverter<T> converter ? converter.Inner : inner;

	/// <inheritdoc/>
	public Converter<T> WrapWithReferencePreservingConverter<T>(Converter<T> inner) => inner is ReferencePreservingConverter<T> || typeof(T).IsValueType ? inner : new ReferencePreservingConverter<T>(inner);
}
