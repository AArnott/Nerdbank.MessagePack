// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft;
using Nerdbank.PolySerializer.MessagePack;

namespace Nerdbank.PolySerializer.Converters;

/// <summary>
/// An abstract, non-generic base class for all converters.
/// </summary>
public abstract class Converter
{
	/// <summary>Initializes a new instance of the <see cref="Converter"/> class.</summary>
	/// <param name="type">The specific type that this converter can convert.</param>
	public Converter(Type type)
	{
	}

	/// <summary>
	/// Gets a value indicating whether callers should prefer the async methods on this object.
	/// </summary>
	/// <value>Unless overridden in a derived converter, this value is always <see langword="false"/>.</value>
	/// <remarks>
	/// Derived types that override the <see cref="WriteObjectAsync"/> and/or <see cref="ReadObjectAsync"/> methods
	/// should also override this property and have it return <see langword="true" />.
	/// </remarks>
	public virtual bool PreferAsyncSerialization => false;

	public virtual void VerifyCompatibility(Formatter formatter, StreamingDeformatter deformatter)
	{
		// We assume converters are compatible by default.
		// Converters that are specialized to a particular format should override this method and throw.
	}

	/// <summary>
	/// Serializes an instance of an object.
	/// </summary>
	/// <param name="writer">The writer to use.</param>
	/// <param name="value">The value to serialize.</param>
	/// <param name="context">Context for the serialization.</param>
	/// <remarks>
	/// Implementations of this method should not flush the writer.
	/// </remarks>
	public abstract void WriteObject(ref Writer writer, object? value, SerializationContext context);

	/// <summary>
	/// Deserializes an instance of an object.
	/// </summary>
	/// <param name="reader">The reader to use.</param>
	/// <param name="context">Context for the deserialization.</param>
	/// <returns>The deserialized value.</returns>
	public abstract object? ReadObject(ref Reader reader, SerializationContext context);

	/// <inheritdoc cref="WriteObject"/>
	/// <returns>A task that tracks the asynchronous operation.</returns>
	[Experimental("NBMsgPackAsync")]
	public abstract ValueTask WriteObjectAsync(AsyncWriter writer, object? value, SerializationContext context);

	/// <inheritdoc cref="ReadObject"/>
	[Experimental("NBMsgPackAsync")]
	public abstract ValueTask<object?> ReadObjectAsync(AsyncReader reader, SerializationContext context);

