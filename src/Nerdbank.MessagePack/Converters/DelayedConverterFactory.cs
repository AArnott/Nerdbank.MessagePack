// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
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
		=> new DelayedValue<MessagePackConverter<T>>(self => new DelayedConverter<T>(self));

	/// <summary>
	/// A converter that defers to another converter that is not yet available.
	/// </summary>
	/// <typeparam name="T">The convertible data type.</typeparam>
	/// <param name="self">A box containing the not-yet-done converter.</param>
	internal class DelayedConverter<T>(DelayedValue<MessagePackConverter<T>> self) : MessagePackConverter<T>
	{
		/// <inheritdoc/>
		public override bool PreferAsyncSerialization => self.Result.PreferAsyncSerialization;

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> self.Result.GetJsonSchema(context, typeShape);

		/// <inheritdoc/>
		public override T? Read(ref MessagePackReader reader, SerializationContext context)
			=> self.Result.Read(ref reader, context);

		/// <inheritdoc/>
		[Experimental("NBMsgPackAsync")]
		public override ValueTask<T?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
			=> self.Result.ReadAsync(reader, context);

		/// <inheritdoc/>
		[Experimental("NBMsgPackAsync")]
		public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
			=> self.Result.Write(ref writer, value, context);

		/// <inheritdoc/>
		[Experimental("NBMsgPackAsync")]
		public override ValueTask WriteAsync(MessagePackAsyncWriter writer, T? value, SerializationContext context)
			=> self.Result.WriteAsync(writer, value, context);
	}
}
