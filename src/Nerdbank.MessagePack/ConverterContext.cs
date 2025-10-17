// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Context in which a <see cref="IMessagePackConverterFactory"/> is invoked.
/// </summary>
/// <remarks>
/// Provides access to other converters that may be required by the requested converter.
/// </remarks>
public struct ConverterContext
{
	private readonly ConverterCache cache;
	private readonly bool preserveReferences;

	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterContext"/> struct.
	/// </summary>
	/// <param name="converterCache">The converter cache.</param>
	/// <param name="typeShapeProvider">The type shape provider.</param>
	/// <param name="referencePreservationMode">The reference preservation mode.</param>
	internal ConverterContext(ConverterCache converterCache, ITypeShapeProvider typeShapeProvider, ReferencePreservationMode referencePreservationMode)
	{
		this.cache = converterCache;
		this.TypeShapeProvider = typeShapeProvider;
		this.preserveReferences = referencePreservationMode != ReferencePreservationMode.Off;
	}

	/// <summary>
	/// Gets the <see cref="ITypeShapeProvider"/> that can provide shapes for given types.
	/// </summary>
	public ITypeShapeProvider TypeShapeProvider { get; }

#if NET
	/// <summary>
	/// Gets a converter for some type.
	/// </summary>
	/// <typeparam name="T">The type for which a converter is required.</typeparam>
	/// <returns>The converter.</returns>
	public MessagePackConverter<T> GetConverter<T>()
		where T : IShapeable<T>
	{
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		MessagePackConverter result = this.cache.GetOrAddConverter(T.GetTypeShape()).ValueOrThrow;
		return (MessagePackConverter<T>)(this.preserveReferences ? ((IMessagePackConverterInternal)result).WrapWithReferencePreservation() : result);
	}

	/// <summary>
	/// Gets a converter for some type.
	/// </summary>
	/// <typeparam name="T">The type for which a converter is required.</typeparam>
	/// <typeparam name="TProvider">The provider of the type's shape.</typeparam>
	/// <returns>The converter.</returns>
	public MessagePackConverter<T> GetConverter<T, TProvider>()
		where TProvider : IShapeable<T>
	{
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		MessagePackConverter result = this.cache.GetOrAddConverter(TProvider.GetTypeShape()).ValueOrThrow;
		return (MessagePackConverter<T>)(this.preserveReferences ? ((IMessagePackConverterInternal)result).WrapWithReferencePreservation() : result);
	}
#endif

	/// <summary>
	/// Gets a converter for a specific type.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	/// <param name="provider">
	/// <inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/>
	/// It can also come from <see cref="TypeShapeProvider"/>.
	/// A <see langword="null" /> value will be filled in with <see cref="TypeShapeProvider"/>.
	/// </param>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	[OverloadResolutionPriority(1)] // for null values, prefer this method over the one that takes ITypeShape.
	public MessagePackConverter<T> GetConverter<T>(ITypeShapeProvider? provider)
	{
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		MessagePackConverter result = this.cache.GetOrAddConverter<T>(provider ?? this.TypeShapeProvider ?? throw new UnreachableException()).ValueOrThrow;
		return (MessagePackConverter<T>)(this.preserveReferences ? ((IMessagePackConverterInternal)result).WrapWithReferencePreservation() : result);
	}

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <param name="typeShape">The shape of the type to be converted.</param>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	public MessagePackConverter GetConverter(ITypeShape typeShape)
	{
		Requires.NotNull(typeShape);
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		MessagePackConverter result = this.cache.GetOrAddConverter(typeShape).ValueOrThrow;
		return this.preserveReferences ? ((IMessagePackConverterInternal)result).WrapWithReferencePreservation() : result;
	}

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	/// <param name="typeShape">The shape of the type to be converted.</param>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	public MessagePackConverter<T> GetConverter<T>(ITypeShape<T> typeShape)
	{
		Requires.NotNull(typeShape);
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		MessagePackConverter result = this.cache.GetOrAddConverter(typeShape).ValueOrThrow;
		return (MessagePackConverter<T>)(this.preserveReferences ? ((IMessagePackConverterInternal)result).WrapWithReferencePreservation() : result);
	}

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <param name="type">The type to be converted.</param>
	/// <param name="provider"><inheritdoc cref="GetConverter{T}(ITypeShapeProvider?)" path="/param[@name='provider']"/></param>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	public MessagePackConverter GetConverter(Type type, ITypeShapeProvider? provider = null)
	{
		Requires.NotNull(type);
		Verify.Operation(this.cache is not null, "No serialization operation is in progress.");
		IMessagePackConverterInternal result = (IMessagePackConverterInternal)this.cache.GetOrAddConverter(type, provider ?? this.TypeShapeProvider ?? throw new UnreachableException()).ValueOrThrow;
		return this.preserveReferences ? result.WrapWithReferencePreservation() : (MessagePackConverter)result;
	}
}
