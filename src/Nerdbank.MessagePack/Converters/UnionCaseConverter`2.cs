// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes a derived type under the declared type of a base type.
/// </summary>
/// <typeparam name="TUnionCase">The derived type.</typeparam>
/// <typeparam name="TUnion">The base type.</typeparam>
/// <param name="inner">The converter that actually serializes <typeparamref name="TUnionCase"/>.</param>
/// <param name="marshaler">The marshaler to change between <typeparamref name="TUnion"/> and <typeparamref name="TUnionCase"/>.</param>
/// <param name="csharpCase">The encoding options for a C# union payload, or null for a view of the union itself.</param>
internal class UnionCaseConverter<TUnionCase, TUnion>(MessagePackConverter<TUnionCase> inner, IMarshaler<TUnionCase, TUnion> marshaler, CSharpUnionCase? csharpCase) : MessagePackConverter<TUnion>
{
	/// <inheritdoc/>
	public override bool PreferAsyncSerialization => inner.PreferAsyncSerialization;

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (csharpCase is not null)
		{
			context.DepthStep();
		}

		return marshaler.Marshal(inner.Read(ref reader, context));
	}

	/// <inheritdoc/>
#pragma warning disable NBMsgPack031 // WriteFixedWidthInteger writes exactly one scalar.
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		if (csharpCase is not null)
		{
			context.DepthStep();
		}

		TUnionCase? payload = marshaler.Unmarshal(value);
		if (csharpCase?.UseFixedWidthIntegers == true)
		{
			WriteFixedWidthInteger(ref writer, payload!);
		}
		else
		{
			inner.Write(ref writer, payload, context);
		}
	}
#pragma warning restore NBMsgPack031

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => inner.GetJsonSchema(context, typeShape);

	/// <inheritdoc/>
	public override async ValueTask<TUnion?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
	{
		if (csharpCase is not null)
		{
			context.DepthStep();
		}

		return marshaler.Marshal(await inner.ReadAsync(reader, context).ConfigureAwait(false));
	}

	/// <inheritdoc/>
	public override ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context)
	{
		if (csharpCase?.UseFixedWidthIntegers == true)
		{
			return base.WriteAsync(writer, value, context);
		}

		if (csharpCase is not null)
		{
			context.DepthStep();
		}

		return inner.WriteAsync(writer, marshaler.Unmarshal(value), context);
	}

	/// <inheritdoc/>
	public override ValueTask<bool> SkipToIndexValueAsync(MessagePackAsyncReader reader, object? index, SerializationContext context) => inner.SkipToIndexValueAsync(reader, index, context);

	/// <inheritdoc/>
	public override ValueTask<bool> SkipToPropertyValueAsync(MessagePackAsyncReader reader, IPropertyShape propertyShape, SerializationContext context) => inner.SkipToPropertyValueAsync(reader, propertyShape, context);

	/// <inheritdoc/>
	public override bool SkipToIndexValue(ref MessagePackReader reader, object? index, SerializationContext context) => inner.SkipToIndexValue(ref reader, index, context);

	/// <inheritdoc/>
	public override bool SkipToPropertyValue(ref MessagePackReader reader, IPropertyShape propertyShape, SerializationContext context) => inner.SkipToPropertyValue(ref reader, propertyShape, context);

	/// <inheritdoc/>
	internal override MessagePackConverter<TUnion> UnwrapReferencePreservation() => throw new NotImplementedException();

	/// <inheritdoc/>
	internal override MessagePackConverter<TUnion> WrapWithReferencePreservation() => throw new NotImplementedException();

	private static void WriteFixedWidthInteger(ref MessagePackWriter writer, TUnionCase value)
	{
		if (typeof(TUnionCase) == typeof(sbyte))
		{
			writer.WriteInt8(Unsafe.As<TUnionCase, sbyte>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(byte))
		{
			writer.WriteUInt8(Unsafe.As<TUnionCase, byte>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(short))
		{
			writer.WriteInt16(Unsafe.As<TUnionCase, short>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(ushort))
		{
			writer.WriteUInt16(Unsafe.As<TUnionCase, ushort>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(char))
		{
			writer.WriteUInt16(Unsafe.As<TUnionCase, char>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(int))
		{
			writer.WriteInt32(Unsafe.As<TUnionCase, int>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(uint))
		{
			writer.WriteUInt32(Unsafe.As<TUnionCase, uint>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(long))
		{
			writer.WriteInt64(Unsafe.As<TUnionCase, long>(ref value));
		}
		else if (typeof(TUnionCase) == typeof(ulong))
		{
			writer.WriteUInt64(Unsafe.As<TUnionCase, ulong>(ref value));
		}
		else
		{
			throw new InvalidOperationException("The union case is not a native integer.");
		}
	}
}
