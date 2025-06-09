// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack.SecureHash;

namespace Nerdbank.MessagePack;

/// <summary>
/// Describes a msgpack extension.
/// </summary>
/// <param name="TypeCode"><inheritdoc cref="ExtensionHeader(sbyte, uint)" path="/param[@name='TypeCode']"/></param>
/// <param name="Data">The data payload, in whatever format is prescribed by the extension as per the <paramref name="TypeCode"/>.</param>
public record struct Extension(sbyte TypeCode, ReadOnlySequence<byte> Data) : IDeepSecureEqualityComparer<Extension>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Extension"/> struct.
	/// </summary>
	/// <param name="typeCode"><inheritdoc cref="Extension(sbyte, ReadOnlySequence{byte})" path="/param[@name='TypeCode']"/></param>
	/// <param name="data"><inheritdoc cref="Extension(sbyte, ReadOnlySequence{byte})" path="/param[@name='Data']"/></param>
	public Extension(sbyte typeCode, ReadOnlyMemory<byte> data)
		: this(typeCode, new ReadOnlySequence<byte>(data))
	{
	}

	/// <summary>
	/// Gets the header for the extension that should precede the <see cref="Data"/> in the msgpack encoded format.
	/// </summary>
	public readonly ExtensionHeader Header => new(this.TypeCode, checked((uint)this.Data.Length));

	/// <inheritdoc/>
	public readonly bool Equals(Extension other) => this.TypeCode == other.TypeCode && this.Data.SequenceEqual(other.Data);

	/// <inheritdoc/>
	public override readonly int GetHashCode() => HashCode.Combine(this.TypeCode, this.Data.Length);

	/// <inheritdoc/>
	bool IDeepSecureEqualityComparer<Extension>.DeepEquals(Extension other) => this.Equals(other);

	/// <inheritdoc/>
	long IDeepSecureEqualityComparer<Extension>.GetSecureHashCode()
	{
		// We don't have an incremental SipHash implementation, so we have to copy the data to a rented buffer.
		byte[] rented = ArrayPool<byte>.Shared.Rent(checked((int)this.Data.Length));
		try
		{
			this.Data.CopyTo(rented);
			return SipHash.Default.Compute(rented.AsSpan(0, (int)this.Data.Length)) + this.TypeCode;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(rented);
		}
	}
}
