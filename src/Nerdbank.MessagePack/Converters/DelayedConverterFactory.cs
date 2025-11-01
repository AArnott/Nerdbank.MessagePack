// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A factory for delayed converters (those that support recursive types).
/// </summary>
internal sealed class DelayedConverterFactory : IDelayedValueFactory
{
	/// <inheritdoc/>
	public DelayedValue Create<T>(ITypeShape<T> typeShape)
		=> new DelayedValue<ConverterResult>(self => ConverterResult.Ok(new DelayedConverter<T>(self)));

	/// <summary>
	/// A converter that defers to another converter that is not yet available.
	/// </summary>
	/// <typeparam name="T">The convertible data type.</typeparam>
	/// <param name="self">A box containing the not-yet-done converter.</param>
	internal class DelayedConverter<T>(DelayedValue<ConverterResult> self) : MessagePackConverter<T>
	{
		/// <inheritdoc/>
		public override bool PreferAsyncSerialization => self.Result.ValueOrThrow.PreferAsyncSerialization;

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> self.Result.ValueOrThrow.GetJsonSchema(context, typeShape);

		/// <inheritdoc/>
		public override T? Read(ref MessagePackReader reader, SerializationContext context)
			=> ((MessagePackConverter<T>)self.Result.ValueOrThrow).Read(ref reader, context);

		/// <inheritdoc/>
		public override ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
			=> ((MessagePackConverter<T>)self.Result.ValueOrThrow).ReadAsync(reader, context);

		/// <inheritdoc/>
		public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
			=> ((MessagePackConverter<T>)self.Result.ValueOrThrow).Write(ref writer, value, context);

		/// <inheritdoc/>
		public override ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context)
			=> ((MessagePackConverter<T>)self.Result.ValueOrThrow).WriteAsync(writer, value, context);

		/// <inheritdoc/>
		public override ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context)
			=> self.Result.ValueOrThrow.SkipToIndexValueAsync(reader, index, context);

		/// <inheritdoc/>
		public override ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
			=> self.Result.ValueOrThrow.SkipToPropertyValueAsync(reader, propertyShape, context);

		/// <inheritdoc/>
		public override bool SkipToIndexValue(ref MessagePackReader reader, object? index, SerializationContext context)
			=> self.Result.ValueOrThrow.SkipToIndexValue(ref reader, index, context);

		/// <inheritdoc/>
		public override bool SkipToPropertyValue(ref MessagePackReader reader, IPropertyShape propertyShape, SerializationContext context)
			=> self.Result.ValueOrThrow.SkipToPropertyValue(ref reader, propertyShape, context);
	}
}
