// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.MessagePack.Converters;

class MsgPackReferencePreservingManager : IReferencePreservingManager
{
	internal static readonly MsgPackReferencePreservingManager Instance = new();

	private MsgPackReferencePreservingManager() { }

	public Converter<T> UnwrapFromReferencePreservingConverter<T>(Converter<T> inner) => inner is ReferencePreservingConverter<T> converter ? converter.Inner : inner;

	public Converter<T> WrapWithReferencePreservingConverter<T>(Converter<T> inner) => inner is ReferencePreservingConverter<T> ? inner : new ReferencePreservingConverter<T>(inner);
}
