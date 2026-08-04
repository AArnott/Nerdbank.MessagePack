// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Implemented by <see cref="MessagePackConverter{T}"/>-derived types that support preserving
/// an unrecognized union discriminator on an object that declares an <see cref="UnusedDataPacket"/> member.
/// </summary>
/// <typeparam name="T">The object type.</typeparam>
internal interface IUnknownUnionCaseFallback<T>
{
	/// <summary>
	/// Gets a value indicating whether the converter can preserve an unrecognized union case.
	/// </summary>
	bool CanPreserveUnknownUnionCase { get; }

	/// <summary>
	/// Associates an unrecognized union discriminator with a deserialized value.
	/// </summary>
	/// <param name="value">The deserialized value.</param>
	/// <param name="discriminator">The unrecognized discriminator.</param>
	void SetUnknownUnionDiscriminator(ref T value, in RawMessagePack discriminator);

	/// <summary>
	/// Gets the preserved discriminator for an unrecognized union case.
	/// </summary>
	/// <param name="value">The value that may have been deserialized from an unrecognized union case.</param>
	/// <param name="discriminator">Receives the preserved discriminator.</param>
	/// <returns><see langword="true"/> if a discriminator was preserved; otherwise, <see langword="false"/>.</returns>
	bool TryGetUnknownUnionDiscriminator(in T value, out RawMessagePack discriminator);
}
