﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
	/// An optimization that avoids the dictionary lookup to start serialization
	/// when the caller repeatedly serializes the same type.
	/// </summary>
	private object? lastConverter;

	private MultiProviderTypeCache? cachedConverters;

#if NET
	/// <inheritdoc cref="SerializerConfiguration.MultiDimensionalArrayFormat"/>
	internal MultiDimensionalArrayFormat MultiDimensionalArrayFormat => configuration.MultiDimensionalArrayFormat;
#endif

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

	/// <inheritdoc cref="SerializerConfiguration.DisableHardwareAcceleration"/>
	internal bool DisableHardwareAcceleration => configuration.DisableHardwareAcceleration;

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

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The data type to convert.</typeparam>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal MessagePackConverter<T> GetOrAddConverter<T>(ITypeShape<T> shape)
		=> (MessagePackConverter<T>)(this.lastConverter is MessagePackConverter<T> lastConverter ? lastConverter : (this.lastConverter = this.CachedConverters.GetOrAdd(shape)!));

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="shape">The shape of the type to convert.</param>
	/// <returns>A msgpack converter.</returns>
	internal MessagePackConverter GetOrAddConverter(ITypeShape shape)
		=> (MessagePackConverter)this.CachedConverters.GetOrAdd(shape)!;

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <typeparam name="T">The type to convert.</typeparam>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal MessagePackConverter<T> GetOrAddConverter<T>(ITypeShapeProvider provider)
		=> (MessagePackConverter<T>)this.CachedConverters.GetOrAddOrThrow(typeof(T), provider);

	/// <summary>
	/// Gets a converter for the given type shape.
	/// An existing converter is reused if one is found in the cache.
	/// If a converter must be created, it is added to the cache for lookup next time.
	/// </summary>
	/// <param name="type">The type to convert.</param>
	/// <param name="provider">The type shape provider.</param>
	/// <returns>A msgpack converter.</returns>
	internal IMessagePackConverterInternal GetOrAddConverter(Type type, ITypeShapeProvider provider)
		=> (IMessagePackConverterInternal)this.CachedConverters.GetOrAddOrThrow(type, provider);

	/// <summary>
	/// Gets a user-defined converter for the specified type if one is available.
	/// </summary>
	/// <typeparam name="T">The data type for which a custom converter is desired.</typeparam>
	/// <param name="typeShape">The shape of the data type that requires a converter.</param>
	/// <param name="converter">Receives the converter, if the user provided one.</param>
	/// <returns>A value indicating whether a customer converter exists.</returns>
	internal bool TryGetUserDefinedConverter<T>(ITypeShape<T> typeShape, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		converter = null;
		if (!configuration.Converters.TryGetConverter(out converter))
		{
			if (configuration.ConverterTypes.TryGetConverterType(typeof(T), out Type? converterType) ||
				(typeof(T).IsGenericType && configuration.ConverterTypes.TryGetConverterType(typeof(T).GetGenericTypeDefinition(), out converterType)))
			{
				if ((typeShape.GetAssociatedTypeShape(converterType) as IObjectTypeShape)?.GetDefaultConstructor() is Func<object> factory)
				{
					converter = (MessagePackConverter<T>)factory();
				}
				else
				{
					throw new MessagePackSerializationException($"Unable to activate converter {converterType} for {typeShape.Type}. Did you forget to define the attribute [assembly: {nameof(TypeShapeExtensionAttribute)}({nameof(TypeShapeExtensionAttribute.AssociatedTypes)} = [typeof(dataType<>), typeof(converterType<>)])]?");
				}
			}
			else
			{
				foreach (IMessagePackConverterFactory factory in configuration.ConverterFactories)
				{
					if ((converter = factory.CreateConverter<T>(typeShape)) is not null)
					{
						break;
					}
				}
			}
		}

		if (converter is not null && configuration.PreserveReferences != ReferencePreservationMode.Off)
		{
			converter = converter.WrapWithReferencePreservation();
		}

		return converter is not null;
	}

	/// <summary>
	/// Gets the runtime registered sub-types for a given base type, if any.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="subTypes">If sub-types are registered, receives the mapping of those sub-types to their aliases.</param>
	/// <returns><see langword="true" /> if sub-types are registered; <see langword="false" /> otherwise.</returns>
	internal bool TryGetDynamicSubTypes(Type baseType, [NotNullWhen(true)] out IReadOnlyDictionary<DerivedTypeIdentifier, ITypeShape>? subTypes)
	{
		if (configuration.DerivedTypeMappings.TryGetDerivedTypeMapping(baseType, out FrozenDictionary<DerivedTypeIdentifier, ITypeShape>? mapping))
		{
			subTypes = mapping;
			return true;
		}

		subTypes = null;
		return false;
	}

	/// <summary>
	/// Gets the property name that should be used when serializing a property.
	/// </summary>
	/// <param name="name">The original property name as given by <see cref="IPropertyShape"/>.</param>
	/// <param name="attributeProvider">The attribute provider for the property.</param>
	/// <returns>The serialized property name to use.</returns>
	internal string GetSerializedPropertyName(string name, ICustomAttributeProvider? attributeProvider)
	{
		if (this.PropertyNamingPolicy is null)
		{
			return name;
		}

		// If the property was decorated with [PropertyShape(Name = "...")], do *not* meddle with the property name.
		if (attributeProvider?.GetCustomAttributes(typeof(PropertyShapeAttribute), false).FirstOrDefault() is PropertyShapeAttribute { Name: not null })
		{
			return name;
		}

		return this.PropertyNamingPolicy.ConvertName(name);
	}
}
