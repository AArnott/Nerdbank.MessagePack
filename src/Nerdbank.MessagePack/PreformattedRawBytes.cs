﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift;

/// <summary>
/// Represents a sequence of raw msgpack bytes.
/// </summary>
/// <remarks>
/// <para>
/// This struct is useful as the type for some field or property that should be serialize or deserialized separately from its surrounding data.
/// For example an RPC protocol may have an envelope around user data, such that the envelope and the user data should have distinct serialization rules.
/// The envelope could use this <see cref="PreformattedRawBytes"/> in order to facilitate this by allowing pre-serialization and deferred deserialization of user data.
/// </para>
/// <para>
/// The <see cref="Converter{T}"/> for this type will always copy the memory from the buffers being read so that this struct has
/// an independent lifetime.
/// </para>
/// </remarks>
[Converter(typeof(PreformattedRawBytesConverter))]
public struct PreformattedRawBytes : IEquatable<PreformattedRawBytes>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PreformattedRawBytes"/> struct.
	/// </summary>
	/// <param name="msgpack">The actual sequence of msgpack bytes.</param>
	public PreformattedRawBytes(ReadOnlySequence<byte> msgpack)
	{
		this.MsgPack = msgpack;
	}

	/// <summary>
	/// Gets a sequence of raw MessagePack bytes.
	/// </summary>
	public ReadOnlySequence<byte> MsgPack { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="MsgPack"/> is owned by this instance,
	/// and thus can be relied on to be immutable and not recycled by others.
	/// </summary>
	public bool IsOwned { get; internal set; }

	/// <summary>
	/// Explicitly converts a <see cref="ReadOnlySequence{T}"/> of bytes to a <see cref="PreformattedRawBytes"/>.
	/// </summary>
	/// <param name="msgpack">The sequence of MessagePack bytes.</param>
	/// <returns>A new instance of <see cref="PreformattedRawBytes"/> containing the provided bytes.</returns>
	public static explicit operator PreformattedRawBytes(ReadOnlySequence<byte> msgpack) => new(msgpack);

	/// <summary>
	/// Explicitly converts a <see cref="ReadOnlyMemory{T}"/> of bytes to a <see cref="PreformattedRawBytes"/>.
	/// </summary>
	/// <param name="msgpack">The memory containing MessagePack bytes.</param>
	/// <returns>A new instance of <see cref="PreformattedRawBytes"/> containing the provided bytes.</returns>
	public static explicit operator PreformattedRawBytes(ReadOnlyMemory<byte> msgpack) => new(new ReadOnlySequence<byte>(msgpack));

	/// <summary>
	/// Explicitly converts a <see cref="Memory{T}"/> of bytes to a <see cref="PreformattedRawBytes"/>.
	/// </summary>
	/// <param name="msgpack">The memory containing MessagePack bytes.</param>
	/// <returns>A new instance of <see cref="PreformattedRawBytes"/> containing the provided bytes.</returns>
	public static explicit operator PreformattedRawBytes(Memory<byte> msgpack) => new(new ReadOnlySequence<byte>(msgpack));

	/// <summary>
	/// Explicitly converts an array of bytes to a <see cref="PreformattedRawBytes"/>.
	/// </summary>
	/// <param name="msgpack">The memory containing MessagePack bytes.</param>
	/// <returns>A new instance of <see cref="PreformattedRawBytes"/> containing the provided bytes.</returns>
	public static explicit operator PreformattedRawBytes(byte[] msgpack) => new(new ReadOnlySequence<byte>(msgpack));

	/// <summary>
	/// Implicitly converts a <see cref="PreformattedRawBytes"/> to a <see cref="ReadOnlySequence{T}"/> of bytes.
	/// </summary>
	/// <param name="msgpack">The <see cref="PreformattedRawBytes"/> instance.</param>
	/// <returns>The <see cref="ReadOnlySequence{T}"/> of bytes contained in the <see cref="PreformattedRawBytes"/>.</returns>
	public static implicit operator ReadOnlySequence<byte>(PreformattedRawBytes msgpack) => msgpack.MsgPack;

	/// <inheritdoc/>
	public readonly bool Equals(PreformattedRawBytes other) => this.MsgPack.SequenceEqual(other.MsgPack);

	/// <summary>
	/// Produces a self-sustaining copy of this struct that will outlive whatever the original source buffer was from which this was created.
	/// </summary>
	/// <returns>
	/// A copy of the data that is guaranteed to be immutable.
	/// When <see cref="IsOwned"/> is already <see langword="true" />, <see langword="this"/> will be returned instead of making a redundant copy.
	/// </returns>
	/// <remarks>
	/// <para>
	/// This is useful when its owner wants the data to live longer than the underlying buffers from which it was created.
	/// </para>
	/// <para>
	/// This struct mutates itself as well to retain possession of the cloned data, so that multiple calls to this method do not result in redundant copies.
	/// This will only be effective when the caller stores the struct in a mutable location.
	/// If this struct is stored in a <see langword="readonly" /> field for instance, the local copy cannot be mutated, though the returned copy
	/// will actually be self-owned.
	/// </para>
	/// </remarks>
	public PreformattedRawBytes ToOwned()
	{
		if (this.IsOwned)
		{
			return this;
		}

		this.MsgPack = this.MsgPack.Clone();
		this.IsOwned = true;

		return this;
	}
}
