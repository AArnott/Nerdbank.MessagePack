// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Microsoft;

namespace Nerdbank.PolySerializer.MessagePack.Converters;


/// <summary>
/// Contains a bunch of converters for arrays of primitives.
/// </summary>
/// <remarks>
/// These aren't strictly necessary, but because we can predict their max encoded representation and embed the
/// direct reader/writer calls, we can avoid the overhead of many tiny calls to
/// <see cref="MessagePackWriter.GetSpan(int)"/> and <see cref="MessagePackWriter.Advance(int)"/>,
/// which speeds things up considerably.
/// </remarks>
internal static partial class ArraysOfPrimitivesConverters
{
	/// <summary>
	/// Creates a converter optimized for primitive arrays if one is available for the given enumerable and element type.
	/// </summary>
	/// <typeparam name="TEnumerable">The type of enumerable.</typeparam>
	/// <typeparam name="TElement">The type of element.</typeparam>
	/// <param name="getEnumerable">The function that produces an <see cref="IEnumerable{T}"/> for a given <typeparamref name="TEnumerable"/>.</param>
	/// <param name="spanConstructor">The constructor for the enumerable type.</param>
	/// <param name="converter">Receives the hardware-accelerated converter if one is available.</param>
	/// <returns>A value indicating whether a converter is available.</returns>
	internal static bool TryGetConverter<TEnumerable, TElement>(
		Func<TEnumerable, IEnumerable<TElement>> getEnumerable,
		SpanConstructor<TElement, TEnumerable> spanConstructor,
		[NotNullWhen(true)] out Converter<TEnumerable>? converter)
	{
		// T[], Memory<T>, ReadOnlyMemory<T>, and possibly more types are all satisfiable by T[].
		// So we avoid allocating or borrowing a temporary array only to copy from it to the span constructor
		// for these types by just allocating an array up-front and returning it directly.
		object? spanConstructorToUse = typeof(TElement[]).IsAssignableTo(typeof(TEnumerable)) ? null : spanConstructor;

		if (typeof(TElement) == typeof(SByte))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new SByteArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<SByte>>)getEnumerable,
				(SpanConstructor<SByte, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Int16))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new Int16ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Int16>>)getEnumerable,
				(SpanConstructor<Int16, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Int32))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new Int32ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Int32>>)getEnumerable,
				(SpanConstructor<Int32, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Int64))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new Int64ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Int64>>)getEnumerable,
				(SpanConstructor<Int64, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(UInt16))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new UInt16ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<UInt16>>)getEnumerable,
				(SpanConstructor<UInt16, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(UInt32))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new UInt32ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<UInt32>>)getEnumerable,
				(SpanConstructor<UInt32, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(UInt64))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new UInt64ArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<UInt64>>)getEnumerable,
				(SpanConstructor<UInt64, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Single))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new SingleArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Single>>)getEnumerable,
				(SpanConstructor<Single, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Double))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new DoubleArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Double>>)getEnumerable,
				(SpanConstructor<Double, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		if (typeof(TElement) == typeof(Boolean))
		{
			converter = (MessagePackConverter<TEnumerable>)(object)new BooleanArrayConverter<TEnumerable>(
				(Func<TEnumerable, IEnumerable<Boolean>>)getEnumerable,
				(SpanConstructor<Boolean, TEnumerable>?)spanConstructorToUse);
			return true;
		}

		converter = null;
		return false;
	}

	/// <summary>
	/// A converter for <see cref="SByte"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class SByteArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<SByte>> getEnumerable,
		SpanConstructor<SByte, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, SByte>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override SByte Read(ref Reader reader) => reader.ReadSByte();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, SByte value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Int16"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class Int16ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Int16>> getEnumerable,
		SpanConstructor<Int16, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Int16>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Int16 Read(ref Reader reader) => reader.ReadInt16();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Int16 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Int32"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class Int32ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Int32>> getEnumerable,
		SpanConstructor<Int32, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Int32>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Int32 Read(ref Reader reader) => reader.ReadInt32();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Int32 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Int64"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class Int64ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Int64>> getEnumerable,
		SpanConstructor<Int64, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Int64>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Int64 Read(ref Reader reader) => reader.ReadInt64();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Int64 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="UInt16"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class UInt16ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<UInt16>> getEnumerable,
		SpanConstructor<UInt16, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, UInt16>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override UInt16 Read(ref Reader reader) => reader.ReadUInt16();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, UInt16 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="UInt32"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class UInt32ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<UInt32>> getEnumerable,
		SpanConstructor<UInt32, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, UInt32>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override UInt32 Read(ref Reader reader) => reader.ReadUInt32();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, UInt32 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="UInt64"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class UInt64ArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<UInt64>> getEnumerable,
		SpanConstructor<UInt64, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, UInt64>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override UInt64 Read(ref Reader reader) => reader.ReadUInt64();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, UInt64 value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "integer",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Single"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class SingleArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Single>> getEnumerable,
		SpanConstructor<Single, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Single>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Single Read(ref Reader reader) => reader.ReadSingle();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Single value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "number",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Double"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class DoubleArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Double>> getEnumerable,
		SpanConstructor<Double, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Double>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Double Read(ref Reader reader) => reader.ReadDouble();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Double value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "number",
				},
			};
	}

	/// <summary>
	/// A converter for <see cref="Boolean"/> enumerables.
	/// </summary>
	/// <typeparam name="TEnumerable">The concrete type of enumerable.</typeparam>
	private class BooleanArrayConverter<TEnumerable>(
		Func<TEnumerable, IEnumerable<Boolean>> getEnumerable,
		SpanConstructor<Boolean, TEnumerable>? spanConstructor) : PrimitiveArrayConverter<TEnumerable, Boolean>(getEnumerable, spanConstructor)
	{
		/// <inheritdoc/>
		protected override Boolean Read(ref Reader reader) => reader.ReadBoolean();

		/// <inheritdoc/>
		protected override bool TryWrite(Span<byte> msgpack, Boolean value, out int written) => MessagePackPrimitives.TryWrite(msgpack, value, out written);

		/// <inheritdoc/>
		public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
			=> new()
			{
				["type"] = "array",
				["items"] = new JsonObject
				{
					["type"] = "boolean",
				},
			};
	}
}
