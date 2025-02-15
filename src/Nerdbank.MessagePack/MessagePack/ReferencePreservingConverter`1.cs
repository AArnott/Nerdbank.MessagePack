// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack031 // NotExactlyOneStructure -- we're doing advanced stuff.
#pragma warning disable NBMsgPack032 // Reference preservation isn't supported when producing a schema at this point.

using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack;

/// <summary>
/// A converter that wraps another converter and ensures that references are preserved during serialization.
/// </summary>
/// <typeparam name="T">The type of value to be serialized.</typeparam>
/// <param name="inner">The actual converter to use when a value is serialized or deserialized for the first time in a stream.</param>
internal class ReferencePreservingConverter<T>(Converter<T> inner) : Converter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => inner.PreferAsyncSerialization;

	public override void VerifyCompatibility(Formatter formatter, StreamingDeformatter deformatter) => MessagePackConverter<int>.VerifyFormat(formatter, deformatter);

	/// <inheritdoc/>
	public override T? Read(ref Reader reader, SerializationContext context)
	{
		if (reader.TryReadNull())
		{
			return default;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		return context.ReferenceEqualityTracker.ReadObject(ref reader, inner, context);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> ReadAsync(AsyncReader reader, SerializationContext context)
	{
		StreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNil;
		while (streamingReader.TryReadNull(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (isNil)
		{
			return default;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		return await context.ReferenceEqualityTracker.ReadObjectAsync(reader, inner, context).ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		context.ReferenceEqualityTracker.WriteObject(ref writer, value, inner, context);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override ValueTask WriteAsync(AsyncWriter writer, T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNull();
			return default;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		return context.ReferenceEqualityTracker.WriteObjectAsync(writer, value, inner, context);
	}

	/// <inheritdoc/>
	internal override Converter WrapWithReferencePreservationCore() => this;

	/// <inheritdoc/>
	internal override Converter UnwrapReferencePreservation() => inner;
}
