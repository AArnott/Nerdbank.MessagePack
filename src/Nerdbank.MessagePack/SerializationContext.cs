// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Context that flows through the serialization process.
/// </summary>
/// <example>
/// The default values on this struct may be changed and the modified struct applied to <see cref="MessagePackSerializer.StartingContext"/>
/// in order to serialize with the updated settings.
/// <code source="../../samples/ApplyingSerializationContext.cs" region="ApplyingStartingContext" lang="C#" />
/// </example>
[DebuggerDisplay($"Depth remaining = {{{nameof(MaxDepth)}}}")]
public record struct SerializationContext
{
	private ImmutableDictionary<object, object?> specialState = ImmutableDictionary<object, object?>.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="SerializationContext"/> struct.
	/// </summary>
	public SerializationContext()
	{
	}

	/// <summary>
	/// Gets or sets the remaining depth of the object graph to serialize or deserialize.
	/// </summary>
	/// <value>The default value is 64.</value>
	/// <remarks>
	/// Exceeding this depth will result in a <see cref="MessagePackSerializationException"/> being thrown
	/// from <see cref="DepthStep"/>.
	/// </remarks>
	public int MaxDepth { get; set; } = 64;

	/// <summary>
	/// Gets a hint as to the number of bytes to write into the buffer before serialization will flush the output.
	/// </summary>
	/// <value>The default value is 64KB.</value>
	public int UnflushedBytesThreshold { get; init; } = 64 * 1024;

	/// <summary>
	/// Gets a cancellation token that can be used to cancel the serialization operation.
	/// </summary>
	/// <remarks>
	/// In <see cref="MessagePackConverter{T}.WriteAsync(MessagePackAsyncWriter, T, SerializationContext)" />
	/// or <see cref="MessagePackConverter{T}.ReadAsync(MessagePackAsyncReader, SerializationContext)"/> methods,
	/// this will tend to be equivalent to the <c>cancellationToken</c> parameter passed to those methods.
	/// </remarks>
	public CancellationToken CancellationToken { get; init; }

	/// <summary>
	/// Gets the type shape provider that applies to the serialization operation.
	/// </summary>
	public ITypeShapeProvider? TypeShapeProvider { get; internal init; }

	/// <summary>
	/// Gets the <see cref="MessagePackSerializer"/> that owns this context.
	/// </summary>
	internal ConverterCache? Cache { get; private init; }

	/// <summary>
	/// Gets the reference equality tracker for this serialization operation.
	/// </summary>
	internal ReferenceEqualityTracker? ReferenceEqualityTracker { get; private init; }

	/// <summary>
	/// Gets or sets the number of elements that must still be skipped to complete a skip operation.
	/// </summary>
	/// <value>0 when no skip operation was suspended and is still incomplete.</value>
	internal int MidSkipRemainingCount { get; set; }

	/// <summary>
	/// Gets or sets special state to be exposed to converters during serialization.
	/// </summary>
	/// <param name="key">Any object that can act as a key in a dictionary.</param>
	/// <returns>The value stored under the specified key, or <see langword="null" /> if no value has been stored under that key.</returns>
	/// <remarks>
	/// <para>A key-value pair is removed from the underlying dictionary by assigning a value of <see langword="null" /> for a given key.</para>
	/// <para>
	/// Strings can serve as convenient keys, but may collide with the same string used by another part of the data model for another purpose.
	/// Make your strings sufficiently unique to avoid collisions, or use a <c>static readonly object MyKey = new object()</c> field that you expose
	/// such that all interested parties can access the object for a key that is guaranteed to be unique.
	/// </para>
	/// </remarks>
	public object? this[object key]
	{
		get => this.specialState.TryGetValue(key, out object? value) ? value : null;
		set => this.specialState = value is not null ? this.specialState.SetItem(key, value) : this.specialState.Remove(key);
	}

