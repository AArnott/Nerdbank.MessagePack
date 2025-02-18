// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace ShapeShift.Converters;

/// <summary>
/// A converter that uses a surrogate type to serialize and deserialize a type.
/// </summary>
/// <typeparam name="T">The type to be serialized.</typeparam>
/// <typeparam name="TSurrogate">the type of surrogate to be used.</typeparam>
/// <param name="shape">The shape of the type with a surrogate.</param>
/// <param name="surrogateConverter">The surrogate converter.</param>
internal class SurrogateConverter<T, TSurrogate>(ISurrogateTypeShape<T, TSurrogate> shape, Converter<TSurrogate> surrogateConverter)
	: Converter<T>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => surrogateConverter.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override T? Read(ref Reader reader, SerializationContext context)
		=> shape.Marshaller.FromSurrogate(surrogateConverter.Read(ref reader, context));

	/// <inheritdoc/>
	public override void Write(ref Writer writer, in T? value, SerializationContext context)
		=> surrogateConverter.Write(ref writer, shape.Marshaller.ToSurrogate(value), context);

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<T?> ReadAsync(AsyncReader reader, SerializationContext context)
		=> shape.Marshaller.FromSurrogate(await surrogateConverter.ReadAsync(reader, context).ConfigureAwait(false));

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override ValueTask WriteAsync(AsyncWriter writer, T? value, SerializationContext context)
		=> surrogateConverter.WriteAsync(writer, shape.Marshaller.ToSurrogate(value), context);

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		=> surrogateConverter.GetJsonSchema(context, shape.SurrogateType);
}
