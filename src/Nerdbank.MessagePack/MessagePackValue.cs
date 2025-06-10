// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Nerdbank.MessagePack.SecureHash;

namespace Nerdbank.MessagePack;

/// <summary>
/// A variant type that can represent any of the MessagePack primitives.
/// </summary>
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
[GenerateShape]
public readonly partial struct MessagePackValue : IEquatable<MessagePackValue>, IStructuralSecureEqualityComparer<MessagePackValue>
{
	/// <summary>
	/// A token that represents Nil.
	/// </summary>
	public static readonly MessagePackValue Nil = new((string?)null);

	private static readonly CollisionResistantHasherUnmanaged<ulong> ULongSecureHasher = new();

	private readonly MessagePackValueKind kind;

	private readonly ValueTypeVariant value;

	/// <summary>
	/// A string or a dictionary or an array.
	/// </summary>
	private readonly object? refValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="byte" /> value.
	/// </summary>
	/// <param name="value">The <see cref="byte" /> value to initialize with.</param>
	public MessagePackValue(byte value)
	{
		this.kind = MessagePackValueKind.UnsignedInteger;
		this.value.Integer = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an <see cref="sbyte" /> value.
	/// </summary>
	/// <param name="value">The <see cref="sbyte" /> value to initialize with.</param>
	public MessagePackValue(sbyte value)
	{
		this.kind = value < 0 ? MessagePackValueKind.SignedInteger : MessagePackValueKind.UnsignedInteger;
		this.value.Integer = unchecked((ulong)value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an <see cref="ushort"/> value.
	/// </summary>
	/// <param name="value">The <see cref="ushort"/> value to initialize with.</param>
	public MessagePackValue(ushort value)
	{
		this.kind = MessagePackValueKind.UnsignedInteger;
		this.value.Integer = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="short"/> value.
	/// </summary>
	/// <param name="value">The <see cref="short"/> value to initialize with.</param>
	public MessagePackValue(short value)
	{
		this.kind = value < 0 ? MessagePackValueKind.SignedInteger : MessagePackValueKind.UnsignedInteger;
		this.value.Integer = unchecked((ulong)value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="uint"/> value.
	/// </summary>
	/// <param name="value">The <see cref="uint"/> value to initialize with.</param>
	public MessagePackValue(uint value)
	{
		this.kind = MessagePackValueKind.UnsignedInteger;
		this.value.Integer = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an <see cref="int"/> value.
	/// </summary>
	/// <param name="value">The <see cref="int"/> value to initialize with.</param>
	public MessagePackValue(int value)
	{
		this.kind = value < 0 ? MessagePackValueKind.SignedInteger : MessagePackValueKind.UnsignedInteger;
		this.value.Integer = unchecked((ulong)value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="ulong"/> value.
	/// </summary>
	/// <param name="value">The <see cref="ulong"/> value to initialize with.</param>
	public MessagePackValue(ulong value)
	{
		this.kind = MessagePackValueKind.UnsignedInteger;
		this.value.Integer = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="long"/> value.
	/// </summary>
	/// <param name="value">The <see cref="long"/> value to initialize with.</param>
	public MessagePackValue(long value)
	{
		this.kind = value < 0 ? MessagePackValueKind.SignedInteger : MessagePackValueKind.UnsignedInteger;
		this.value.Integer = unchecked((ulong)value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="bool" /> value.
	/// </summary>
	/// <param name="value">The <see cref="bool" /> value to initialize with.</param>
	public MessagePackValue(bool value)
	{
		this.kind = MessagePackValueKind.Boolean;
		this.value.Boolean = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="float"/> value.
	/// </summary>
	/// <param name="value">The <see cref="float"/> value to initialize with.</param>
	public MessagePackValue(float value)
	{
		this.kind = MessagePackValueKind.Single;
		this.value.Float32 = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="double"/> value.
	/// </summary>
	/// <param name="value">The <see cref="double"/> value to initialize with.</param>
	public MessagePackValue(double value)
	{
		this.kind = MessagePackValueKind.Double;
		this.value.Float64 = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a <see cref="string"/> or <see langword="null"/> value.
	/// </summary>
	/// <param name="value">The <see cref="string"/> value to initialize with.</param>
	[OverloadResolutionPriority(10)] // avoid ambiguity for null values
	public MessagePackValue(string? value)
	{
		this.kind = value is null ? MessagePackValueKind.Nil : MessagePackValueKind.String;
		this.refValue = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with binary data.
	/// </summary>
	/// <param name="value">The data to initialize with.</param>
	public MessagePackValue(byte[] value)
	{
		this.kind = MessagePackValueKind.Binary;
		this.refValue = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an array.
	/// </summary>
	/// <param name="value">The array to initialize with.</param>
	public MessagePackValue(MessagePackValue[] value)
	{
		this.kind = MessagePackValueKind.Array;
		this.refValue = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with a map.
	/// </summary>
	/// <param name="value">The map to initialize with.</param>
	public MessagePackValue(IReadOnlyDictionary<MessagePackValue, MessagePackValue> value)
	{
		this.kind = MessagePackValueKind.Map;
		this.refValue = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an <see cref="Extension"/>.
	/// </summary>
	/// <param name="value">The extension to initialize with.</param>
	public MessagePackValue(Extension value)
	{
		this.kind = MessagePackValueKind.Extension;

		// We choose to box the value, because it's rare and the Extension struct is large.
		// But we *could* compress it into the 8 bytes allowed for value types
		// if the extension's data length is 7 bytes or shorter, which may be a common case.
		// In fact we could even capture data lengths as long as 7+7 due to memory alignment
		// and our 1-byte 'kind' enum.
		// When reading this out, we would first check for a boxed Extension in refValue,
		// and if it is null then we know we encoded it into our value fields.
		this.refValue = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackValue"/> struct with an <see cref="DateTime"/>.
	/// </summary>
	/// <param name="value">The value to initialize with.</param>
	public MessagePackValue(DateTime value)
	{
		this.kind = MessagePackValueKind.DateTime;
		this.value.DateTime = value;
	}

	/// <summary>
	/// Gets the type of the MessagePack token.
	/// </summary>
	public MessagePackValueKind Kind => this.kind;

	/// <summary>
	/// Gets the value as a <see cref="byte"/>.
	/// </summary>
	/// <inheritdoc cref="UnsignedInteger" path="/exception"/>
	public byte ValueAsByte => checked((byte)this.UnsignedInteger);

	/// <summary>
	/// Gets the value as an <see cref="sbyte"/>.
	/// </summary>
	/// <inheritdoc cref="SignedInteger" path="/exception"/>
	public sbyte ValueAsSByte => checked((sbyte)this.SignedInteger);

	/// <summary>
	/// Gets the value as an <see cref="ushort"/>.
	/// </summary>
	/// <inheritdoc cref="UnsignedInteger" path="/exception"/>
	public ushort ValueAsUInt16 => checked((ushort)this.UnsignedInteger);

	/// <summary>
	/// Gets the value as a <see cref="short"/>.
	/// </summary>
	/// <inheritdoc cref="SignedInteger" path="/exception"/>
	public short ValueAsInt16 => checked((short)this.SignedInteger);

	/// <summary>
	/// Gets the value as a <see cref="uint"/>.
	/// </summary>
	/// <inheritdoc cref="UnsignedInteger" path="/exception"/>
	public uint ValueAsUInt32 => checked((uint)this.UnsignedInteger);

	/// <summary>
	/// Gets the value as an <see cref="int"/>.
	/// </summary>
	/// <inheritdoc cref="SignedInteger" path="/exception"/>
	public int ValueAsInt32 => checked((int)this.SignedInteger);

	/// <summary>
	/// Gets the value as a <see cref="ulong"/>.
	/// </summary>
	/// <inheritdoc cref="UnsignedInteger" path="/exception"/>
	public ulong ValueAsUInt64 => this.UnsignedInteger;

	/// <summary>
	/// Gets the value as a <see cref="long"/>.
	/// </summary>
	/// <inheritdoc cref="SignedInteger" path="/exception"/>
	public long ValueAsInt64 => this.SignedInteger;

	/// <summary>
	/// Gets the value as a <see cref="bool"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not a <see cref="bool"/>.</exception>
#pragma warning disable SA1623 // Property summary documentation should match accessors
	public bool ValueAsBoolean => this.kind == MessagePackValueKind.Boolean ? this.value.Boolean : this.ThrowInvalidCastException<bool>();
#pragma warning restore SA1623 // Property summary documentation should match accessors

	/// <summary>
	/// Gets the value as a <see cref="float"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not a floating point number.</exception>
	public float ValueAsSingle => this.kind switch
	{
		MessagePackValueKind.Single => this.value.Float32,
		MessagePackValueKind.Double => checked((float)this.value.Float64),
		MessagePackValueKind.SignedInteger => this.SignedInteger,
		MessagePackValueKind.UnsignedInteger => this.UnsignedInteger,
		_ => this.ThrowInvalidCastException<float>(),
	};

	/// <summary>
	/// Gets the value as a <see cref="double"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not a floating point number.</exception>
	public double ValueAsDouble => this.kind switch
	{
		MessagePackValueKind.Single => this.value.Float32,
		MessagePackValueKind.Double => this.value.Float64,
		MessagePackValueKind.SignedInteger => this.SignedInteger,
		MessagePackValueKind.UnsignedInteger => this.UnsignedInteger,
		_ => this.ThrowInvalidCastException<double>(),
	};

	/// <summary>
	/// Gets the value as a <see cref="string"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not a <see cref="string"/> or a <see langword="null"/>.</exception>
	public string? ValueAsString => this.kind switch
	{
		MessagePackValueKind.String => (string?)this.refValue,
		MessagePackValueKind.Nil => null,
		_ => this.ThrowInvalidCastException<string>(),
	};

	/// <summary>
	/// Gets the value as a <see cref="ReadOnlyMemory{T}"/> of <see cref="byte"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not binary data.</exception>
	public ReadOnlyMemory<byte> ValueAsBinary => this.kind == MessagePackValueKind.Binary ? (byte[])this.refValue! : this.ThrowInvalidCastException<ReadOnlyMemory<byte>>();

	/// <summary>
	/// Gets the value as an array.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an array.</exception>
	public ReadOnlyMemory<MessagePackValue> ValueAsArray => this.kind == MessagePackValueKind.Array ? (MessagePackValue[])this.refValue! : this.ThrowInvalidCastException<ReadOnlyMemory<MessagePackValue>>();

	/// <summary>
	/// Gets the value as an map.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an map or a <see langword="null"/>.</exception>
	public IReadOnlyDictionary<MessagePackValue, MessagePackValue> ValueAsMap => this.kind == MessagePackValueKind.Map ? (IReadOnlyDictionary<MessagePackValue, MessagePackValue>)this.refValue! : this.ThrowInvalidCastException<IReadOnlyDictionary<MessagePackValue, MessagePackValue>>();

	/// <summary>
	/// Gets the value as an <see cref="Extension"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an <see cref="Extension"/>.</exception>
	public Extension ValueAsExtension => this.kind == MessagePackValueKind.Extension ? (Extension)this.refValue! : this.ThrowInvalidCastException<Extension>();

	/// <summary>
	/// Gets the value as an <see cref="DateTime"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an <see cref="DateTime"/>.</exception>
	public DateTime ValueAsDateTime => this.kind == MessagePackValueKind.DateTime ? this.value.DateTime : this.ThrowInvalidCastException<DateTime>();

	/// <summary>
	/// Gets the signed integer value.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an integer.</exception>
	/// <exception cref="OverflowException">Thrown when the value cannot be represented in an integer of the required length.</exception>
	private long SignedInteger => this.kind switch
	{
		MessagePackValueKind.SignedInteger => unchecked((long)this.value.Integer),
		MessagePackValueKind.UnsignedInteger => checked((long)this.value.Integer),
		_ => this.ThrowInvalidCastException<long>(),
	};

	/// <summary>
	/// Gets the unsigned integer value.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when the value is not an integer.</exception>
	/// <exception cref="OverflowException">Thrown when the value cannot be represented in an integer of the required length.</exception>
	private ulong UnsignedInteger => this.kind switch
	{
		MessagePackValueKind.UnsignedInteger => this.value.Integer,
		MessagePackValueKind.SignedInteger => checked((ulong)unchecked((long)this.value.Integer)),
		_ => this.ThrowInvalidCastException<ulong>(),
	};

	private string DebuggerDisplay => this.ToString();

	/// <summary>
	/// Implicitly converts a <see cref="byte" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="byte" /> value to convert.</param>
	public static implicit operator MessagePackValue(byte value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="byte"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <exception cref="InvalidCastException">Thrown when the value is not an unsigned integer.</exception>
	/// <inheritdoc cref="ValueAsByte" path="/exception"/>
	public static explicit operator byte(MessagePackValue token) => token.ValueAsByte;

	/// <summary>
	/// Implicitly converts an <see cref="sbyte" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="sbyte" /> value to convert.</param>
	public static implicit operator MessagePackValue(sbyte value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to an <see cref="sbyte"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsSByte" path="/exception"/>
	public static explicit operator sbyte(MessagePackValue token) => token.ValueAsSByte;

	/// <summary>
	/// Implicitly converts a <see cref="ushort" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="ushort" /> value to convert.</param>
	public static implicit operator MessagePackValue(ushort value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="ushort"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsUInt16" path="/exception"/>
	public static explicit operator ushort(MessagePackValue token) => token.ValueAsUInt16;

	/// <summary>
	/// Implicitly converts a <see cref="short" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="short" /> value to convert.</param>
	public static implicit operator MessagePackValue(short value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="short"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsInt16" path="/exception"/>
	public static explicit operator short(MessagePackValue token) => token.ValueAsInt16;

	/// <summary>
	/// Implicitly converts a <see cref="uint" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="uint" /> value to convert.</param>
	public static implicit operator MessagePackValue(uint value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="uint"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsUInt32" path="/exception"/>
	public static explicit operator uint(MessagePackValue token) => token.ValueAsUInt32;

	/// <summary>
	/// Implicitly converts an <see cref="int" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="int" /> value to convert.</param>
	public static implicit operator MessagePackValue(int value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to an <see cref="int"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsInt32" path="/exception"/>
	public static explicit operator int(MessagePackValue token) => token.ValueAsInt32;

	/// <summary>
	/// Implicitly converts a <see cref="ulong" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="ulong" /> value to convert.</param>
	public static implicit operator MessagePackValue(ulong value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="ulong"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsUInt64" path="/exception"/>
	public static explicit operator ulong(MessagePackValue token) => token.ValueAsUInt64;

	/// <summary>
	/// Implicitly converts a <see cref="long" /> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="long" /> value to convert.</param>
	public static implicit operator MessagePackValue(long value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="long"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsInt64" path="/exception"/>
	public static explicit operator long(MessagePackValue token) => token.ValueAsInt64;

	/// <summary>
	/// Implicitly converts a <see cref="bool"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The boolean value to convert.</param>
	public static implicit operator MessagePackValue(bool value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="bool"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsBoolean" path="/exception"/>
	public static explicit operator bool(MessagePackValue token) => token.ValueAsBoolean;

	/// <summary>
	/// Implicitly converts a <see cref="float"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="float"/> value to convert.</param>
	public static implicit operator MessagePackValue(float value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="float"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsSingle" path="/exception"/>
	public static explicit operator float(MessagePackValue token) => token.ValueAsSingle;

	/// <summary>
	/// Implicitly converts a <see cref="double"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="double"/> value to convert.</param>
	public static implicit operator MessagePackValue(double value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="double"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsDouble" path="/exception"/>
	public static explicit operator double(MessagePackValue token) => token.ValueAsDouble;

	/// <summary>
	/// Implicitly converts a nullable <see cref="string"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The nullable <see cref="string"/> value to convert.</param>
	public static implicit operator MessagePackValue(string? value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a nullable <see cref="string"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsString" path="/exception"/>
	public static explicit operator string?(MessagePackValue token) => token.ValueAsString;

	/// <summary>
	/// Implicitly converts binary data to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The binary data value to convert.</param>
	public static implicit operator MessagePackValue(byte[]? value) => value is null ? Nil : new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a nullable <see cref="string"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsString" path="/exception"/>
	public static explicit operator ReadOnlyMemory<byte>(MessagePackValue token) => token.ValueAsBinary;

	/// <summary>
	/// Implicitly converts an array to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The array to convert.</param>
	public static implicit operator MessagePackValue(MessagePackValue[] value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to an array.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsArray" path="/exception"/>
	public static explicit operator ReadOnlyMemory<MessagePackValue>(MessagePackValue token) => token.ValueAsArray;

	/// <summary>
	/// Implicitly converts a <see cref="FrozenDictionary{TKey, TValue}"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The map to convert.</param>
	public static implicit operator MessagePackValue(FrozenDictionary<MessagePackValue, MessagePackValue> value) => new(value);

	/// <summary>
	/// Implicitly converts a <see cref="Dictionary{TKey, TValue}"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The map to convert.</param>
	public static implicit operator MessagePackValue(Dictionary<MessagePackValue, MessagePackValue> value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="FrozenDictionary{TKey, TValue}"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsMap" path="/exception"/>
	public static explicit operator FrozenDictionary<MessagePackValue, MessagePackValue>(MessagePackValue token) => (FrozenDictionary<MessagePackValue, MessagePackValue>)token.ValueAsMap;

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to a <see cref="Dictionary{TKey, TValue}"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="ValueAsMap" path="/exception"/>
	public static explicit operator Dictionary<MessagePackValue, MessagePackValue>(MessagePackValue token) => (Dictionary<MessagePackValue, MessagePackValue>)token.ValueAsMap;

	/// <summary>
	/// Implicitly converts an <see cref="Extension"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="Extension"/> to convert.</param>
	public static implicit operator MessagePackValue(Extension value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to an <see cref="Extension"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="Extension" path="/exception"/>
	public static explicit operator Extension(MessagePackValue token) => token.ValueAsExtension;

	/// <summary>
	/// Implicitly converts an <see cref="DateTime"/> to a <see cref="MessagePackValue"/>.
	/// </summary>
	/// <param name="value">The <see cref="DateTime"/> to convert.</param>
	public static implicit operator MessagePackValue(DateTime value) => new(value);

	/// <summary>
	/// Explicitly converts a <see cref="MessagePackValue"/> to an <see cref="DateTime"/>.
	/// </summary>
	/// <param name="token">The <see cref="MessagePackValue"/> to convert.</param>
	/// <inheritdoc cref="DateTime" path="/exception"/>
	public static explicit operator DateTime(MessagePackValue token) => token.ValueAsDateTime;

	/// <inheritdoc/>
	public override string ToString()
		=> this.kind switch
		{
			MessagePackValueKind.Nil => "null",
			MessagePackValueKind.SignedInteger => this.SignedInteger.ToString(CultureInfo.InvariantCulture),
			MessagePackValueKind.UnsignedInteger => this.UnsignedInteger.ToString(CultureInfo.InvariantCulture),
			MessagePackValueKind.Boolean => this.value.Boolean ? "true" : "false",
			MessagePackValueKind.Single => this.value.Float32.ToString("R", CultureInfo.InvariantCulture),
			MessagePackValueKind.Double => this.value.Float64.ToString("R", CultureInfo.InvariantCulture),
			MessagePackValueKind.String => (string)this.refValue!,
			MessagePackValueKind.Binary => $"byte[{((byte[])this.refValue!).Length}]",
			MessagePackValueKind.Array => $"array[{((MessagePackValue[])this.refValue!).Length}]",
			MessagePackValueKind.Map => $"map[{((IReadOnlyDictionary<MessagePackValue, MessagePackValue>)this.refValue!).Count}]",
			MessagePackValueKind.Extension => $"extension type {((Extension)this.refValue!).TypeCode}",
			MessagePackValueKind.DateTime => this.value.DateTime.ToString(CultureInfo.InvariantCulture),
			_ => throw new NotImplementedException(),
		};

	/// <inheritdoc/>
	public bool Equals(MessagePackValue other) => this.Equals(other, structural: false);

	/// <inheritdoc/>
	public bool StructuralEquals(MessagePackValue other) => this.Equals(other, structural: true);

	/// <inheritdoc/>
	public override bool Equals([NotNullWhen(true)] object? obj) => obj is MessagePackValue other && this.Equals(other);

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		HashCode hashCode = default;
		hashCode.Add((byte)this.kind);
		switch (this.kind)
		{
			case MessagePackValueKind.Nil:
				break;
			case MessagePackValueKind.UnsignedInteger or MessagePackValueKind.SignedInteger:
				hashCode.Add(this.value.Integer);
				break;
			case MessagePackValueKind.Boolean:
				hashCode.Add(this.value.Boolean);
				break;
			case MessagePackValueKind.Single:
				hashCode.Add(this.value.Float32);
				break;
			case MessagePackValueKind.Double:
				hashCode.Add(this.value.Float64);
				break;
			case MessagePackValueKind.String:
				hashCode.Add((string?)this.refValue);
				break;
			case MessagePackValueKind.Binary:
				hashCode.Add((byte[])this.refValue!);
				break;
			case MessagePackValueKind.Array:
				hashCode.Add((MessagePackValue[])this.refValue!);
				break;
			case MessagePackValueKind.Map:
				hashCode.Add((IReadOnlyDictionary<MessagePackValue, MessagePackValue>)this.refValue!);
				break;
			case MessagePackValueKind.Extension:
				hashCode.Add((Extension)this.refValue!);
				break;
			case MessagePackValueKind.DateTime:
				hashCode.Add(this.value.DateTime);
				break;
			default:
				throw new NotImplementedException();
		}

		return hashCode.ToHashCode();
	}

	/// <inheritdoc/>
	long IStructuralSecureEqualityComparer<MessagePackValue>.GetSecureHashCode() => this.GetSecureHashCode();

	private long GetSecureHashCode()
	{
		return (byte)this.kind + this.kind switch
		{
			MessagePackValueKind.Nil => 0,
			MessagePackValueKind.UnsignedInteger or MessagePackValueKind.SignedInteger => ULongSecureHasher.GetSecureHashCode(this.value.Integer),
			MessagePackValueKind.Boolean => HashCollisionResistantPrimitives.BooleanEqualityComparer.Instance.GetSecureHashCode(this.value.Boolean),
			MessagePackValueKind.Single => HashCollisionResistantPrimitives.SingleEqualityComparer.Instance.GetSecureHashCode(this.value.Float32),
			MessagePackValueKind.Double => HashCollisionResistantPrimitives.DoubleEqualityComparer.Instance.GetSecureHashCode(this.value.Float64),
			MessagePackValueKind.String => HashCollisionResistantPrimitives.StringEqualityComparer.Instance.GetSecureHashCode((string?)this.refValue),
			MessagePackValueKind.Binary => HashCollisionResistantPrimitives.ByteArrayEqualityComparer.Default.GetSecureHashCode((byte[])this.refValue!),
			MessagePackValueKind.Array => UncheckedArraySum(this),
			MessagePackValueKind.Map => UncheckedMapSum(this),
			MessagePackValueKind.Extension => ((IStructuralSecureEqualityComparer<Extension>)this.refValue!).GetSecureHashCode(),
			MessagePackValueKind.DateTime => HashCollisionResistantPrimitives.DateTimeEqualityComparer.Instance.GetSecureHashCode(this.value.DateTime),
			_ => throw new NotImplementedException(),
		};

		static long UncheckedArraySum(in MessagePackValue self)
		{
			long sum = 0;
			foreach (MessagePackValue element in (MessagePackValue[])self.refValue!)
			{
				sum = unchecked(sum + element.GetSecureHashCode());
			}

			return sum;
		}

		static long UncheckedMapSum(in MessagePackValue self)
		{
			long sum = 0;
			foreach (KeyValuePair<MessagePackValue, MessagePackValue> pair in (IReadOnlyDictionary<MessagePackValue, MessagePackValue>)self.refValue!)
			{
				sum = unchecked(sum + pair.Key.GetSecureHashCode() + pair.Value.GetSecureHashCode());
			}

			return sum;
		}
	}

	private bool Equals(MessagePackValue other, bool structural)
	{
		if (this.kind != other.kind)
		{
			return false;
		}

		return this.kind switch
		{
			MessagePackValueKind.Nil => true,
			MessagePackValueKind.UnsignedInteger or MessagePackValueKind.SignedInteger => this.value.Integer == other.value.Integer,
			MessagePackValueKind.Boolean => this.value.Boolean == other.value.Boolean,
			MessagePackValueKind.Single => this.value.Float32 == other.value.Float32,
			MessagePackValueKind.Double => this.value.Float64 == other.value.Float64,
			MessagePackValueKind.String => (string?)this.refValue == (string?)other.refValue,
			MessagePackValueKind.Binary => ((byte[])this.refValue!).SequenceEqual((byte[])other.refValue!),
			MessagePackValueKind.Array => this.refValue == other.refValue || (structural && ((MessagePackValue[])this.refValue!).SequenceEqual((MessagePackValue[])other.refValue!)),
			MessagePackValueKind.Map => this.refValue == other.refValue || (structural && MapEquals((IReadOnlyDictionary<MessagePackValue, MessagePackValue>?)this.refValue, (IReadOnlyDictionary<MessagePackValue, MessagePackValue>?)other.refValue)),
			MessagePackValueKind.Extension => ((Extension)this.refValue!).Equals((Extension)other.refValue!),
			MessagePackValueKind.DateTime => this.value.DateTime != other.value.DateTime,
			_ => throw new NotImplementedException(),
		};

		static bool MapEquals<TKey, TValue>(IReadOnlyDictionary<TKey, TValue>? left, IReadOnlyDictionary<TKey, TValue>? right)
		{
			if (left is null || right is null)
			{
				return false;
			}

			if (left.Count != right.Count)
			{
				return false;
			}

			EqualityComparer<TValue> valueEq = EqualityComparer<TValue>.Default;
			foreach (KeyValuePair<TKey, TValue> item in left)
			{
				if (!right.TryGetValue(item.Key, out TValue? rightValue) || !valueEq.Equals(item.Value, rightValue))
				{
					return false;
				}
			}

			return true;
		}
	}

	[DoesNotReturn]
	private T ThrowInvalidCastException<T>() => throw new InvalidCastException($"Cannot cast a {this.Kind} to a {typeof(T)}.");

	[StructLayout(LayoutKind.Explicit)]
	private struct ValueTypeVariant
	{
		[FieldOffset(0)]
		internal ulong Integer;

		[FieldOffset(0)]
		internal bool Boolean;

		[FieldOffset(0)]
		internal float Float32;

		[FieldOffset(0)]
		internal double Float64;

		[FieldOffset(0)]
		internal DateTime DateTime;
	}
}