	/// <summary>
	/// Decrements the depth remaining and checks the cancellation token.
	/// </summary>
	/// <remarks>
	/// Converters that (de)serialize nested objects should invoke this once <em>before</em> passing the context to nested (de)serializers.
	/// </remarks>
	/// <exception cref="MessagePackSerializationException">Thrown if the depth limit has been exceeded.</exception>
	/// <exception cref="OperationCanceledException">Thrown if <see cref="CancellationToken"/> has been canceled.</exception>
	public void DepthStep()
	{
		this.CancellationToken.ThrowIfCancellationRequested();
		if (--this.MaxDepth < 0)
		{
			throw new MessagePackSerializationException("Exceeded maximum depth of object graph.");
		}
	}

#if NET
	/// <inheritdoc cref="GetConverter{T, TProvider}()"/>
	public MessagePackConverter<T> GetConverter<T>()
		where T : IShapeable<T>
	{
		Verify.Operation(this.Cache is not null, "No serialization operation is in progress.");
		MessagePackConverter<T> result = this.Cache.GetOrAddConverter(T.GetShape());
		return this.ReferenceEqualityTracker is null ? result : result.WrapWithReferencePreservation();
	}

	/// <summary>
	/// Gets a converter for a specific type.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	/// <typeparam name="TProvider">The type that provides the shape of the type to be converted.</typeparam>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	public MessagePackConverter<T> GetConverter<T, TProvider>()
		where TProvider : IShapeable<T>
	{
		Verify.Operation(this.Cache is not null, "No serialization operation is in progress.");
		MessagePackConverter<T> result = this.Cache.GetOrAddConverter(TProvider.GetShape());
		return this.ReferenceEqualityTracker is null ? result : result.WrapWithReferencePreservation();
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
	public MessagePackConverter<T> GetConverter<T>(ITypeShapeProvider? provider)
	{
		Verify.Operation(this.Cache is not null, "No serialization operation is in progress.");
		MessagePackConverter<T> result = this.Cache.GetOrAddConverter<T>(provider ?? this.TypeShapeProvider ?? throw new UnreachableException());
		return this.ReferenceEqualityTracker is null ? result : result.WrapWithReferencePreservation();
	}

	/// <summary>
	/// Gets a converter for a given type shape.
	/// </summary>
	/// <param name="shape">The shape of the type to be converted.</param>
	/// <returns>The converter.</returns>
	/// <exception cref="InvalidOperationException">Thrown if no serialization operation is in progress.</exception>
	/// <remarks>
	/// This method is intended only for use by custom converters in order to delegate conversion of sub-values.
	/// </remarks>
	public IMessagePackConverter GetConverter(ITypeShape shape)
	{
		Verify.Operation(this.Cache is not null, "No serialization operation is in progress.");
		IMessagePackConverterInternal result = this.Cache.GetOrAddConverter(shape);
		return this.ReferenceEqualityTracker is null ? result : result.WrapWithReferencePreservation();
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
	public IMessagePackConverter GetConverter(Type type, ITypeShapeProvider? provider)
	{
		Verify.Operation(this.Cache is not null, "No serialization operation is in progress.");
		IMessagePackConverterInternal result = this.Cache.GetOrAddConverter(type, provider ?? this.TypeShapeProvider ?? throw new UnreachableException());
		return this.ReferenceEqualityTracker is null ? result : result.WrapWithReferencePreservation();
	}

	/// <summary>
	/// Starts a new serialization operation.
	/// </summary>
	/// <param name="owner">The owning serializer.</param>
	/// <param name="cache">The converter cache.</param>
	/// <param name="provider"><inheritdoc cref="MessagePackSerializer.Deserialize{T}(ref MessagePackReader, ITypeShapeProvider, CancellationToken)" path="/param[@name='provider']"/></param>
	/// <param name="cancellationToken">A cancellation token to associate with this serialization operation.</param>
	/// <returns>The new context for the operation.</returns>
	internal SerializationContext Start(MessagePackSerializer owner, ConverterCache cache, ITypeShapeProvider provider, CancellationToken cancellationToken)
	{
		return this with
		{
			Cache = cache,
			ReferenceEqualityTracker = cache.PreserveReferences ? ReusableObjectPool<ReferenceEqualityTracker>.Take(owner) : null,
			TypeShapeProvider = provider,
			CancellationToken = cancellationToken,
		};
	}

	/// <summary>
	/// Responds to the conclusion of a serialization operation by recycling any relevant objects.
	/// </summary>
	internal void End()
	{
		if (this.ReferenceEqualityTracker is not null)
		{
			ReusableObjectPool<ReferenceEqualityTracker>.Return(this.ReferenceEqualityTracker);
		}
	}
}
