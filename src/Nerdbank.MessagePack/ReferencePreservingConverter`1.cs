// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack031 // NotExactlyOneStructure -- we're doing advanced stuff.
#pragma warning disable NBMsgPack032 // Reference preservation isn't supported when producing a schema at this point.

using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A converter that wraps another converter and ensures that references are preserved during serialization.
/// </summary>
/// <typeparam name="T">The type of value to be serialized.</typeparam>
/// <param name="inner">The actual converter to use when a value is serialized or deserialized for the first time in a stream.</param>
internal class ReferencePreservingConverter<T>(MessagePackConverter<T> inner) : MessagePackConverter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => false; // inner.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override void Read(ref MessagePackReader reader, ref T? value, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			value = default;
			return;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		value = context.ReferenceEqualityTracker.ReadObject(ref reader, inner, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		Assumes.NotNull(context.ReferenceEqualityTracker);
		context.ReferenceEqualityTracker.WriteObject(ref writer, value, inner, context);
	}

	/// <inheritdoc/>
	internal override MessagePackConverter<T> WrapWithReferencePreservation() => this;

	/// <inheritdoc/>
	internal override MessagePackConverter<T> UnwrapReferencePreservation() => inner;
}
