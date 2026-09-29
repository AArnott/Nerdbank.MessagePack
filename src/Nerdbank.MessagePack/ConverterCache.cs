// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using PolyType.Utilities;

namespace Nerdbank.MessagePack;

/// <summary>
/// Tracks all inputs to converter construction and caches the results of construction itself.
/// </summary>
/// <param name="configuration">An immutable configuration that this cache builds upon.</param>
/// <remarks>
/// <para>
/// This type is observably immutable and thread-safe.
/// </para>
/// <para>
/// This type offers something of an information barrier to converter construction.
/// The <see cref="StandardVisitor"/> only gets a reference to this object,
/// and this object does <em>not</em> have a reference to <see cref="MessagePackSerializer"/>.
/// This ensures that properties on <see cref="MessagePackSerializer"/> cannot serve as inputs to the converters.
/// Thus, the only properties that should reset the <see cref="cachedConverters"/> are those declared on this type.
/// </para>
/// </remarks>
internal class ConverterCache(SerializerConfiguration configuration)
{
	/// <summary>
	/// The number of direct-mapped slots in <see cref="lastConverters"/>. Must be a power of two.
	/// </summary>
	private const int LastConverterSlotCount = 16;

	/// <summary>
	/// Assigns each type that is (de)serialized through this library a slot in <see cref="lastConverters"/>.
	/// </summary>
	private static int nextSlot = -1;

	/// <summary>
	/// An optimization that avoids the dictionary lookup to start serialization
	/// when the caller repeatedly serializes a small set of types.
	/// </summary>
	/// <remarks>
	/// Each element is either <see langword="null" /> or a converter result that the
	/// <see cref="ConverterResult.Shape"/> property identifies. Since each element is a reference,
	/// reads and writes are atomic, making this cache thread-safe without locks <em>and</em> without
	/// allocating a wrapper object on each cache miss.
	/// </remarks>
	private readonly ConverterResult?[] lastConverters = new ConverterResult?[LastConverterSlotCount];

	/// <summary>
	/// The most recently used converter result, which allows the extremely common case of
	/// repeatedly (de)serializing one type to skip even the slot computation.
	/// </summary>
	private ConverterResult? lastConverter;

	private MultiProviderTypeCache? cachedConverters;

	/// <inheritdoc cref="SerializerConfiguration.MultiDimensionalArrayFormat"/>
	internal MultiDimensionalArrayFormat MultiDimensionalArrayFormat => configuration.MultiDimensionalArrayFormat;

	/// <inheritdoc cref="SerializerConfiguration.PreserveReferences"/>
	internal ReferencePreservationMode PreserveReferences => configuration.PreserveReferences;

	/// <inheritdoc cref="SerializerConfiguration.SerializeEnumValuesByName"/>
	internal bool SerializeEnumValuesByName => configuration.SerializeEnumValuesByName;

	/// <inheritdoc cref="SerializerConfiguration.SerializeDefaultValues"/>
	internal SerializeDefaultValuesPolicy SerializeDefaultValues => configuration.SerializeDefaultValues;

	/// <inheritdoc cref="SerializerConfiguration.DeserializeDefaultValues"/>
	internal DeserializeDefaultValuesPolicy DeserializeDefaultValues => configuration.DeserializeDefaultValues;

	/// <inheritdoc cref="SerializerConfiguration.InternStrings"/>
	internal bool InternStrings => configuration.InternStrings;

	/// <inheritdoc cref="SerializerConfiguration.PropertyNamingPolicy"/>
	internal MessagePackNamingPolicy? PropertyNamingPolicy => configuration.PropertyNamingPolicy;

	/// <inheritdoc cref="SerializerConfiguration.ComparerProvider"/>
	internal IComparerProvider? ComparerProvider => configuration.ComparerProvider;

	/// <inheritdoc cref="SerializerConfiguration.PerfOverSchemaStability"/>
	internal bool PerfOverSchemaStability => configuration.PerfOverSchemaStability;

	/// <inheritdoc cref="SerializerConfiguration.IgnoreKeyAttributes"/>
	internal bool IgnoreKeyAttributes => configuration.IgnoreKeyAttributes;

	/// <inheritdoc cref="SerializerConfiguration.DisableHardwareAcceleration"/>
	internal bool DisableHardwareAcceleration => configuration.DisableHardwareAcceleration;

	/// <inheritdoc cref="SerializerConfiguration.UseDiscriminatorObjects"/>
	internal bool UseDiscriminatorObjects => configuration.UseDiscriminatorObjects;

	/// <summary>
	/// Gets all the converters this instance knows about so far.
	/// </summary>
	private MultiProviderTypeCache CachedConverters
	{
		get
		{
			if (this.cachedConverters is null)
			{
				this.cachedConverters = new()
				{
					DelayedValueFactory = new DelayedConverterFactory(),
					ValueBuilderFactory = ctx =>
					{
						StandardVisitor standardVisitor = new StandardVisitor(this, ctx);
						if (this.PreserveReferences == ReferencePreservationMode.Off)
						{
							return standardVisitor;
						}

						ReferencePreservingVisitor visitor = new(standardVisitor);
						standardVisitor.OutwardVisitor = visitor;
						return standardVisitor;
					},
				};
			}

			return this.cachedConverters;
		}
	}

