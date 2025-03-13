// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft;
using PolyType.Utilities;

namespace Nerdbank.MessagePack;

/// <summary>
/// Tracks all inputs to converter construction and caches the results of construction itself.
/// </summary>
/// <remarks>
/// This type offers something of an information barrier to converter construction.
/// The <see cref="StandardVisitor"/> only gets a reference to this object,
/// and this object does <em>not</em> have a reference to <see cref="MessagePackSerializer"/>.
/// This ensures that properties on <see cref="MessagePackSerializer"/> cannot serve as inputs to the converters.
/// Thus, the only properties that should reset the <see cref="cachedConverters"/> are those declared on this type.
/// </remarks>
internal record class ConverterCache
{
	/// <summary>
	/// A mapping of data types to their custom converters that were registered at runtime.
	/// </summary>
	private readonly ConcurrentDictionary<Type, object> userProvidedConverterObjects = new();

	/// <summary>
	/// A mapping of data types to their custom converter types that were registered at runtime.
	/// </summary>
	private readonly ConcurrentDictionary<Type, Type> userProvidedConverterTypes = new();

	private readonly ConcurrentDictionary<Type, IDerivedTypeMapping> userProvidedKnownSubTypes = new();

	/// <summary>
	/// An optimization that avoids the dictionary lookup to start serialization
	/// when the caller repeatedly serializes the same type.
	/// </summary>
	private object? lastConverter;

	private MultiProviderTypeCache? cachedConverters;

#if NET
	private MultiDimensionalArrayFormat multiDimensionalArrayFormat = MultiDimensionalArrayFormat.Nested;
#endif

	private bool preserveReferences;
	private bool serializeEnumValuesByName;
	private SerializeDefaultValuesPolicy serializeDefaultValues = SerializeDefaultValuesPolicy.Required;
	private bool internStrings;
	private bool disableHardwareAcceleration;
	private MessagePackNamingPolicy? propertyNamingPolicy;
	private bool perfOverStability;

#if NET

	/// <summary>
	/// Gets the format to use when serializing multi-dimensional arrays.
	/// </summary>
	internal MultiDimensionalArrayFormat MultiDimensionalArrayFormat
	{
		get => this.multiDimensionalArrayFormat;
		init => this.ChangeSetting(ref this.multiDimensionalArrayFormat, value);
	}
#endif

	/// <summary>
	/// Gets a value indicating whether to preserve reference equality when serializing objects.
	/// </summary>
	/// <value>The default value is <see langword="false" />.</value>
	/// <remarks>
	/// <para>
	/// When <see langword="false" />, if an object appears multiple times in a serialized object graph, it will be serialized at each location.
	/// This has two outcomes: redundant data leading to larger serialized payloads and the loss of reference equality when deserialized.
	/// This is the default behavior because it requires no msgpack extensions and is compatible with all msgpack readers.
	/// </para>
	/// <para>
	/// When <see langword="true"/>, every object is serialized normally the first time it appears in the object graph.
	/// Each subsequent type the object appears in the object graph, it is serialized as a reference to the first occurrence.
	/// This reference requires between 3-6 bytes of overhead per reference instead of whatever the object's by-value representation would have required.
	/// Upon deserialization, all objects that were shared across the object graph will also be shared across the deserialized object graph.
	/// Of course there will not be reference equality between the original and deserialized objects, but the deserialized objects will have reference equality with each other.
	/// This option utilizes a proprietary msgpack extension and can only be deserialized by libraries that understand this extension.
	/// There is a small perf penalty for this feature, but depending on the object graph it may turn out to improve performance due to avoiding redundant serializations.
	/// </para>
	/// <para>
	/// Reference cycles (where an object refers to itself or to another object that eventually refers back to it) are <em>not</em> supported in either mode.
	/// When this property is <see langword="true" />, an exception will be thrown when a cycle is detected.
	/// When this property is <see langword="false" />, a cycle will eventually result in a <see cref="StackOverflowException" /> being thrown.
	/// </para>
	/// </remarks>
	internal bool PreserveReferences
	{
		get => this.preserveReferences;
		init
		{
			if (this.ChangeSetting(ref this.preserveReferences, value))
			{
				// Extra steps must be taken when this property changes because
				// we apply this setting to user-provided converters as they are added.
				this.ReconfigureUserProvidedConverters();
			}
		}
	}

