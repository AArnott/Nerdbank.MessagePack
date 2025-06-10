// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for <see cref="ExpandoObject"/>.
/// </summary>
/// <remarks>
/// <para>
/// This can <em>deserialize</em> anything, but can only <em>serialize</em> object graphs for which every runtime type
/// has a shape available as provided by <see cref="SerializationContext.TypeShapeProvider"/>.
/// </para>
/// <para>
/// This converter is not included by default because it requires dynamic code support in the runtime.
/// But it is offered as a converter that may be added to <see cref="MessagePackSerializer.Converters"/>
/// in order to enable <see cref="ExpandoObject"/> serialization.
/// </para>
/// </remarks>
[RequiresDynamicCode(Reasons.DynamicObject)]
internal class ExpandoObjectConverter : MessagePackConverter<ExpandoObject>
{
	/// <summary>
	/// A reusable, shareable instance of this converter.
	/// </summary>
	internal static readonly ExpandoObjectConverter Instance = new();

	/// <inheritdoc/>
	public override ExpandoObject? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		ExpandoObject result = new();
		int count = reader.ReadMapHeader();
		if (count > 0)
		{
			MessagePackConverter<string> keyFormatter = context.GetConverter<string>(MsgPackPrimitivesWitness.ShapeProvider);
			IDictionary<string, object?> dictionary = result;

			context.DepthStep();
			for (int i = 0; i < count; i++)
			{
				string? key = keyFormatter.Read(ref reader, context);
				if (key is null)
				{
					throw new NotSupportedException("Null key in map.");
				}

				object? value = PrimitivesAsDynamicConverter.Instance.Read(ref reader, context);
				dictionary.Add(key, value);
			}
		}

		return result;
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in ExpandoObject? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		IDictionary<string, object?> dict = value;
		MessagePackConverter<string> keyFormatter = context.GetConverter<string>(MsgPackPrimitivesWitness.ShapeProvider);

		writer.WriteMapHeader(dict.Count);
		foreach (KeyValuePair<string, object?> item in dict)
		{
			keyFormatter.Write(ref writer, item.Key, context);
			if (item.Value is null)
			{
				writer.WriteNil();
			}
			else
			{
				context.GetConverter(item.Value.GetType(), null).WriteObject(ref writer, item.Value, context);
			}
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => CreateUndocumentedSchema(this.GetType());
}
