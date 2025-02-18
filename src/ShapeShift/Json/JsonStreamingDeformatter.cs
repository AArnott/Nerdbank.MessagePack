// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ShapeShift.Json;

/// <summary>
/// A JSON implementation of <see cref="StreamingDeformatter"/>.
/// </summary>
internal record JsonStreamingDeformatter : StreamingDeformatter
{
	/// <summary>
	/// The default instance.
	/// </summary>
	internal static readonly JsonStreamingDeformatter Default = new();

	private JsonStreamingDeformatter()
	{
	}

	/// <inheritdoc/>
	public override string FormatName => "JSON";

	/// <inheritdoc/>
	public override Encoding Encoding => JsonFormatter.DefaultUTF8Encoding;

	/// <inheritdoc/>
	public override DecodeResult TryPeekIsFloat32(in Reader reader, out bool float32)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryPeekIsSignedInteger(in Reader reader, out bool signed)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryPeekNextTypeCode(in Reader reader, out TokenType typeCode)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out bool value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out char value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out sbyte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out short value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out int value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out long value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out byte value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out ushort value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out uint value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out ulong value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out float value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out double value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryRead(ref Reader reader, out string? value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadBinary(ref Reader reader, out ReadOnlySequence<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadNull(ref Reader reader)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadNull(ref Reader reader, out bool isNull)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadRaw(ref Reader reader, long length, out ReadOnlySequence<byte> rawBytes)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStartMap(ref Reader reader, out int count)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStartVector(ref Reader reader, out int length)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStringSequence(ref Reader reader, out ReadOnlySequence<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TryReadStringSpan(scoped ref Reader reader, out bool contiguous, out ReadOnlySpan<byte> value)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	public override DecodeResult TrySkip(ref Reader reader, ref SerializationContext context)
	{
		throw new NotImplementedException();
	}

	/// <inheritdoc/>
	[DoesNotReturn]
	protected internal override Exception ThrowInvalidCode(in Reader reader)
	{
		throw new NotImplementedException();
	}
}