	/// <summary>
	/// Gets a value indicating whether enum values will be serialized by name rather than by their numeric value.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Serializing by name is a best effort.
	/// Most enums do not define a name for every possible value, and flags enums may have complicated string representations when multiple named enum elements are combined to form a value.
	/// When a simple string cannot be constructed for a given value, the numeric form is used.
	/// </para>
	/// <para>
	/// When deserializing enums by name, name matching is case <em>insensitive</em> unless the enum type defines multiple values with names that are only distinguished by case.
	/// </para>
	/// </remarks>
	internal bool SerializeEnumValuesByName
	{
		get => this.serializeEnumValuesByName;
		init => this.ChangeSetting(ref this.serializeEnumValuesByName, value);
	}

	/// <summary>
	/// Gets the policy concerning which properties to serialize though they are set to their default values.
	/// </summary>
	/// <value>The default value is <see cref="SerializeDefaultValuesPolicy.Required"/>, meaning that only required properties or properties with non-default values will be serialized.</value>
	/// <remarks>
	/// <para>
	/// By default, the serializer omits properties and fields that are set to their default values when serializing objects.
	/// This property can be used to override that behavior and serialize all properties and fields, regardless of their value.
	/// </para>
	/// <para>
	/// Objects that are serialized as arrays (i.e. types that use <see cref="KeyAttribute"/> on their members),
	/// have a limited ability to omit default values because the order of the elements in the array is significant.
	/// See the <see cref="KeyAttribute" /> documentation for details.
	/// </para>
	/// <para>
	/// Default values are assumed to be <c>default(TPropertyType)</c> except where overridden, as follows:
	/// <list type="bullet">
	///   <item><description>Primary constructor default parameter values. e.g. <c>record Person(int Age = 18)</c></description></item>
	///   <item><description>Properties or fields attributed with <see cref="System.ComponentModel.DefaultValueAttribute"/>. e.g. <c>[DefaultValue(18)] internal int Age { get; set; } = 18;</c></description></item>
	/// </list>
	/// </para>
	/// </remarks>
	internal SerializeDefaultValuesPolicy SerializeDefaultValues
	{
		get => this.serializeDefaultValues;
		init => this.ChangeSetting(ref this.serializeDefaultValues, value);
	}

	/// <summary>
	/// Gets a value indicating whether to intern strings during deserialization.
	/// </summary>
	/// <remarks>
	/// <para>
	/// String interning means that a string that appears multiple times (within a single deserialization or across many)
	/// in the msgpack data will be deserialized as the same <see cref="string"/> instance, reducing GC pressure.
	/// </para>
	/// <para>
	/// When enabled, all deserialized are retained with a weak reference, allowing them to be garbage collected
	/// while also being reusable for future deserializations as long as they are in memory.
	/// </para>
	/// <para>
	/// This feature has a positive impact on memory usage but may have a negative impact on performance due to searching
	/// through previously deserialized strings to find a match.
	/// If your application is performance sensitive, you should measure the impact of this feature on your application.
	/// </para>
	/// <para>
	/// This feature is orthogonal and complementary to <see cref="PreserveReferences"/>.
	/// Preserving references impacts the serialized result and can hurt interoperability if the other party is not using the same feature.
	/// Preserving references also does not guarantee that equal strings will be reused because the original serialization may have had
	/// multiple string objects for the same value, so deserialization would produce the same result.
	/// Preserving references alone will never reuse strings across top-level deserialization operations either.
	/// Interning strings however, has no impact on the serialized result and is always safe to use.
	/// Interning strings will guarantee string objects are reused within and across deserialization operations so long as their values are equal.
	/// The combination of the two features will ensure the most compact msgpack, and will produce faster deserialization times than string interning alone.
	/// Combining the two features also activates special behavior to ensure that serialization only writes a string once
	/// and references that string later in that same serialization, even if the equal strings were unique objects.
	/// </para>
	/// </remarks>
	internal bool InternStrings
	{
		get => this.internStrings;
		init => this.ChangeSetting(ref this.internStrings, value);
	}

	/// <summary>
	/// Gets the transformation function to apply to property names before serializing them.
	/// </summary>
	/// <value>
	/// The default value is null, indicating that property names should be persisted exactly as they are declared in .NET.
	/// </value>
	internal MessagePackNamingPolicy? PropertyNamingPolicy
	{
		get => this.propertyNamingPolicy;
		init => this.ChangeSetting(ref this.propertyNamingPolicy, value);
	}