	private ConcurrentDictionary<(Type Type, Type Provider), ITypeShape> CachedTypeShapes => field ??= new();

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal ConverterResult GetOrAddConverter<T>(ITypeShape<T> shape)
	{
		ConverterResult? lastConverter = this.lastConverter;
		if (lastConverter is not null && ReferenceEquals(lastConverter.Shape, shape))
		{
			return lastConverter;
		}

		lastConverter = this.lastConverters[TypeSlot<T>.Index];
		if (lastConverter is not null && ReferenceEquals(lastConverter.Shape, shape))
		{
			this.lastConverter = lastConverter;
			return lastConverter;
		}

		return this.AddConverter(shape);
	}

	/// <summary>
	/// Gets a successfully constructed converter for the given type shape.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if converter construction failed.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal MessagePackConverter<T> GetOrAddConverterValue<T>(ITypeShape<T> shape)
	{
		ConverterResult? lastConverter = this.lastConverter;
		if (lastConverter is not null && ReferenceEquals(lastConverter.Shape, shape))
		{
			return lastConverter.Value is { } converter
				? (MessagePackConverter<T>)converter
				: ThrowConverterError<T>(lastConverter);
		}

		return this.GetOrAddConverterValueSlow(shape);
	}

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal ConverterResult GetOrAddConverter(ITypeShape shape)
		=> (ConverterResult)this.CachedConverters.GetOrAdd(shape)!;

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The type to convert.</typeparam>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal ConverterResult GetOrAddConverter<T>(ITypeShapeProvider provider)
		=> (ConverterResult)this.CachedConverters.GetOrAddOrThrow(typeof(T), provider);

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="type">The type to convert.</param>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal ConverterResult GetOrAddConverter(Type type, ITypeShapeProvider provider)
		=> (ConverterResult)this.CachedConverters.GetOrAddOrThrow(type, provider);

