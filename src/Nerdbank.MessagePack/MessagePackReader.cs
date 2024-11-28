// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file was originally derived from https://github.com/MessagePack-CSharp/MessagePack-CSharp/
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nerdbank.MessagePack;

/// <summary>
/// A primitive types reader for the MessagePack format.
/// </summary>
/// <remarks>
/// <see href="https://github.com/msgpack/msgpack/blob/master/spec.md">The MessagePack spec.</see>.
/// </remarks>
/// <exception cref="MessagePackSerializationException">Thrown when reading methods fail due to invalid data.</exception>
/// <exception cref="EndOfStreamException">Thrown by reading methods when there are not enough bytes to read the required value.</exception>
public ref partial struct MessagePackReader
{
	/// <summary>
	/// The reader over the sequence.
	/// </summary>
	private MessagePackStreamingReader streamingReader;

	/// <inheritdoc cref="MessagePackReader(in ReadOnlySequence{byte})"/>
	public MessagePackReader(ReadOnlyMemory<byte> msgpack)
		: this(new ReadOnlySequence<byte>(msgpack))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackReader"/> struct.
	/// </summary>
	/// <param name="msgpack">The sequence to read from.</param>
	public MessagePackReader(scoped in ReadOnlySequence<byte> msgpack)
		: this()
	{
		this.streamingReader = new(msgpack, null, null);
	}

	[UnscopedRef]
	private ref SequenceReader<byte> reader => ref this.streamingReader.SequenceReader;

	/// <summary>
	/// Gets the <see cref="ReadOnlySequence{T}"/> originally supplied to the constructor.
	/// </summary>
	public ReadOnlySequence<byte> Sequence => this.reader.Sequence;

	/// <summary>
	/// Gets the current position of the reader within <see cref="Sequence"/>.
	/// </summary>
	public SequencePosition Position => this.reader.Position;

	/// <summary>
	/// Gets the number of bytes consumed by the reader.
	/// </summary>
	public long Consumed => this.reader.Consumed;

	/// <summary>
	/// Gets a value indicating whether the reader is at the end of the sequence.
	/// </summary>
	public bool End => this.reader.End;

	/// <summary>
	/// Gets a value indicating whether the reader position is pointing at a nil value.
	/// </summary>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	public bool IsNil => this.NextCode == MessagePackCode.Nil;

	/// <summary>
	/// Gets the next message pack type to be read.
	/// </summary>
	public MessagePackType NextMessagePackType => MessagePackCode.ToMessagePackType(this.NextCode);

	/// <summary>
	/// Gets the type of the next MessagePack block.
	/// </summary>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	/// <remarks>
	/// See <see cref="MessagePackCode"/> for valid message pack codes and ranges.
	/// </remarks>
	public byte NextCode
	{
		get
		{
			ThrowInsufficientBufferUnless(this.reader.TryPeek(out byte code));
			return code;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackReader"/> struct,
	/// with the same settings as this one, but with its own buffer to read from.
	/// </summary>
	/// <param name="readOnlySequence">The sequence to read from.</param>
	/// <returns>The new reader.</returns>
	public MessagePackReader Clone(scoped in ReadOnlySequence<byte> readOnlySequence) => new MessagePackReader(readOnlySequence);

	/// <summary>
	/// Creates a new <see cref="MessagePackReader"/> at this reader's current position.
	/// The two readers may then be used independently without impacting each other.
	/// </summary>
	/// <returns>A new <see cref="MessagePackReader"/>.</returns>
	/// <devremarks>
	/// Since this is a struct, copying it completely is as simple as returning itself
	/// from a property that isn't a "ref return" property.
	/// </devremarks>
	public MessagePackReader CreatePeekReader() => this;

	/// <summary>
	/// Advances the reader to the next MessagePack structure to be read.
	/// </summary>
	/// <param name="context">Serialization context. Used for the stack guard.</param>
	/// <remarks>
	/// The entire structure is skipped, including content of maps or arrays, or any other type with payloads.
	/// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw(SerializationContext)"/> instead.
	/// </remarks>
	public void Skip(SerializationContext context) => ThrowInsufficientBufferUnless(this.TrySkip(context));

	/// <summary>
	/// Reads a <see cref="MessagePackCode.Nil"/> value.
	/// </summary>
	public void ReadNil()
	{
		switch (this.streamingReader.TryReadNil())
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			default:
				throw ThrowNotEnoughBytesException();
		}
	}

	/// <summary>
	/// Reads nil if it is the next token.
	/// </summary>
	/// <returns><see langword="true"/> if the next token was nil; <see langword="false"/> otherwise.</returns>
	/// <exception cref="EndOfStreamException">Thrown if the end of the sequence provided to the constructor is reached before the expected end of the data.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadNil()
	{
		switch (this.streamingReader.TryReadNil())
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return true;
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				return false;
		}
	}

	/// <summary>
	/// Reads a sequence of bytes without any decoding.
	/// </summary>
	/// <param name="length">The number of bytes to read.</param>
	/// <returns>
	/// The raw MessagePack sequence, taken as a slice from the <see cref="Sequence"/>.
	/// The caller should copy any data that must out-live its underlying buffers.
	/// </returns>
	public ReadOnlySequence<byte> ReadRaw(long length)
	{
		switch (this.streamingReader.TryReadRaw(length, out ReadOnlySequence<byte> result))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return result;
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads the next MessagePack structure.
	/// </summary>
	/// <param name="context">The serialization context. Used for the stack guard.</param>
	/// <returns>
	/// The raw MessagePack sequence, taken as a slice from the <see cref="Sequence"/>.
	/// The caller should copy any data that must out-live its underlying buffers.
	/// </returns>
	/// <remarks>
	/// The entire structure is read, including content of maps or arrays, or any other type with payloads.
	/// </remarks>
	public ReadOnlySequence<byte> ReadRaw(SerializationContext context)
	{
		SequencePosition initialPosition = this.Position;
		this.Skip(context);
		return this.Sequence.Slice(initialPosition, this.Position);
	}

	/// <summary>
	/// Read an array header from
	/// <see cref="MessagePackCode.Array16"/>,
	/// <see cref="MessagePackCode.Array32"/>, or
	/// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>.
	/// </summary>
	/// <returns>The number of elements in the array.</returns>
	/// <exception cref="EndOfStreamException">
	/// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
	/// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
	/// </exception>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an array header is encountered.</exception>
	public int ReadArrayHeader()
	{
		ThrowInsufficientBufferUnless(this.TryReadArrayHeader(out int count));

		// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
		// We allow for each primitive to be the minimal 1 byte in size.
		// Formatters that know each element is larger can optionally add a stronger check.
		ThrowInsufficientBufferUnless(this.reader.Remaining >= count);

		return count;
	}

	/// <summary>
	/// Reads an array header from
	/// <see cref="MessagePackCode.Array16"/>,
	/// <see cref="MessagePackCode.Array32"/>, or
	/// some built-in code between <see cref="MessagePackCode.MinFixArray"/> and <see cref="MessagePackCode.MaxFixArray"/>
	/// if there is sufficient buffer to read it.
	/// </summary>
	/// <param name="count">Receives the number of elements in the array if the entire array header could be read.</param>
	/// <returns><see langword="true"/> if there was sufficient buffer and an array header was found; <see langword="false"/> if the buffer incompletely describes an array header.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an array header is encountered.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryReadArrayHeader(out int count)
	{
		switch (this.streamingReader.TryReadArrayHeader(out count))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return true;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}

	}

	/// <summary>
	/// Read a map header from
	/// <see cref="MessagePackCode.Map16"/>,
	/// <see cref="MessagePackCode.Map32"/>, or
	/// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>.
	/// </summary>
	/// <returns>The number of key=value pairs in the map.</returns>
	/// <exception cref="EndOfStreamException">
	/// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
	/// or if it is clear that there are insufficient bytes remaining after the header to include all the elements the header claims to be there.
	/// </exception>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an map header is encountered.</exception>
	public int ReadMapHeader()
	{
		ThrowInsufficientBufferUnless(this.TryReadMapHeader(out int count));

		// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
		// We allow for each primitive to be the minimal 1 byte in size, and we have a key=value map, so that's 2 bytes.
		// Formatters that know each element is larger can optionally add a stronger check.
		ThrowInsufficientBufferUnless(this.reader.Remaining >= count * 2);

		return count;
	}

	/// <summary>
	/// Reads a map header from
	/// <see cref="MessagePackCode.Map16"/>,
	/// <see cref="MessagePackCode.Map32"/>, or
	/// some built-in code between <see cref="MessagePackCode.MinFixMap"/> and <see cref="MessagePackCode.MaxFixMap"/>
	/// if there is sufficient buffer to read it.
	/// </summary>
	/// <param name="count">Receives the number of key=value pairs in the map if the entire map header can be read.</param>
	/// <returns><see langword="true"/> if there was sufficient buffer and a map header was found; <see langword="false"/> if the buffer incompletely describes an map header.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an map header is encountered.</exception>
	public bool TryReadMapHeader(out int count)
	{
		switch (this.streamingReader.TryReadMapHeader(out count))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return true;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a boolean value from either a <see cref="MessagePackCode.False"/> or <see cref="MessagePackCode.True"/>.
	/// </summary>
	/// <returns>The value.</returns>
	public bool ReadBoolean()
	{
		switch (this.streamingReader.TryRead(out bool value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a <see cref="char"/> from any of:
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// or anything between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
	/// </summary>
	/// <returns>A character.</returns>
	public char ReadChar() => (char)this.ReadUInt16();

	/// <summary>
	/// Reads an <see cref="float"/> value from any value encoded with:
	/// <see cref="MessagePackCode.Float32"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>,
	/// <see cref="MessagePackCode.Int64"/>,
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.UInt64"/>,
	/// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
	/// </summary>
	/// <returns>The value.</returns>
	public unsafe float ReadSingle()
	{
		switch (this.streamingReader.TryRead(out float value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads an <see cref="double"/> value from any value encoded with:
	/// <see cref="MessagePackCode.Float64"/>,
	/// <see cref="MessagePackCode.Float32"/>,
	/// <see cref="MessagePackCode.Int8"/>,
	/// <see cref="MessagePackCode.Int16"/>,
	/// <see cref="MessagePackCode.Int32"/>,
	/// <see cref="MessagePackCode.Int64"/>,
	/// <see cref="MessagePackCode.UInt8"/>,
	/// <see cref="MessagePackCode.UInt16"/>,
	/// <see cref="MessagePackCode.UInt32"/>,
	/// <see cref="MessagePackCode.UInt64"/>,
	/// or some value between <see cref="MessagePackCode.MinNegativeFixInt"/> and <see cref="MessagePackCode.MaxNegativeFixInt"/>,
	/// or some value between <see cref="MessagePackCode.MinFixInt"/> and <see cref="MessagePackCode.MaxFixInt"/>.
	/// </summary>
	/// <returns>The value.</returns>
	public unsafe double ReadDouble()
	{
		switch (this.streamingReader.TryRead(out double value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a <see cref="DateTime"/> from a value encoded with
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>, or
	/// <see cref="MessagePackCode.Ext8"/>.
	/// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
	/// </summary>
	/// <returns>The value.</returns>
	public DateTime ReadDateTime()
	{
		switch (this.streamingReader.TryRead(out DateTime value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a <see cref="DateTime"/> from a value encoded with
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.Ext8"/>.
	/// Expects extension type code <see cref="ReservedMessagePackExtensionTypeCode.DateTime"/>.
	/// </summary>
	/// <param name="header">The extension header that was already read.</param>
	/// <returns>The value.</returns>
	public DateTime ReadDateTime(ExtensionHeader header)
	{
		switch (this.streamingReader.TryRead(header, out DateTime value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a span of bytes, whose length is determined by a header of one of these types:
	/// <see cref="MessagePackCode.Bin8"/>,
	/// <see cref="MessagePackCode.Bin16"/>,
	/// <see cref="MessagePackCode.Bin32"/>,
	/// or to support OldSpec compatibility:
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or something between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
	/// </summary>
	/// <returns>
	/// A sequence of bytes, or <see langword="null"/> if the read token is <see cref="MessagePackCode.Nil"/>.
	/// The data is a slice from the original sequence passed to this reader's constructor.
	/// </returns>
	public ReadOnlySequence<byte>? ReadBytes()
	{
		if (this.TryReadNil())
		{
			return null;
		}

		uint length = this.GetBytesLength();
		ThrowInsufficientBufferUnless(this.reader.Remaining >= length);
		ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
		this.reader.Advance(length);
		return result;
	}

	/// <summary>
	/// Reads a string of bytes, whose length is determined by a header of one of these types:
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
	/// </summary>
	/// <returns>
	/// The sequence of bytes, or <see langword="null"/> if the read token is <see cref="MessagePackCode.Nil"/>.
	/// The data is a slice from the original sequence passed to this reader's constructor.
	/// </returns>
	public ReadOnlySequence<byte>? ReadStringSequence()
	{
		if (this.TryReadNil())
		{
			return null;
		}

		uint length = this.GetStringLengthInBytes();
		ThrowInsufficientBufferUnless(this.reader.Remaining >= length);
		ReadOnlySequence<byte> result = this.reader.Sequence.Slice(this.reader.Position, length);
		this.reader.Advance(length);
		return result;
	}

	/// <summary>
	/// Reads a string of bytes, whose length is determined by a header of one of these types:
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
	/// </summary>
	/// <param name="span">Receives the span to the string.</param>
	/// <returns>
	/// <see langword="true"/> if the string is contiguous in memory such that it could be set as a single span.
	/// <see langword="false"/> if the read token is <see cref="MessagePackCode.Nil"/> or the string is not in a contiguous span.
	/// </returns>
	/// <remarks>
	/// Callers should generally be prepared for a <see langword="false"/> result and failover to calling <see cref="ReadStringSequence"/>
	/// which can represent a <see langword="null"/> result and handle strings that are not contiguous in memory.
	/// </remarks>
	public bool TryReadStringSpan(out ReadOnlySpan<byte> span)
	{
		switch (this.streamingReader.TryReadStringSpan(out bool contiguous, out span))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return contiguous;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				if (this.streamingReader.TryPeekNextCode(out byte code) == MessagePackPrimitives.DecodeResult.Success
					&& code == MessagePackCode.Nil)
				{
					span = default;
					return false;
				}

				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads a string, whose length is determined by a header of one of these types:
	/// <see cref="MessagePackCode.Str8"/>,
	/// <see cref="MessagePackCode.Str16"/>,
	/// <see cref="MessagePackCode.Str32"/>,
	/// or a code between <see cref="MessagePackCode.MinFixStr"/> and <see cref="MessagePackCode.MaxFixStr"/>.
	/// </summary>
	/// <returns>A string, or <see langword="null"/> if the current msgpack token is <see cref="MessagePackCode.Nil"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string? ReadString()
	{
		switch (this.streamingReader.TryRead(out string? value))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return value;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				throw ThrowNotEnoughBytesException();
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads an extension format header, based on one of these codes:
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.FixExt16"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>.
	/// </summary>
	/// <returns>The extension header.</returns>
	/// <exception cref="EndOfStreamException">
	/// Thrown if the header cannot be read in the bytes left in the <see cref="Sequence"/>
	/// or if it is clear that there are insufficient bytes remaining after the header to include all the bytes the header claims to be there.
	/// </exception>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an extension format header is encountered.</exception>
	public ExtensionHeader ReadExtensionHeader()
	{
		ThrowInsufficientBufferUnless(this.TryReadExtensionHeader(out ExtensionHeader header));

		// Protect against corrupted or mischievous data that may lead to allocating way too much memory.
		ThrowInsufficientBufferUnless(this.reader.Remaining >= header.Length);

		return header;
	}

	/// <summary>
	/// Reads an extension format header, based on one of these codes:
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.FixExt16"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>
	/// if there is sufficient buffer to read it.
	/// </summary>
	/// <param name="extensionHeader">Receives the extension header if the remaining bytes in the <see cref="Sequence"/> fully describe the header.</param>
	/// <returns>A value indicating whether an extension header is fully represented at the reader's position.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if a code other than an extension format header is encountered.</exception>
	public bool TryReadExtensionHeader(out ExtensionHeader extensionHeader)
	{
		switch (this.streamingReader.TryReadExtensionHeader(out extensionHeader))
		{
			case MessagePackPrimitives.DecodeResult.Success:
				return true;
			case MessagePackPrimitives.DecodeResult.TokenMismatch:
				throw ThrowInvalidCode(this.NextCode);
			case MessagePackPrimitives.DecodeResult.EmptyBuffer:
			case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
				return false;
			default:
				throw ThrowUnreachable();
		}
	}

	/// <summary>
	/// Reads an extension format header and data, based on one of these codes:
	/// <see cref="MessagePackCode.FixExt1"/>,
	/// <see cref="MessagePackCode.FixExt2"/>,
	/// <see cref="MessagePackCode.FixExt4"/>,
	/// <see cref="MessagePackCode.FixExt8"/>,
	/// <see cref="MessagePackCode.FixExt16"/>,
	/// <see cref="MessagePackCode.Ext8"/>,
	/// <see cref="MessagePackCode.Ext16"/>, or
	/// <see cref="MessagePackCode.Ext32"/>.
	/// </summary>
	/// <returns>
	/// The extension format.
	/// The data is a slice from the original sequence passed to this reader's constructor.
	/// </returns>
	public Extension ReadExtension()
	{
		ExtensionHeader header = this.ReadExtensionHeader();
		try
		{
			ReadOnlySequence<byte> data = this.reader.Sequence.Slice(this.reader.Position, header.Length);
			this.reader.Advance(header.Length);
			return new Extension(header.TypeCode, data);
		}
		catch (ArgumentOutOfRangeException ex)
		{
			throw ThrowNotEnoughBytesException(ex);
		}
	}

	/// <summary>
	/// Throws an <see cref="MessagePackSerializationException"/> explaining an unexpected code was encountered.
	/// </summary>
	/// <param name="code">The code that was encountered.</param>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	internal static Exception ThrowInvalidCode(byte code)
	{
		throw new MessagePackSerializationException(string.Format("Unexpected msgpack code {0} ({1}) encountered.", code, MessagePackCode.ToFormatName(code)));
	}

	/// <summary>
	/// Advances the reader to the next MessagePack structure to be read.
	/// </summary>
	/// <param name="context">The serialization context. Used for the stack guard.</param>
	/// <returns><see langword="true"/> if the entire structure beginning at the current <see cref="Position"/> is found in the <see cref="Sequence"/>; <see langword="false"/> otherwise.</returns>
	/// <remarks>
	/// The entire structure is skipped, including content of maps or arrays, or any other type with payloads.
	/// To get the raw MessagePack sequence that was skipped, use <see cref="ReadRaw(SerializationContext)"/> instead.
	/// WARNING: when false is returned, the position of the reader is undefined.
	/// </remarks>
	internal bool TrySkip(SerializationContext context)
	{
		if (this.reader.Remaining == 0)
		{
			return false;
		}

		byte code = this.NextCode;
		switch (code)
		{
			case byte x when MessagePackCode.IsPositiveFixInt(x) || MessagePackCode.IsNegativeFixInt(x):
			case MessagePackCode.Nil:
			case MessagePackCode.True:
			case MessagePackCode.False:
				return this.reader.TryAdvance(1);
			case MessagePackCode.Int8:
			case MessagePackCode.UInt8:
				return this.reader.TryAdvance(2);
			case MessagePackCode.Int16:
			case MessagePackCode.UInt16:
				return this.reader.TryAdvance(3);
			case MessagePackCode.Int32:
			case MessagePackCode.UInt32:
			case MessagePackCode.Float32:
				return this.reader.TryAdvance(5);
			case MessagePackCode.Int64:
			case MessagePackCode.UInt64:
			case MessagePackCode.Float64:
				return this.reader.TryAdvance(9);
			case byte x when MessagePackCode.IsFixMap(x):
			case MessagePackCode.Map16:
			case MessagePackCode.Map32:
				context.DepthStep();
				return this.TrySkipNextMap(context);
			case byte x when MessagePackCode.IsFixArray(x):
			case MessagePackCode.Array16:
			case MessagePackCode.Array32:
				context.DepthStep();
				return this.TrySkipNextArray(context);
			case byte x when MessagePackCode.IsFixStr(x):
			case MessagePackCode.Str8:
			case MessagePackCode.Str16:
			case MessagePackCode.Str32:
				return this.TryGetStringLengthInBytes(out uint length) && this.reader.TryAdvance(length);
			case MessagePackCode.Bin8:
			case MessagePackCode.Bin16:
			case MessagePackCode.Bin32:
				return this.TryGetBytesLength(out length) && this.reader.TryAdvance(length);
			case MessagePackCode.FixExt1:
			case MessagePackCode.FixExt2:
			case MessagePackCode.FixExt4:
			case MessagePackCode.FixExt8:
			case MessagePackCode.FixExt16:
			case MessagePackCode.Ext8:
			case MessagePackCode.Ext16:
			case MessagePackCode.Ext32:
				return this.TryReadExtensionHeader(out ExtensionHeader header) && this.reader.TryAdvance(header.Length);
			default:
				// We don't actually expect to ever hit this point, since every code is supported.
				Debug.Fail("Missing handler for code: " + code);
				throw ThrowInvalidCode(code);
		}
	}

	/// <summary>
	/// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
	/// the promised data.
	/// </summary>
	private static EndOfStreamException ThrowNotEnoughBytesException() => throw new EndOfStreamException();

	/// <summary>
	/// Throws an exception indicating that there aren't enough bytes remaining in the buffer to store
	/// the promised data.
	/// </summary>
	private static EndOfStreamException ThrowNotEnoughBytesException(Exception innerException) => throw new EndOfStreamException(new EndOfStreamException().Message, innerException);

	/// <summary>
	/// Throws <see cref="EndOfStreamException"/> if a condition is false.
	/// </summary>
	/// <param name="condition">A boolean value.</param>
	/// <exception cref="EndOfStreamException">Thrown if <paramref name="condition"/> is <see langword="false"/>.</exception>
	private static void ThrowInsufficientBufferUnless(bool condition)
	{
		if (!condition)
		{
			ThrowNotEnoughBytesException();
		}
	}

	[DoesNotReturn]
	private static Exception ThrowUnreachable() => throw new Exception("Presumed unreachable point in code reached.");

	private uint GetBytesLength()
	{
		ThrowInsufficientBufferUnless(this.TryGetBytesLength(out uint length));
		return length;
	}

	private bool TryGetBytesLength(out uint length)
	{
		bool usingBinaryHeader = true;
		MessagePackPrimitives.DecodeResult readResult = MessagePackPrimitives.TryReadBinHeader(this.reader.UnreadSpan, out length, out int tokenSize);
		if (readResult == MessagePackPrimitives.DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return true;
		}

		return SlowPath(ref this, readResult, usingBinaryHeader, ref length, ref tokenSize);

		static bool SlowPath(ref MessagePackReader self, MessagePackPrimitives.DecodeResult readResult, bool usingBinaryHeader, ref uint length, ref int tokenSize)
		{
			switch (readResult)
			{
				case MessagePackPrimitives.DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return true;
				case MessagePackPrimitives.DecodeResult.TokenMismatch:
					if (usingBinaryHeader)
					{
						usingBinaryHeader = false;
						readResult = MessagePackPrimitives.TryReadStringHeader(self.reader.UnreadSpan, out length, out tokenSize);
						return SlowPath(ref self, readResult, usingBinaryHeader, ref length, ref tokenSize);
					}
					else
					{
						throw ThrowInvalidCode(self.reader.UnreadSpan[0]);
					}

				case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = usingBinaryHeader
							? MessagePackPrimitives.TryReadBinHeader(buffer, out length, out tokenSize)
							: MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
						return SlowPath(ref self, readResult, usingBinaryHeader, ref length, ref tokenSize);
					}
					else
					{
						length = default;
						return false;
					}

				default:
					throw ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Gets the length of the next string.
	/// </summary>
	/// <param name="length">Receives the length of the next string, if there were enough bytes to read it.</param>
	/// <returns><see langword="true"/> if there were enough bytes to read the length of the next string; <see langword="false"/> otherwise.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool TryGetStringLengthInBytes(out uint length)
	{
		MessagePackPrimitives.DecodeResult readResult = MessagePackPrimitives.TryReadStringHeader(this.reader.UnreadSpan, out length, out int tokenSize);
		if (readResult == MessagePackPrimitives.DecodeResult.Success)
		{
			this.reader.Advance(tokenSize);
			return true;
		}

		return SlowPath(ref this, readResult, ref length, ref tokenSize);

		static bool SlowPath(ref MessagePackReader self, MessagePackPrimitives.DecodeResult readResult, ref uint length, ref int tokenSize)
		{
			switch (readResult)
			{
				case MessagePackPrimitives.DecodeResult.Success:
					self.reader.Advance(tokenSize);
					return true;
				case MessagePackPrimitives.DecodeResult.TokenMismatch:
					throw ThrowInvalidCode(self.reader.UnreadSpan[0]);
				case MessagePackPrimitives.DecodeResult.EmptyBuffer:
				case MessagePackPrimitives.DecodeResult.InsufficientBuffer:
					Span<byte> buffer = stackalloc byte[tokenSize];
					if (self.reader.TryCopyTo(buffer))
					{
						readResult = MessagePackPrimitives.TryReadStringHeader(buffer, out length, out tokenSize);
						return SlowPath(ref self, readResult, ref length, ref tokenSize);
					}
					else
					{
						length = default;
						return false;
					}

				default:
					throw ThrowUnreachable();
			}
		}
	}

	/// <summary>
	/// Gets the length of the next string.
	/// </summary>
	/// <returns>The length of the next string.</returns>
	private uint GetStringLengthInBytes()
	{
		ThrowInsufficientBufferUnless(this.TryGetStringLengthInBytes(out uint length));
		return length;
	}

	private bool TrySkipNextArray(SerializationContext context) => this.TryReadArrayHeader(out int count) && this.TrySkip(count, context);

	private bool TrySkipNextMap(SerializationContext context) => this.TryReadMapHeader(out int count) && this.TrySkip(count * 2, context);

	private bool TrySkip(int count, SerializationContext context)
	{
		for (int i = 0; i < count; i++)
		{
			if (!this.TrySkip(context))
			{
				return false;
			}
		}

		return true;
	}
}