	/// <summary>
	/// Gets a value indicating whether to boost performance
	/// using methods that may compromise the stability of the serialized schema.
	/// </summary>
	/// <value>The default value is <see langword="false" />.</value>
	/// <remarks>
	/// <para>
	/// This setting is intended for use in performance-sensitive scenarios where the serialized data
	/// will not be stored or shared with other systems, but rather is used in a single system live data
	/// such that the schema need not be stable between versions of the application.
	/// </para>
	/// <para>
	/// Examples of behavioral changes that may occur when this setting is <see langword="true" />:
	/// <list type="bullet">
	/// <item>All objects are serialized with an array of their values instead of maps that include their property names.</item>
	/// <item>Polymorphic type identifiers are always integers.</item>
	/// </list>
	/// </para>
	/// <para>
	/// In particular, the schema is liable to change when this property is <see langword="true"/> and:
	/// <list type="bullet">
	/// <item>Serialized members are added, removed or reordered within their declaring type.</item>
	/// <item>A <see cref="DerivedTypeShapeAttribute"/> is removed, or inserted before the last such attribute on a given type.</item>
	/// </list>
	/// </para>
	/// <para>
	/// Changing this property (either direction) is itself liable to alter the schema of the serialized data.
	/// </para>
	/// <para>
	/// Performance and schema stability can both be achieved at once by:
	/// <list type="bullet">
	/// <item>Using the <see cref="KeyAttribute"/> on all serialized properties.</item>
	/// <item>Specifying <see cref="DerivedTypeShapeAttribute.Tag"/> explicitly for all polymorphic types.</item>
	/// </list>
	/// </para>
	/// </remarks>
	internal bool PerfOverSchemaStability
	{
		get => this.perfOverStability;
		init => this.ChangeSetting(ref this.perfOverStability, value);
	}

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	internal bool DisableHardwareAcceleration
	{
		get => this.disableHardwareAcceleration;
		init => this.ChangeSetting(ref this.disableHardwareAcceleration, value);
	}

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
						if (!this.PreserveReferences)
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
	/// Registers a converter for use with this serializer.
	/// </summary>
	/// <typeparam name="T">The convertible type.</typeparam>
	/// <param name="converter">The converter.</param>
	/// <remarks>
	/// If a converter for the data type has already been cached, the new value takes its place.
	/// Custom converters should be registered before serializing anything on this
	/// instance of <see cref="MessagePackSerializer" />.
	/// </remarks>
	internal void RegisterConverter<T>(MessagePackConverter<T> converter)
	{
		Requires.NotNull(converter);
		this.OnChangingConfiguration();
		this.userProvidedConverterObjects[typeof(T)] = this.PreserveReferences
			? ((IMessagePackConverterInternal)converter).WrapWithReferencePreservation()
			: converter;
	}

