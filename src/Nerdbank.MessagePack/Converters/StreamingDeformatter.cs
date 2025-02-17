// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Nerdbank.PolySerializer.Converters;

public abstract record StreamingDeformatter
{
	/// <summary>
	/// A value indicating whether no more bytes can be expected once we reach the end of the current buffer.
	/// </summary>
	private bool eof;

	public abstract string FormatName { get; }

	public abstract Encoding Encoding { get; }

	public abstract DecodeResult TryPeekNextCode(in Reader reader, out TypeCode typeCode);

	public abstract DecodeResult TryReadNull(ref Reader reader);

	public abstract DecodeResult TryReadNull(ref Reader reader, out bool isNull);

	public abstract DecodeResult TryReadArrayHeader(ref Reader reader, out int length);

	public abstract DecodeResult TryReadMapHeader(ref Reader reader, out int count);

	public abstract DecodeResult TryReadBinary(ref Reader reader, out ReadOnlySequence<byte> value);

	public abstract DecodeResult TryRead(ref Reader reader, out bool value);

	public abstract DecodeResult TryRead(ref Reader reader, out char value);

	public abstract DecodeResult TryRead(ref Reader reader, out sbyte value);

	public abstract DecodeResult TryRead(ref Reader reader, out short value);

	public abstract DecodeResult TryRead(ref Reader reader, out int value);

	public abstract DecodeResult TryRead(ref Reader reader, out long value);

	public abstract DecodeResult TryRead(ref Reader reader, out byte value);

	public abstract DecodeResult TryRead(ref Reader reader, out ushort value);

	public abstract DecodeResult TryRead(ref Reader reader, out uint value);

	public abstract DecodeResult TryRead(ref Reader reader, out ulong value);

	public abstract DecodeResult TryRead(ref Reader reader, out float value);

	public abstract DecodeResult TryRead(ref Reader reader, out double value);

	public abstract DecodeResult TryRead(ref Reader reader, out string? value);

	public abstract DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value);

	public abstract DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value);

	public abstract DecodeResult TrySkip(ref Reader reader, ref SerializationContext context);

	/// <summary>
	/// Reads a given number of bytes from the stream without decoding them.
	/// </summary>
	/// <param name="length">The number of bytes to read. This should always be the length of exactly one structure (e.g. scalar value, whole array or map).</param>
	/// <param name="rawMsgPack">The bytes if the read was successful.</param>
	/// <returns>The success or error code.</returns>
	public abstract DecodeResult TryReadRaw(ref Reader reader, long length, out ReadOnlySequence<byte> rawMsgPack);

	/// <summary>
	/// Tests whether the next token is an integer which <em>may</em> be negative.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="signed">When decoding is successful, receives a value indicating whether the integer may be negative.</param>
	/// <returns>Result of the decoding operation. <see cref="DecodeResult.TokenMismatch"/> if the next token is not an integer at all.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="TryRead(ref Reader, out long)"/>
	/// instead of <see cref="TryRead(ref Reader, out ulong)"/> to read a token typed as <see cref="TypeCode.Integer"/>.
	/// </remarks>
	public abstract DecodeResult TryPeekIsSignedInteger(in Reader reader, out bool signed);

	/// <summary>
	/// Tests whether the next token is encoded as a <see cref="float"/> (as opposed to a <see cref="double"/>).
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="float32">When decoding is successful, receives a value indicating whether the floating point number is encoded with only 32-bits.</param>
	/// <returns>Result of the decoding operation. <see cref="DecodeResult.TokenMismatch"/> if the next token is not a floating point number.</returns>
	/// <remarks>
	/// This is useful for readers that wish to know whether they should use <see cref="TryRead(ref Reader, out float)"/>
	/// instead of <see cref="TryRead(ref Reader, out double)"/> to read a token typed as <see cref="TypeCode.Float"/>.
	/// </remarks>
	public abstract DecodeResult TryPeekIsFloat32(in Reader reader, out bool float32);

	/// <summary>
	/// Throws an <see cref="SerializationException"/> explaining an unexpected code was encountered.
	/// </summary>
	/// <param name="reader">The reader positioned at the unexpected token.</param>
	/// <returns>Nothing. This method always throws.</returns>
	[DoesNotReturn]
	protected internal abstract Exception ThrowInvalidCode(in Reader reader);

	/// <summary>
	/// Gets the error code to return when the buffer has insufficient bytes to finish a decode request.
	/// </summary>
	protected DecodeResult InsufficientBytes(in Reader reader) => this.eof && reader.SequenceReader.Sequence.IsEmpty ? DecodeResult.EmptyBuffer : DecodeResult.InsufficientBuffer;
}
