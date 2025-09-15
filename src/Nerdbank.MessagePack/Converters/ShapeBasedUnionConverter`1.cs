// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A converter for union types that distinguishes between member types based on their shape characteristics
/// rather than explicit aliases.
/// </summary>
/// <typeparam name="TUnion">The union base type.</typeparam>
internal class ShapeBasedUnionConverter<TUnion> : MessagePackConverter<TUnion>
{
	private readonly MessagePackConverter<TUnion> baseConverter;
	private readonly DerivedTypeDuckTyping shapeMapping;
	private readonly Dictionary<Type, MessagePackConverter> convertersByType;

	/// <summary>
	/// Initializes a new instance of the <see cref="ShapeBasedUnionConverter{TUnion}"/> class.
	/// </summary>
	/// <param name="baseConverter">The converter for the base type.</param>
	/// <param name="shapeMapping">The shape-based mapping for distinguishing between types.</param>
	/// <param name="convertersByType">Converters for each member type.</param>
	public ShapeBasedUnionConverter(
		MessagePackConverter<TUnion> baseConverter,
		DerivedTypeDuckTyping shapeMapping,
		Dictionary<Type, MessagePackConverter> convertersByType)
	{
		this.baseConverter = baseConverter;
		this.shapeMapping = shapeMapping;
		this.convertersByType = convertersByType;
	}

	/// <inheritdoc/>
	public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		// Try to identify the type based on shape analysis
		if (this.shapeMapping.TryIdentifyType(reader, context, out ITypeShape? identifiedShape))
		{
			if (this.convertersByType.TryGetValue(identifiedShape.Type, out MessagePackConverter? converter))
			{
				return (TUnion?)converter.ReadObject(ref reader, context);
			}
		}

		// Fall back to base converter if no specific type could be identified
		return this.baseConverter.Read(ref reader, context);
	}

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TUnion? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		Type actualType = value.GetType();

		// Use specific converter if available, otherwise fallback to base converter.
		if (this.convertersByType.TryGetValue(actualType, out MessagePackConverter? converter))
		{
			converter.WriteObject(ref writer, value, context);
		}
		else
		{
			this.baseConverter.Write(ref writer, value, context);
		}
	}

	/// <inheritdoc/>
	public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
	{
		// For shape-based unions, we create a oneOf schema without the array wrapper
		JsonArray oneOfArray = [];

		// Add base type schema
		oneOfArray.Add((JsonNode?)this.baseConverter.GetJsonSchema(context, this.shapeMapping.BaseShape) ?? CreateUndocumentedSchema(this.baseConverter.GetType()));

		// Add schemas for each derived type.
		foreach (ITypeShape shape in this.shapeMapping.DerivedShapes.Span)
		{
			oneOfArray.Add((JsonNode)context.GetJsonSchema(shape));
		}

		return new JsonObject
		{
			["oneOf"] = oneOfArray,
		};
	}
}
