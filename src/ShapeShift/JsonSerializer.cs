// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;
using ShapeShift.Json;

namespace ShapeShift;

/// <summary>
/// Serializes .NET objects using the JSON format.
/// </summary>
/// <devremarks>
/// <para>
/// This class may declare properties that customize how JSON serialization is performed.
/// These properties must use <see langword="init"/> accessors to prevent modification after construction,
/// since there is no means to replace converters once they are created.
/// </para>
/// </devremarks>
public record JsonSerializer : SerializerBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonSerializer"/> class.
	/// </summary>
	public JsonSerializer()
		: base(new JsonConverterCache(JsonFormatter.Default, new Deformatter(JsonStreamingDeformatter.Default)))
	{
	}

	/// <summary>
	/// Gets the encoding used to format JSON.
	/// </summary>
	public Encoding Encoding => this.ConverterCache.Formatter.Encoding;

#if NET
	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public string Serialize<T>(in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(in T, ITypeShapeProvider, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public string Serialize<T, TProvider>(in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(value, TProvider.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(string, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T>([StringSyntax("json")] string json, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Deserialize(json, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Deserialize{T}(string, ITypeShapeProvider, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public T? Deserialize<T, TProvider>([StringSyntax("json")] string json, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Deserialize(json, TProvider.GetShape(), cancellationToken);
#endif

	/// <inheritdoc cref="SerializerBase.Serialize{T}(ref Writer, in T, ITypeShape{T}, CancellationToken)" />
	/// <returns>A JSON string.</returns>
	public string Serialize<T>(in T? value, ITypeShape<T> typeShape, CancellationToken cancellationToken = default)
	{
		using SequencePool<byte>.Rental sequenceRental = SequencePool<byte>.Shared.Rent();
		this.Serialize(sequenceRental.Value, value, typeShape, cancellationToken);
		return this.Encoding.GetString(sequenceRental.Value.AsReadOnlySequence);
	}

	/// <inheritdoc cref="SerializerBase.Serialize{T}(ref Writer, in T, ITypeShapeProvider, CancellationToken)" />
	/// <returns>A JSON string.</returns>
	public string Serialize<T>(in T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		using SequencePool<byte>.Rental sequenceRental = SequencePool<byte>.Shared.Rent();
		this.Serialize(sequenceRental.Value, value, provider, cancellationToken);
		return this.Encoding.GetString(sequenceRental.Value.AsReadOnlySequence);
	}

	/// <inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)"/>
	/// <param name="json">The JSON to be deserialized.</param>
	/// <param name="typeShape"><inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" path="/param[@name='typeShape']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	public T? Deserialize<T>([StringSyntax("json")] string json, ITypeShape<T> typeShape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(json);
		byte[] bytes = ArrayPool<byte>.Shared.Rent(this.Encoding.GetMaxByteCount(json.Length));
		try
		{
			int byteCount = this.Encoding.GetBytes(json.AsSpan(), bytes);
			return this.Deserialize(bytes.AsMemory(0, byteCount), typeShape, cancellationToken);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	/// <inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)"/>
	/// <param name="json">The JSON to be deserialized.</param>
	/// <param name="provider"><inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken"><inheritdoc cref="SerializerBase.Deserialize{T}(ReadOnlyMemory{byte}, ITypeShape{T}, CancellationToken)" path="/param[@name='cancellationToken']"/></param>
	public T? Deserialize<T>([StringSyntax("json")] string json, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(json);
		byte[] bytes = ArrayPool<byte>.Shared.Rent(this.Encoding.GetMaxByteCount(json.Length));
		try
		{
			int byteCount = this.Encoding.GetBytes(json.AsSpan(), bytes);
			return this.Deserialize<T>(bytes.AsMemory(0, byteCount), provider, cancellationToken);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(bytes);
		}
	}
}
