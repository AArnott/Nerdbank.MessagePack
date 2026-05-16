// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Security settings that may be applied to serialization.
/// </summary>
/// <remarks>
/// Applications <em>may</em> derive from this class to add additional settings
/// that its custom converters may honor.
/// Added settings should have secure defaults.
/// </remarks>
public record class SecuritySettings
{
	/// <summary>
	/// Default settings to use when (de)serializing untrusted data.
	/// </summary>
	/// <remarks>
	/// This value is optimized for security when processing untrusted data.
	/// </remarks>
	public static readonly SecuritySettings UntrustedData = new();

	/// <summary>
	/// Default settings to use with trusted data.
	/// </summary>
	/// <remarks>
	/// This value is optimized for high performance assuming the data is trustworthy, and should not be used with untrusted data.
	/// </remarks>
	public static readonly SecuritySettings TrustedData = new()
	{
		MaxCollectionPreallocation = Array.MaxLength,
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="SecuritySettings"/> class
	/// with secure defaults (those matching the values found in <see cref="UntrustedData"/>).
	/// </summary>
	public SecuritySettings()
	{
		this.MaxCollectionPreallocation = 4096;
	}

	/// <summary>
	/// Gets the largest capacity that a collection should be precreated with during deserialization.
	/// </summary>
	/// <remarks>
	/// Collections are allowed to grow to any size during deserialization regardless of this value.
	/// This value influences the initial capacity of collections created during deserialization,
	/// which can help mitigate DoS attacks that attempt to cause excessive memory allocations using only small payloads.
	/// </remarks>
	public int MaxCollectionPreallocation
	{
		get => field;
		init
		{
			Requires.Range(value > 0, nameof(value), "Value must be positive.");
			field = value;
		}
	}
}
