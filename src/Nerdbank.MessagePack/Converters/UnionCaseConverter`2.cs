// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a derived type under the declared type of a base type.
/// </summary>
/// <typeparam name="TUnionCase">The derived type.</typeparam>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="inner">The converter that actually serializes <typeparamref name="TUnionCase"/>.</param>
internal class UnionCaseConverter<TUnionCase, TUnion>(MessagePackConverter<TUnionCase> inner) : MessagePackConverter<TUnion>
	where TUnionCase : TUnion
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => inner.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context) => inner.Read(ref reader, context);

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context) => inner.Write(ref writer, (TUnionCase?)value, context);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => inner.GetJsonSchema(context, typeShape);

	/// <inheritdoc/>
	public override async ValueTask<TUnion?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context) => await inner.ReadAsync(reader, context).ConfigureAwait(false);

	/// <inheritdoc/>
	public override ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context) => inner.WriteAsync(writer, (TUnionCase?)value, context);

	/// <inheritdoc/>
	public override ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context) => inner.SkipToIndexValueAsync(reader, index, context);

	/// <inheritdoc/>
	public override ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context) => inner.SkipToPropertyValueAsync(reader, propertyShape, context);

	/// <inheritdoc/>
	internal override MessagePackConverter<TUnion> UnwrapReferencePreservation() => throw new NotImplementedException();

	/// <inheritdoc/>
	internal override MessagePackConverter<TUnion> WrapWithReferencePreservation() => throw new NotImplementedException();
}
