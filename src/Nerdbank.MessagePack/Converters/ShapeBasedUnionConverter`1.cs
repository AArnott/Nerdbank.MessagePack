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
    private readonly ShapeBasedUnionAnalyzer.ShapeBasedUnionMapping shapeMapping;
    private readonly Dictionary<Type, MessagePackConverter> convertersByType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShapeBasedUnionConverter{TUnion}"/> class.
    /// </summary>
    /// <param name="baseConverter">The converter for the base type.</param>
    /// <param name="shapeMapping">The shape-based mapping for distinguishing between types.</param>
    /// <param name="convertersByType">Converters for each member type.</param>
    public ShapeBasedUnionConverter(
        MessagePackConverter<TUnion> baseConverter,
        ShapeBasedUnionAnalyzer.ShapeBasedUnionMapping shapeMapping,
        Dictionary<Type, MessagePackConverter> convertersByType)
    {
        this.baseConverter = baseConverter;
        this.shapeMapping = shapeMapping;
        this.convertersByType = convertersByType;
        this.PreferAsyncSerialization = baseConverter.PreferAsyncSerialization || convertersByType.Values.Any(c => c.PreferAsyncSerialization);
    }

    /// <inheritdoc/>
    public override bool PreferAsyncSerialization { get; }

    /// <inheritdoc/>
    public override TUnion? Read(ref MessagePackReader reader, SerializationContext context)
    {
        if (reader.TryReadNil())
        {
            return default;
        }

        // Try to identify the type based on shape analysis
        MessagePackReader peekReader = reader.CreatePeekReader();
        if (this.shapeMapping.TryIdentifyType(ref peekReader, context, out ITypeShape? identifiedShape))
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
        
        // Use specific converter if available
        if (this.convertersByType.TryGetValue(actualType, out MessagePackConverter? converter))
        {
            converter.WriteObject(ref writer, value, context);
        }
        else
        {
            // Fall back to base converter
            this.baseConverter.Write(ref writer, value, context);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask<TUnion?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context)
    {
        MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
        
        // Check for null
        bool success;
        while (streamingReader.TryReadNil(out success).NeedsMoreBytes())
        {
            streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
        }

        if (success)
        {
            reader.ReturnReader(ref streamingReader);
            return default;
        }

        // For async scenarios, we need to buffer the entire value to perform shape analysis
        // This is the performance trade-off mentioned in the issue
        reader.ReturnReader(ref streamingReader);
        
        // Read the entire value into a buffer
        ReadOnlySequence<byte> bufferedData = await this.BufferValueAsync(reader, context).ConfigureAwait(false);
        MessagePackReader bufferedReader = new(bufferedData);
        
        // Now we can perform shape analysis on the buffered data
        if (this.shapeMapping.TryIdentifyType(ref bufferedReader, context, out ITypeShape? identifiedShape))
        {
            if (this.convertersByType.TryGetValue(identifiedShape.Type, out MessagePackConverter? converter))
            {
                bufferedReader = new(bufferedData); // Reset reader position
                return (TUnion?)converter.ReadObject(ref bufferedReader, context);
            }
        }

        // Fall back to base converter
        bufferedReader = new(bufferedData); // Reset reader position
        return this.baseConverter.Read(ref bufferedReader, context);
    }

    /// <inheritdoc/>
    public override async ValueTask WriteAsync(MessagePackAsyncWriter writer, TUnion? value, SerializationContext context)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        Type actualType = value.GetType();
        
        // Use specific converter if available
        if (this.convertersByType.TryGetValue(actualType, out MessagePackConverter? converter))
        {
            if (converter.PreferAsyncSerialization)
            {
                await converter.WriteObjectAsync(writer, value, context).ConfigureAwait(false);
            }
            else
            {
                MessagePackWriter syncWriter = writer.CreateWriter();
                converter.WriteObject(ref syncWriter, value, context);
                writer.ReturnWriter(ref syncWriter);
            }
        }
        else
        {
            // Fall back to base converter
            if (this.baseConverter.PreferAsyncSerialization)
            {
                await this.baseConverter.WriteAsync(writer, value, context).ConfigureAwait(false);
            }
            else
            {
                MessagePackWriter syncWriter = writer.CreateWriter();
                this.baseConverter.Write(ref syncWriter, value, context);
                writer.ReturnWriter(ref syncWriter);
            }
        }

        await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public override JsonObject? GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
    {
        // For shape-based unions, we create a oneOf schema without the array wrapper
        JsonArray oneOfArray = [];

        // Add base type schema
        var baseSchema = this.baseConverter.GetJsonSchema(context, typeShape) ?? CreateUndocumentedSchema(this.baseConverter.GetType());
        oneOfArray.Add(baseSchema);

        // Add schemas for each member type
        foreach (var (memberType, _) in this.convertersByType)
        {
            if (this.shapeMapping.TypeShapeMapping.TryGetValue(memberType, out ITypeShape? memberShape))
            {
                var memberSchema = context.GetJsonSchema(memberShape);
                oneOfArray.Add(memberSchema);
            }
        }

        return new JsonObject
        {
            ["oneOf"] = oneOfArray,
        };
    }

    /// <summary>
    /// Buffers an entire MessagePack value for analysis.
    /// </summary>
    /// <param name="reader">The async reader.</param>
    /// <param name="context">The serialization context.</param>
    /// <returns>The buffered data.</returns>
    private async ValueTask<ReadOnlySequence<byte>> BufferValueAsync(MessagePackAsyncReader reader, SerializationContext context)
    {
        // For now, this is a placeholder implementation that doesn't actually implement async buffering.
        // The async path would require complex buffering logic to collect the bytes while parsing.
        // For this initial implementation, we'll throw NotImplementedException to indicate
        // that sync-only shape analysis is supported.
        await Task.CompletedTask.ConfigureAwait(false); // Suppress async warning
        throw new NotImplementedException("Async buffering for shape-based unions is not yet implemented. Use sync deserialization for shape-based union analysis.");
    }

    private static JsonObject CreateUndocumentedSchema(Type converterType)
    {
        return new JsonObject
        {
            ["description"] = $"Converter {converterType.Name} did not provide a schema.",
        };
    }
}