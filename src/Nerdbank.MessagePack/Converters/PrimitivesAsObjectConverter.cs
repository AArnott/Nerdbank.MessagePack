﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack031

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter that can write msgpack primitives or other convertible values typed as <see cref="object"/>
/// and reads everything into primitives, dictionaries and arrays.
/// </summary>
/// <remarks>
/// <para>
/// This converter is not included by default because untyped serialization is not generally desirable.
/// It is offered as a converter that may be added to <see cref="MessagePackSerializer.Converters"/>
/// in order to enable limited untyped serialization.
/// </para>
/// <para>
/// Maps are deserialized as objects that implement <see cref="IReadOnlyDictionary{TKey, TValue}"/>
/// where the key is <see cref="object"/> and the value is a nullable <see cref="object"/>.
/// This converter deserializes an entire msgpack structure using these primitives,
/// since no type information is available to deserialize any sub-graph as a higher-level type.
/// </para>
/// </remarks>
/// <seealso cref="PrimitivesAsDynamicConverter"/>
internal class PrimitivesAsObjectConverter : MessagePackConverter<object?>
{
	/// <summary>
	/// Gets the default instance of the converter.
	/// </summary>
	internal static readonly PrimitivesAsObjectConverter Instance = new();

	/// <summary>Reads any one msgpack structure.</summary>
	/// <param name="reader">The msgpack reader.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>A <see langword="null"/>, <see cref="string"/>, <see cref="long" />, <see cref="ulong" />, <see cref="float"/>, <see cref="double"/>, <see cref="DateTime"/>, <see cref="IReadOnlyDictionary{TKey, TValue}"/> with <see cref="object"/> keys and values, an array of <see cref="object"/>s, or an <see cref="Extension"/>.</returns>
	/// <exception cref="NotSupportedException">Thrown if a msgpack map includes a <see langword="null" /> key.</exception>
	/// <remarks>
	/// <para>
	/// Maps implement <see cref="IReadOnlyDictionary{TKey, TValue}"/> with <see cref="object" /> keys and values.
	/// </para>
	/// <para>
	/// Keep in mind when using integers that msgpack doesn't preserve integer length information.
	/// This method deserializes all integers as <see cref="ulong"/> (or <see cref="long"/> if the value is negative).
	/// Integer length stretching is automatically applied when using integers as keys in maps so that they can match.
	/// </para>
	/// <para>
	/// All arrays are instances of <c>object?[]</c>.
	/// </para>
	/// <para>
	/// Msgpack binary data is represented as a <c>byte[]</c> object.
	/// </para>
	/// <para>
	/// Msgpack extensions are represented by <see cref="Extension"/> values.
	/// </para>
	/// </remarks>
	public override object? Read(ref MessagePackReader reader, SerializationContext context)
	{
		return ReadOneObject(ref reader, context);

		object? ReadOneObject(ref MessagePackReader reader, SerializationContext context)
			=> reader.NextMessagePackType switch
			{
				MessagePackType.Nil => reader.ReadNil(),
				MessagePackType.Integer => MessagePackCode.IsSignedInteger(reader.NextCode) ? NonNonNegativeSignedInt(reader.ReadInt64()) : reader.ReadUInt64(),
				MessagePackType.Boolean => reader.ReadBoolean(),
				MessagePackType.Float => reader.NextCode == MessagePackCode.Float32 ? reader.ReadSingle() : (dynamic)reader.ReadDouble(),
				MessagePackType.String => reader.ReadString(),
				MessagePackType.Array => ReadArray(ref reader, context),
				MessagePackType.Map => ReadMap(ref reader, context),
				MessagePackType.Binary => reader.ReadBytes()!.Value.ToArray(),
				MessagePackType.Extension when TryReadDateTime(ref reader, out DateTime? dateTime) => dateTime,
				MessagePackType.Extension => reader.ReadExtension(),
				_ => throw new NotImplementedException($"{reader.NextMessagePackType} not yet implemented."),
			};

		static object NonNonNegativeSignedInt(long value) => value < 0 ? value : (ulong)value;

		static bool TryReadDateTime(ref MessagePackReader reader, out DateTime? dateTime)
		{
			MessagePackReader peekReader = reader;
			ExtensionHeader header = peekReader.ReadExtensionHeader();
			if (header.TypeCode != ReservedMessagePackExtensionTypeCode.DateTime)
			{
				dateTime = null;
				return false;
			}

			dateTime = peekReader.ReadDateTime(header);
			reader = peekReader;
			return true;
		}

		object?[] ReadArray(ref MessagePackReader reader, SerializationContext context)
		{
			context.DepthStep();
			object?[] array = new object?[reader.ReadArrayHeader()];
			for (int i = 0; i < array.Length; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				array[i] = ReadOneObject(ref reader, context);
			}

			return array;
		}

		object? ReadMap(ref MessagePackReader reader, SerializationContext context)
		{
			context.DepthStep();
			int count = reader.ReadMapHeader();
			Dictionary<object, object?> map = new(count);
			for (int i = 0; i < count; i++)
			{
				context.CancellationToken.ThrowIfCancellationRequested();
				object? key = ReadOneObject(ref reader, context);
				if (key is null)
				{
					throw new NotSupportedException("Null key in map cannot be represented in a .NET object graph.");
				}

				object? value = ReadOneObject(ref reader, context);
				map[key] = value;
			}

			return this.WrapDictionary(map);
		}
	}

	/// <inheritdoc />
	public override void Write(ref MessagePackWriter writer, in object? value, SerializationContext context)
	{
		switch (value)
		{
			case null:
				writer.WriteNil();
				break;
			case bool v:
				writer.Write(v);
				break;
			case string v:
				writer.Write(v);
				break;
			case char v:
				writer.Write(v);
				break;
			case DateTime v:
				writer.Write(v);
				break;
			case byte v:
				writer.Write(v);
				break;
			case ushort v:
				writer.Write(v);
				break;
			case uint v:
				writer.Write(v);
				break;
			case ulong v:
				writer.Write(v);
				break;
			case sbyte v:
				writer.Write(v);
				break;
			case short v:
				writer.Write(v);
				break;
			case int v:
				writer.Write(v);
				break;
			case long v:
				writer.Write(v);
				break;
			case double v:
				writer.Write(v);
				break;
			case float v:
				writer.Write(v);
				break;
			default:
				context.GetConverter(value.GetType(), null).WriteObject(ref writer, value, context);
				break;
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => CreateUndocumentedSchema(this.GetType());

	/// <summary>
	/// Wraps a dictionary in a more convenient accessor (i.e. minimally one that stretches integers).
	/// </summary>
	/// <param name="content">The dictionary to be wrapped.</param>
	/// <returns>The wrapper.</returns>
	protected virtual IReadOnlyDictionary<object, object?> WrapDictionary(IReadOnlyDictionary<object, object?> content)
		=> new IntegerStretchingDictionary(content ?? throw new ArgumentNullException(nameof(content)));
}
