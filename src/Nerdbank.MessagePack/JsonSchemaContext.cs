// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Nerdbank.MessagePack;

/// <summary>
/// The context provided to <see cref="IMessagePackConverterJsonSchemaProvider.GetJsonSchema(JsonSchemaContext)"/>
/// to aid in the generation of JSON schemas for types.
/// </summary>
public class JsonSchemaContext
{
	private static readonly JsonObject AnyTypeReferenceModel = new JsonObject
	{
		["$ref"] = "#/definitions/any",
	};

	private readonly MessagePackSerializer serializer;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonSchemaContext"/> class.
	/// </summary>
	/// <param name="serializer">The <see cref="MessagePackSerializer"/> object from which the JSON schema is being retrieved.</param>
	internal JsonSchemaContext(MessagePackSerializer serializer)
	{
		this.serializer = serializer;
	}

	/// <summary>
	/// Gets a value indicating whether the "#/definitions/any" schema was referenced.
	/// </summary>
	internal bool ReferencedAnyType { get; private set; }

	private JsonObject AnyTypeReference
	{
		get
		{
			this.ReferencedAnyType = true;
			return (JsonObject)AnyTypeReferenceModel.DeepClone();
		}
	}

	/// <summary>
	/// Obtains the JSON schema for a given type.
	/// </summary>
	/// <typeparam name="T">The type whose schema is required.</typeparam>
	/// <param name="typeShape">The shape for the type.</param>
	/// <returns>The JSON schema.</returns>
	public JsonObject GetJsonSchema(ITypeShape typeShape)
	{
		// TODO: be willing to return a reference to where this schema was previously generated.
		IMessagePackConverter converter = this.serializer.GetOrAddConverter(typeShape);
		if (converter.GetJsonSchema(this, typeShape) is JsonObject schema)
		{
			return schema;
		}

		JsonObject unknownSchema = this.AnyTypeReference;
		unknownSchema["description"] = $"The schema of this object is unknown as it is determined by the {converter.GetType().FullName} converter which does not override {nameof(MessagePackConverter<int>.GetJsonSchema)}.";
		return unknownSchema;
	}

	/// <inheritdoc cref="GetJsonSchema(ITypeShape)"/>
	public JsonObject GetJsonSchema<T>()
		where T : IShapeable<T> => this.GetJsonSchema(T.GetShape());
}