	/// <inheritdoc cref="TypeShapeResolver.ResolveDynamicOrThrow{T}()"/>
#if NET8_0
	[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
	internal ITypeShape<T> ResolveDynamicTypeShapeOrThrow<T>()
	{
		Type type = typeof(T);
		(Type, Type) key = (type, type);
		if (!this.CachedTypeShapes.TryGetValue(key, out ITypeShape? shape))
		{
			// We want to cache the result because TypeShapeResolver.ResolveDynamicOrThrow instantiates a new ITypeShapeProvider with each call,
			// and we want to be alloc-free after the first call. See https://github.com/eiriktsarpalis/PolyType/pull/432/changes#r3260136940
			shape = this.CachedTypeShapes.GetOrAdd(key, TypeShapeResolver.ResolveDynamicOrThrow<T>());
		}

		return (ITypeShape<T>)shape;
	}

	/// <inheritdoc cref="TypeShapeResolver.ResolveDynamicOrThrow{T, TProvider}()"/>
#if NET8_0
	[RequiresDynamicCode(MessagePackSerializerExtensions.ResolveDynamicMessage)]
#endif
	internal ITypeShape<T> ResolveDynamicTypeShapeOrThrow<T, TProvider>()
	{
		(Type, Type) key = (typeof(T), typeof(TProvider));
		if (!this.CachedTypeShapes.TryGetValue(key, out ITypeShape? shape))
		{
			// We want to cache the result because TypeShapeResolver.ResolveDynamicOrThrow instantiates a new ITypeShapeProvider with each call,
			// and we want to be alloc-free after the first call. See https://github.com/eiriktsarpalis/PolyType/pull/432/changes#r3260136940
			shape = this.CachedTypeShapes.GetOrAdd(key, TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());
		}

		return (ITypeShape<T>)shape;
	}

	/// <summary>
	/// Gets a user-defined converter for the specified type if one is available from
	/// converters the user has supplied <em>at runtime</em>.
	/// </summary>
	/// <param name="type">The type to be converted.</param>
	/// <param name="typeShape">The shape of the data type that requires a converter, if available.</param>
	/// <param name="shapeProvider">The shape provider used for this conversion overall (which may not have a shape available if <paramref name="typeShape" /> is <see langword="null" />.</param>
	/// <param name="converter">Receives the converter, if the user provided one.</param>
	/// <returns>A value indicating whether a customer converter exists.</returns>
	/// <remarks>
	/// <para>
	/// This method only searches <see cref="SerializerConfiguration.Converters"/>,
	/// <see cref="SerializerConfiguration.ConverterTypes"/> and <see cref="SerializerConfiguration.ConverterFactories"/>
	/// for matches.
	/// <see cref="MessagePackConverterAttribute"/> is <em>not</em> considered.
	/// </para>
	/// <para>
	/// A converter returned from this method will be wrapped with reference-preservation logic when appropriate.
	/// </para>
	/// </remarks>
	internal bool TryGetRuntimeProfferedConverter(Type type, ITypeShape? typeShape, ITypeShapeProvider shapeProvider, [NotNullWhen(true)] out MessagePackConverter? converter)
	{
		converter = null;
		if (!configuration.Converters.TryGetConverter(type, out converter))
		{
			if (configuration.ConverterTypes.TryGetConverterType(type, out Type? converterType) ||
				(type.IsGenericType && configuration.ConverterTypes.TryGetConverterType(type.GetGenericTypeDefinition(), out converterType)))
			{
				if ((typeShape?.GetAssociatedTypeShape(converterType) as IObjectTypeShape)?.GetDefaultConstructor() is Func<object> factory)
				{
					converter = (MessagePackConverter)factory();
				}
				else if (!converterType.IsGenericTypeDefinition)
				{
					// Try to find a constructor that takes a ConverterContext parameter
					ConstructorInfo? converterContextCtor = converterType.GetConstructor([typeof(ConverterContext)]);
					if (converterContextCtor is not null)
					{
						ConverterContext context = new(this, shapeProvider, this.PreserveReferences);
						converter = (MessagePackConverter)converterContextCtor.Invoke([context]);
					}
					else
					{
						// Fall back to parameterless constructor
						converter = (MessagePackConverter)Activator.CreateInstance(converterType)!;
					}
				}
				else
				{
					throw new MessagePackSerializationException($"Unable to activate converter {converterType} for {type}. Did you forget to define the attribute [assembly: {nameof(TypeShapeExtensionAttribute)}({nameof(TypeShapeExtensionAttribute.AssociatedTypes)} = [typeof(dataType<>), typeof(converterType<>)])]?");
				}
			}
			else
			{
				ConverterContext context = new(this, shapeProvider, this.PreserveReferences);
				foreach (IMessagePackConverterFactory factory in configuration.ConverterFactories)
				{
					if ((converter = factory.CreateConverter(type, typeShape, context)) is not null)
					{
						break;
					}
				}
			}
		}

		if (converter is not null && configuration.PreserveReferences != ReferencePreservationMode.Off)
		{
			converter = ((IMessagePackConverterInternal)converter).WrapWithReferencePreservation();
		}

		return converter is not null;
	}

	/// <inheritdoc cref="DerivedTypeUnionCollection.TryGetDerivedTypeUnion(Type, out DerivedTypeUnion?)"/>
	internal bool TryGetDynamicUnion(Type baseType, [NotNullWhen(true)] out DerivedTypeUnion? union) => configuration.DerivedTypeUnions.TryGetDerivedTypeUnion(baseType, out union);

	/// <summary>
	/// Gets the property name that should be used when serializing a property.
	/// </summary>
	/// <param name="name">The original property name as given by <see cref="IPropertyShape"/>.</param>
	/// <param name="attributeProvider">The attribute provider for the property.</param>
	/// <returns>The serialized property name to use.</returns>
	internal string GetSerializedPropertyName(string name, IGenericCustomAttributeProvider? attributeProvider)
	{
		if (this.PropertyNamingPolicy is null)
		{
			return name;
		}

		// If the property was decorated with [PropertyShape(Name = "...")], do *not* meddle with the property name.
		if (attributeProvider?.GetCustomAttributes<PropertyShapeAttribute>(inherit: false).FirstOrDefault() is PropertyShapeAttribute { Name: not null })
		{
			return name;
		}

		return this.PropertyNamingPolicy.ConvertName(name);
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static MessagePackConverter<T> ThrowConverterError<T>(ConverterResult converter)
		=> throw converter.Error!.ThrowException();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private MessagePackConverter<T> GetOrAddConverterValueSlow<T>(ITypeShape<T> shape)
	{
		ConverterResult converter = this.GetOrAddConverter(shape);
		return (MessagePackConverter<T>)converter.ValueOrThrow;
	}

	/// <summary>
	/// Looks up (and caches) the converter for a shape that was not found in either level of the last-converter cache.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>The converter result.</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private ConverterResult AddConverter<T>(ITypeShape<T> shape)
	{
		ConverterResult converter = (ConverterResult)this.CachedConverters.GetOrAdd(shape)!;

		// Only successful results may be cached by shape association, because failure results
		// may be shared instances (e.g. static singletons) that serve more than one shape or cache.
		if (converter.Success)
		{
			converter.Shape = shape;
			this.lastConverters[TypeSlot<T>.Index] = converter;
			this.lastConverter = converter;
		}

		return converter;
	}

	/// <summary>
	/// Assigns a stable <see cref="lastConverters"/> slot to each data type.
	/// </summary>
	/// <typeparam name="T">The data type to be converted.</typeparam>
	/// <remarks>
	/// Slots are handed out by a monotonically increasing counter so that the types an application
	/// actually uses tend to land in distinct slots rather than colliding as hash codes may.
	/// A collision is harmless; it merely reduces the cache to a slower (but still correct) lookup.
	/// </remarks>
	private static class TypeSlot<T>
	{
		/// <summary>
		/// The index into <see cref="lastConverters"/> reserved for <typeparamref name="T"/>.
		/// </summary>
		internal static readonly int Index = (int)((uint)Interlocked.Increment(ref nextSlot) & (LastConverterSlotCount - 1));
	}
}