	/// <inheritdoc cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/>
	public virtual JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape) => null;

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value of the specified property can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="propertyShape">The shape of the property whose value is to be skipped to.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified property was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks><inheritdoc cref="SkipToIndexValueAsync(AsyncReader, object?, SerializationContext)" path="/remarks"/></remarks>
	[Experimental("NBMsgPackAsync")]
	public virtual ValueTask<bool> SkipToPropertyValueAsync(AsyncReader reader, IPropertyShape propertyShape, SerializationContext context)
		=> throw this.ThrowNotSupported();

	/// <summary>
	/// Skips ahead in the msgpack data to the point where the value at the specified index can be read.
	/// </summary>
	/// <param name="reader">The reader.</param>
	/// <param name="index">The key or index of the value to be retrieved.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns><see langword="true" /> if the specified index was found in the data and the value is ready to be read; <see langword="false" /> otherwise.</returns>
	/// <remarks>
	/// This method is used by <see cref="SerializerBase.DeserializeEnumerableAsync{T, TElement}(System.IO.Pipelines.PipeReader, ITypeShape{T}, MessagePackSerializer.StreamingEnumerationOptions{T, TElement}, CancellationToken)"/>
	/// to skip to the starting position of a sequence that should be asynchronously enumerated.
	/// </remarks>
	[Experimental("NBMsgPackAsync")]
	public virtual ValueTask<bool> SkipToIndexValueAsync(AsyncReader reader, object? index, SerializationContext context)
		=> throw this.ThrowNotSupported();

	internal abstract TResult Invoke<TState, TResult>(ITypedConverterInvoke<TState, TResult> invoker, TState state);

	/// <summary>
	/// Transforms a JSON schema to include "null" as a possible value for the schema.
	/// </summary>
	/// <param name="schema">The schema to transform. This value may be mutated.</param>
	/// <returns>The result of the transformation, which may be a different root object than given in <paramref name="schema"/>.</returns>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	protected internal static JsonObject ApplyJsonSchemaNullability(JsonObject schema)
	{
		Requires.NotNull(schema);

		if (schema.TryGetPropertyValue("type", out JsonNode? typeValue))
		{
			if (schema["type"] is JsonArray types)
			{
				if (!types.Any(n => n?.GetValueKind() == System.Text.Json.JsonValueKind.String && n.GetValue<string>() == "null"))
				{
					types.Add((JsonNode)"null");
				}
			}
			else
			{
				schema["type"] = new JsonArray { (JsonNode)(string)typeValue!, (JsonNode)"null" };
			}
		}
		else
		{
			// This is probably a schema reference.
			schema = new()
			{
				["oneOf"] = new JsonArray(schema, new JsonObject { ["type"] = "null" }),
			};
		}

		return schema;
	}

	/// <summary>
	/// Creates a JSON schema fragment that describes a type that has no documented schema.
	/// </summary>
	/// <param name="undocumentingConverter">The converter that has not provided a schema.</param>
	/// <returns>The JSON schema fragment that permits anything and explains why.</returns>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	protected internal static JsonObject CreateUndocumentedSchema(Type undocumentingConverter)
	{
		Requires.NotNull(undocumentingConverter);

		return new()
		{
			["type"] = new JsonArray("number", "integer", "string", "boolean", "object", "array", "null"),
			["description"] = $"The schema of this object is unknown as it is determined by the {undocumentingConverter.FullName} converter which does not override {nameof(GetJsonSchema)}.",
		};
	}

	/// <summary>
	/// Creates a JSON schema fragment that provides a cursory description of a binary blob, encoded as base64.
	/// </summary>
	/// <param name="description">An optional description to include with the schema.</param>
	/// <returns>A JSON schema fragment.</returns>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	protected static JsonObject CreateBase64EncodedBinarySchema(string? description = null)
	{
		// TODO: review this and move to Formatter.
		JsonObject schema = new()
		{
			["type"] = "string",
			["pattern"] = "^Binary as base64: ",
		};

		if (description is not null)
		{
			schema["description"] = description;
		}

		return schema;
	}

	protected static void VerifyFormat<T>(in Reader reader)
		where T : StreamingDeformatter
	{
		Verify.Operation(reader.Deformatter.StreamingDeformatter is T, $"This is a {reader.Deformatter.StreamingDeformatter.FormatName} sequence, but this converter expects to use {typeof(T).Name}.");
	}

	protected static void VerifyFormat<T>(in Writer writer)
		where T : Formatter
	{
		Verify.Operation(writer.Formatter is T, $"This is a {writer.Formatter.FormatName} sequence, but this converter expects to use {typeof(T).Name}.");
	}

	/// <summary>
	/// Wraps a boxed primitive as a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="value">The boxed primitive to wrap as a <see cref="JsonValue"/>. Only certain primitives are supported (roughly those supported by non-generic overloads of <c>JsonValue.Create</c>.</param>
	/// <returns>The <see cref="JsonValue"/>, or <see langword="null" /> if <paramref name="value"/> is <see langword="null" /> because <see cref="JsonValue"/> does not represent null.</returns>
	/// <exception cref="NotSupportedException">Thrown if <paramref name="value"/> is of a type that cannot be wrapped as a simple JSON value.</exception>
	/// <remarks>
	/// This is provided as a helper function for <see cref="GetJsonSchema(JsonSchemaContext, ITypeShape)"/> implementations.
	/// </remarks>
	[return: NotNullIfNotNull(nameof(value))]
	protected static JsonValue? CreateJsonValue(object? value)
	{
		return value switch
		{
			null => null,
			string v => JsonValue.Create(v),
			short v => JsonValue.Create(v),
			int v => JsonValue.Create(v),
			long v => JsonValue.Create(v),
			float v => JsonValue.Create(v),
			double v => JsonValue.Create(v),
			decimal v => JsonValue.Create(v),
			bool v => JsonValue.Create(v),
			byte v => JsonValue.Create(v),
			sbyte v => JsonValue.Create(v),
			ushort v => JsonValue.Create(v),
			uint v => JsonValue.Create(v),
			ulong v => JsonValue.Create(v),
			char v => JsonValue.Create(v),
			_ => throw new NotSupportedException($"Unsupported object type: {value.GetType().FullName}"),
		};
	}

	[DoesNotReturn]
	protected Exception ThrowNotSupported() => throw new NotSupportedException($"The {this.GetType().FullName} converter does not support this operation.");
}

public interface ITypedConverterInvoke<TState, TResult>
{
	TResult Invoke<T>(Converter<T> converter, TState state);
}
