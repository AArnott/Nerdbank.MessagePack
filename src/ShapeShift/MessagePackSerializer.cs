// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable RS0026 // optional parameter on a method with overloads

using System.Diagnostics.CodeAnalysis;
using Microsoft;

using ShapeShift.MessagePack;

namespace ShapeShift;

/// <summary>
/// Serializes .NET objects using the MessagePack format.
/// </summary>
public record MessagePackSerializer : SerializerBase
{
	/// <summary>
	/// A thread-local, recyclable array that may be used for short bursts of code.
	/// </summary>
	[ThreadStatic]
	private static byte[]? scratchArray;

	/// <summary>
	/// Initializes a new instance of the <see cref="MessagePackSerializer"/> class.
	/// </summary>
	public MessagePackSerializer()
		: base(new MessagePackConverterCache(MessagePackFormatter.Default, MessagePackDeformatter.Default))
	{
	}

	/// <inheritdoc cref="ConverterCache.PreserveReferences"/>
	public bool PreserveReferences
	{
		get => this.ConverterCache.PreserveReferences;
		init => this.ConverterCache = this.ConverterCache with { PreserveReferences = value };
	}

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes { get; init; } = LibraryReservedMessagePackExtensionTypeCode.Default;

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration
	{
		get => this.ConverterCache.DisableHardwareAcceleration;
		init => this.ConverterCache = this.ConverterCache with { DisableHardwareAcceleration = value };
	}

	/// <inheritdoc cref="SerializerBase.ConverterCache" />
	internal new MessagePackConverterCache ConverterCache
	{
		get => (MessagePackConverterCache)base.ConverterCache;
		init => base.ConverterCache = value;
	}

	/// <inheritdoc />
	internal override ReusableObjectPool<IReferenceEqualityTracker>? ReferenceTrackingPool { get; } = new(() => new ReferenceEqualityTracker());

#if NET
	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public byte[] Serialize<T>(in T? value, CancellationToken cancellationToken = default)
		where T : IShapeable<T> => this.Serialize(value, T.GetShape(), cancellationToken);

	/// <inheritdoc cref="Serialize{T}(in T, ITypeShape{T}, CancellationToken)" />
	[ExcludeFromCodeCoverage]
	public byte[] Serialize<T, TProvider>(in T? value, CancellationToken cancellationToken = default)
		where TProvider : IShapeable<T> => this.Serialize(value, TProvider.GetShape(), cancellationToken);
#endif

	/// <inheritdoc cref="SerializerBase.Serialize{T}(ref Writer, in T, ITypeShape{T}, CancellationToken)" />
	/// <returns>A byte array containing the serialized msgpack.</returns>
	public byte[] Serialize<T>(in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(shape);

		// Although the static array is thread-local, we still want to null it out while using it
		// to avoid any potential issues with re-entrancy due to a converter that makes a (bad) top-level call to the serializer.
		(byte[] array, scratchArray) = (scratchArray ?? new byte[65536], null);
		try
		{
			Writer writer = new(SequencePool<byte>.Shared, array, this.Formatter);
			this.Serialize(ref writer, value, shape, cancellationToken);
			return writer.FlushAndGetArray();
		}
		finally
		{
			scratchArray = array;
		}
	}

	/// <inheritdoc cref="SerializerBase.Serialize{T}(ref Writer, in T, ITypeShapeProvider, CancellationToken)" />
	/// <returns>A byte array containing the serialized msgpack.</returns>
	public byte[] Serialize<T>(in T? value, ITypeShapeProvider provider, CancellationToken cancellationToken = default)
	{
		Requires.NotNull(provider);

		// Although the static array is thread-local, we still want to null it out while using it
		// to avoid any potential issues with re-entrancy due to a converter that makes a (bad) top-level call to the serializer.
		(byte[] array, scratchArray) = (scratchArray ?? new byte[65536], null);
		try
		{
			Writer writer = new(SequencePool<byte>.Shared, array, this.Formatter);
			this.Serialize(ref writer, value, provider, cancellationToken);
			return writer.FlushAndGetArray();
		}
		finally
		{
			scratchArray = array;
		}
	}

	/// <inheritdoc />
	protected internal override void RenderAsJson(ref Reader reader, TextWriter writer)
	{
		Requires.NotNull(writer);

		switch (((MessagePackDeformatter)reader.Deformatter).PeekNextMessagePackType(reader))
		{
			case MessagePackType.Extension:
				Extension extension = ((MessagePackDeformatter)this.Deformatter).ReadExtension(ref reader);
				writer.Write($"\"msgpack extension {extension.Header.TypeCode} as base64: ");
				writer.Write(Convert.ToBase64String(extension.Data.ToArray()));
				writer.Write('\"');
				break;
			case MessagePackType type:
				throw new NotImplementedException($"{type} not yet implemented.");
		}
	}
}
