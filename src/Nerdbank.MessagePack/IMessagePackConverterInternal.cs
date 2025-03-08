// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Non-generic access to internal methods of <see cref="MessagePackConverter{T}"/>.
/// </summary>
internal interface IMessagePackConverterInternal
{
	/// <summary>
	/// Wraps this converter with a reference preservation converter.
	/// </summary>
	/// <returns>A converter. Possibly <see langword="this"/> if this instance is already reference preserving.</returns>
	MessagePackConverter WrapWithReferencePreservation();

	/// <summary>
	/// Removes the outer reference preserving converter, if present.
	/// </summary>
	/// <returns>The unwrapped converter.</returns>
	MessagePackConverter UnwrapReferencePreservation();
}
