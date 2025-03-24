// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Converts the <see cref="MessagePackValue"/> variant struct to msgpack.
/// </summary>
internal class MessagePackValueConverter : MessagePackConverter<MessagePackValue>
{
	/// <inheritdoc/>
	public override MessagePackValue Read(ref MessagePackReader reader, SerializationContext context)
	{
		switch (reader.NextMessagePackType)
		{
			case MessagePackType.Nil:
				reader.ReadNil();
				return MessagePackValue.Nil;
			case MessagePackType.Integer:
				return MessagePackCode.IsSignedInteger(reader.NextCode) ? reader.ReadInt64() : reader.ReadUInt64();
			case MessagePackType.Boolean:
				return reader.ReadBoolean();
			case MessagePackType.Float when reader.NextCode == MessagePackCode.Float32:
				return reader.ReadSingle();
			case MessagePackType.Float when reader.NextCode == MessagePackCode.Float64:
				return reader.ReadDouble();
			case MessagePackType.String:
				return reader.ReadString();
			case MessagePackType.Binary:
				return reader.ReadBytes()!.Value.ToArray();
			case MessagePackType.Array:
				context.DepthStep();
				int length = reader.ReadArrayHeader();
				MessagePackValue[] array = new MessagePackValue[length];
				for (int i = 0; i < length; i++)
				{
					array[i] = this.Read(ref reader, context);
				}

				return array;
			case MessagePackType.Map:
				context.DepthStep();
				int count = reader.ReadMapHeader();
				Dictionary<MessagePackValue, MessagePackValue> dict = new(count);
				for (int i = 0; i < count; i++)
				{
					MessagePackValue key = this.Read(ref reader, context);
					MessagePackValue value = this.Read(ref reader, context);
					dict.Add(key, value);
				}

				return dict;
			case MessagePackType.Extension:
				return reader.ReadExtension();
			default:
				throw new NotSupportedException();
		}
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in MessagePackValue value, SerializationContext context)
	{
		switch (value.Kind)
		{
			case MessagePackValueKind.Nil:
				writer.WriteNil();
				break;
			case MessagePackValueKind.SignedInteger:
				writer.Write(value.ValueAsInt64);
				break;
			case MessagePackValueKind.UnsignedInteger:
				writer.Write(value.ValueAsUInt64);
				break;
			case MessagePackValueKind.Boolean:
				writer.Write(value.ValueAsBoolean);
				break;
			case MessagePackValueKind.Single:
				writer.Write(value.ValueAsSingle);
				break;
			case MessagePackValueKind.Double:
				writer.Write(value.ValueAsDouble);
				break;
			case MessagePackValueKind.String:
				writer.Write(value.ValueAsString);
				break;
			case MessagePackValueKind.Binary:
				writer.Write(value.ValueAsBinary.Span);
				break;
			case MessagePackValueKind.Array:
				context.DepthStep();
				ReadOnlySpan<MessagePackValue> array = value.ValueAsArray.Span;
				writer.WriteArrayHeader(array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					this.Write(ref writer, array[i], context);
				}

				break;
			case MessagePackValueKind.Map:
				context.DepthStep();
				IReadOnlyDictionary<MessagePackValue, MessagePackValue> map = value.ValueAsMap;
				writer.WriteMapHeader(map.Count);
				foreach (KeyValuePair<MessagePackValue, MessagePackValue> item in map)
				{
					this.Write(ref writer, item.Key, context);
					this.Write(ref writer, item.Value, context);
				}

				break;
			case MessagePackValueKind.Extension:
				writer.Write(value.ValueAsExtension);
				break;
			default:
				throw new NotSupportedException();
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;
}