	/// <summary>
	/// Registers a converter for use with this serializer.
	/// </summary>
	/// <param name="converterType">
	/// The type of the converter.
	/// This class must declare a public default constructor and derive from <see cref="MessagePackConverter{T}"/>.
	/// </param>
	/// <remarks>
	/// The assembly that declares the converter class should also declare a
	/// <see cref="TypeShapeExtensionAttribute"/> that describes the link from the data type converted to the converter itself.
	/// This is particularly important when the converter being registered is an open generic type that must be closed
	/// using type arguments on the data type to be converted.
	/// </remarks>
	internal void RegisterConverter([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType)
	{
		Requires.NotNull(converterType);
		Requires.Argument(converterType.IsClass && !converterType.IsAbstract, nameof(converterType), "Type must be a concrete class.");

		// Discover what the data type being converted is.
		Type? baseType = converterType.BaseType;
		while (baseType is not null && !(baseType.IsGenericType && typeof(MessagePackConverter<>).IsAssignableFrom(baseType.GetGenericTypeDefinition())))
		{
			baseType = baseType.BaseType;
		}

		Requires.Argument(baseType is not null, nameof(converterType), $"Type does not derive from MessagePackConverter<T>.");
		Type dataType = baseType.GetGenericArguments()[0];

		// If the data type has no generic type arguments, turn it into a proper generic type definition so we can find it later.
		if (dataType.GenericTypeArguments is [{ IsGenericParameter: true }, ..])
		{
			dataType = dataType.GetGenericTypeDefinition();
		}

		this.OnChangingConfiguration();
		this.userProvidedConverterTypes[dataType] = converterType;
	}

	/// <summary>
	/// Registers a known sub-type mapping for a base type.
	/// </summary>
	/// <typeparam name="TBase"><inheritdoc cref="DerivedTypeMapping{TBase}" path="/typeparam[@name='TBase']" /></typeparam>
	/// <param name="mapping">The mapping.</param>
	/// <remarks>
	/// <para>
	/// This method provides a runtime dynamic alternative to the otherwise simpler but static
	/// <see cref="DerivedTypeShapeAttribute"/>, enabling scenarios such as sub-types that are not known at compile time.
	/// </para>
	/// <para>
	/// This is also the only way to force the serialized schema to <em>support</em> sub-types in the future when
	/// no sub-types are defined yet, such that they can be added later without a schema-breaking change.
	/// </para>
	/// <para>
	/// A mapping provided for a given <typeparamref name="TBase"/> will completely replace any mapping from
	/// <see cref="DerivedTypeShapeAttribute"/> attributes that may be applied to that same <typeparamref name="TBase"/>.
	/// </para>
	/// </remarks>
	internal void RegisterDerivedTypes<TBase>(DerivedTypeMapping<TBase> mapping)
	{
		Requires.NotNull(mapping);
		this.OnChangingConfiguration();
		this.userProvidedKnownSubTypes[typeof(TBase)] = mapping;
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
	/// <param name="converter">Receives the converter, if the user provided one (e.g. via <see cref="RegisterConverter{T}(MessagePackConverter{T})"/>.</param>
	/// <returns>A value indicating whether a customer converter exists.</returns>
	internal bool TryGetUserDefinedConverter<T>(ITypeShape<T> typeShape, [NotNullWhen(true)] out MessagePackConverter<T>? converter)
	{
		if (this.userProvidedConverterObjects.TryGetValue(typeof(T), out object? value))
		{
			converter = (MessagePackConverter<T>)value;
			return true;
		}

		if (this.userProvidedConverterTypes.TryGetValue(typeof(T), out Type? converterType) ||
			(typeof(T).IsGenericType && this.userProvidedConverterTypes.TryGetValue(typeof(T).GetGenericTypeDefinition(), out converterType)))
		{
			if (typeShape.GetAssociatedTypeFactory(converterType) is Func<object> factory)
			{
				converter = (MessagePackConverter<T>)factory();
				return true;
			}
			else
			{
				throw new MessagePackSerializationException($"Unable to activate converter {converterType} for {typeShape.Type}. Did you forget to define the attribute [assembly: {nameof(TypeShapeExtensionAttribute)}({nameof(TypeShapeExtensionAttribute.AssociatedTypes)} = [typeof(dataType<>), typeof(converterType<>)])]?");
			}
		}

		converter = default;
		return false;
	}

	/// <summary>
	/// Gets the runtime registered sub-types for a given base type, if any.
	/// </summary>
	/// <param name="baseType">The base type.</param>
	/// <param name="subTypes">If sub-types are registered, receives the mapping of those sub-types to their aliases.</param>
	/// <returns><see langword="true" /> if sub-types are registered; <see langword="false" /> otherwise.</returns>
	internal bool TryGetDynamicSubTypes(Type baseType, [NotNullWhen(true)] out IReadOnlyDictionary<DerivedTypeIdentifier, ITypeShape>? subTypes)
	{
		if (this.userProvidedKnownSubTypes.TryGetValue(baseType, out IDerivedTypeMapping? mapping))
		{
			subTypes = mapping.CreateDerivedTypesMapping();
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

	/// <summary>
	/// Throws <see cref="InvalidOperationException"/> if this object should not be mutated any more
	/// (because serializations have already happened, so mutating again can lead to unpredictable behavior).
	/// </summary>
	private void OnChangingConfiguration()
	{
		// Once we start building converters, they may read from any properties set on this object.
		// If the properties on this object are changed, we necessarily must drop all cached converters and rebuild.
		// Even if this cache had a Clear method, we do *not* use it since the cache may still be in use by other
		// instances of this record.
		this.cachedConverters = null;
		this.lastConverter = null;
	}

	private void ReconfigureUserProvidedConverters()
	{
		foreach (KeyValuePair<Type, object> pair in this.userProvidedConverterObjects)
		{
			IMessagePackConverterInternal converter = (IMessagePackConverterInternal)pair.Value;
			this.userProvidedConverterObjects[pair.Key] = this.PreserveReferences ? converter.WrapWithReferencePreservation() : converter.UnwrapReferencePreservation();
		}
	}

	private bool ChangeSetting<T>(ref T location, T value)
	{
		if (!EqualityComparer<T>.Default.Equals(location, value))
		{
			this.OnChangingConfiguration();
			location = value;
			return true;
		}

		return false;
	}
}
