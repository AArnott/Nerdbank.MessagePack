// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Nerdbank.MessagePack;

/// <summary>
/// An immutable configuration object that describes how to serialize and deserialize objects.
/// </summary>
internal record SerializerConfiguration
{
	/// <summary>
	/// Gets the default configuration.
	/// </summary>
	public static readonly SerializerConfiguration Default = new();

	private ConverterCache? converterCache;
	private ConverterCollection converters = [];
	private ConverterTypeCollection converterTypes = [];
	private ImmutableArray<IMessagePackConverterFactory> converterFactories = [];
	private DerivedTypeMappingCollection derivedTypeMappings = [];
	private bool perfOverSchemaStability;
	private bool serializeEnumValuesByName;
	private ReferencePreservationMode preserveReferences;
	private MessagePackNamingPolicy? propertyNamingPolicy;
	private bool internStrings;
	private bool disableHardwareAcceleration;
	private SerializeDefaultValuesPolicy serializeDefaultValues = SerializeDefaultValuesPolicy.Required;
	private DeserializeDefaultValuesPolicy deserializeDefaultValues = DeserializeDefaultValuesPolicy.Default;
	private LibraryReservedMessagePackExtensionTypeCode libraryExtensionTypeCodes = LibraryReservedMessagePackExtensionTypeCode.Default;
	private IComparerProvider? comparerProvider = SecureComparerProvider.Default;
#if NET
	private MultiDimensionalArrayFormat multiDimensionalArrayFormat = MultiDimensionalArrayFormat.Nested;
#endif

	private SerializerConfiguration()
	{
	}

	/// <summary>
	/// Gets an array of <see cref="MessagePackConverter{T}"/> objects that should be used for their designated data types.
	/// </summary>
	/// <remarks>
	/// Converters in this collection are searched first when creating a converter for a given type, before <see cref="ConverterTypes"/> and <see cref="ConverterFactories"/>.
	/// </remarks>
	public ConverterCollection Converters
	{
		get => this.converters;
		init => this.ChangeSetting(ref this.converters, value);
	}

	/// <summary>
	/// Gets a collection of <see cref="MessagePackConverter{T}"/> types that should be used for their designated data types.
	/// </summary>
	/// <remarks>
	/// The types in this collection are searched after matching <see cref="Converters"/> and before <see cref="ConverterFactories"/> when creating a converter for a given type.
	/// </remarks>
	public ConverterTypeCollection ConverterTypes
	{
		get => this.converterTypes;
		init => this.ChangeSetting(ref this.converterTypes, value);
	}

	/// <summary>
	/// Gets an array of converter factories to consult when creating a converter for a given type.
	/// </summary>
	/// <remarks>
	/// Factories are the last resort for creating a custom converter, coming after <see cref="Converters"/> and <see cref="ConverterTypes"/>.
	/// </remarks>
	public ImmutableArray<IMessagePackConverterFactory> ConverterFactories
	{
		get => this.converterFactories;
		init => this.ChangeSetting(ref this.converterFactories, value);
	}

	/// <summary>
	/// Gets an array of <see cref="DerivedShapeMapping{TBase}"/> objects that add runtime insight into what derived
	/// types may appear in the serialized data for a given base type.
	/// </summary>
	public DerivedTypeMappingCollection DerivedTypeMappings
	{
		get => this.derivedTypeMappings;
		init => this.ChangeSetting(ref this.derivedTypeMappings, value);
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
	public bool PerfOverSchemaStability
	{
		get => this.perfOverSchemaStability;
		init => this.ChangeSetting(ref this.perfOverSchemaStability, value);
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
	public bool SerializeEnumValuesByName
	{
		get => this.serializeEnumValuesByName;
		init => this.ChangeSetting(ref this.serializeEnumValuesByName, value);
	}

	/// <summary>
	/// Gets a setting that determines how references to objects are preserved during serialization and deserialization.
	/// </summary>
	/// <value>
	/// The default value is <see cref="ReferencePreservationMode.Off" />
	/// because it requires no msgpack extensions, is compatible with all msgpack readers,
	/// adds no security considerations and is the most performant.
	/// </value>
	public ReferencePreservationMode PreserveReferences
	{
		get => this.preserveReferences;
		init => this.ChangeSetting(ref this.preserveReferences, value);
	}

	/// <summary>
	/// Gets the transformation function to apply to property names before serializing them.
	/// </summary>
	/// <value>
	/// The default value is null, indicating that property names should be persisted exactly as they are declared in .NET.
	/// </value>
	public MessagePackNamingPolicy? PropertyNamingPolicy
	{
		get => this.propertyNamingPolicy;
		init => this.ChangeSetting(ref this.propertyNamingPolicy, value);
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
	public bool InternStrings
	{
		get => this.internStrings;
		init => this.ChangeSetting(ref this.internStrings, value);
	}

	/// <summary>
	/// Gets a value indicating whether hardware accelerated converters should be avoided.
	/// </summary>
	public bool DisableHardwareAcceleration
	{
		get => this.disableHardwareAcceleration;
		init => this.ChangeSetting(ref this.disableHardwareAcceleration, value);
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
	/// <para>
	/// When using anything besides <see cref="SerializeDefaultValuesPolicy.Always"/>,
	/// avoid using member initializers to set default values without also using <see cref="System.ComponentModel.DefaultValueAttribute"/>.
	/// Otherwise round-trip serialization may not work as expected.
	/// For example a property declared as <c>public int Age = 18;</c> may be skipped during serialization when its value is 0 (because <c>default(int) == 0</c>),
	/// and upon deserialization, the value of Age will be 18 because no value for the property was provided.
	/// But simply adding <c>[DefaultValue(18)]</c> to that same property declaration allows the serializer to understand what value should be considered the default value,
	/// so that it is only skipped during serialization if the age was 18, allowing all values to be correctly round-tripped.
	/// </para>
	/// </remarks>
	public SerializeDefaultValuesPolicy SerializeDefaultValues
	{
		get => this.serializeDefaultValues;
		init => this.ChangeSetting(ref this.serializeDefaultValues, value);
	}

	/// <summary>
	/// Gets the policy concerning how to handle missing or <see langword="null" /> properties
	/// during deserialization.
	/// </summary>
	/// <value>The default value is <see cref="DeserializeDefaultValuesPolicy.Default"/>.</value>
	public DeserializeDefaultValuesPolicy DeserializeDefaultValues
	{
		get => this.deserializeDefaultValues;
		init => this.ChangeSetting(ref this.deserializeDefaultValues, value);
	}

	/// <summary>
	/// Gets the extension type codes to use for library-reserved extension types.
	/// </summary>
	/// <remarks>
	/// This property may be used to reassign the extension type codes for library-provided extension types
	/// in order to avoid conflicts with other libraries the application is using.
	/// </remarks>
	public LibraryReservedMessagePackExtensionTypeCode LibraryExtensionTypeCodes
	{
		get => this.libraryExtensionTypeCodes;
		init => this.ChangeSetting(ref this.libraryExtensionTypeCodes, value);
	}

	/// <summary>
	/// Gets the provider of <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/> instances
	/// to use when instantiating collections that support them.
	/// </summary>
	/// <value>
	/// The default value is an instance of <see cref="SecureComparerProvider"/>,
	/// which provides hash collision resistance for improved security when deserializing untrusted data.
	/// </value>
	/// <remarks>
	/// This property may be cleared from its secure default for improved performance when deserializing trusted data.
	/// </remarks>
	/// <seealso href="../docs/security.html#hash-collisions">Security: hash collisions</seealso>
	public IComparerProvider? ComparerProvider
	{
		get => this.comparerProvider;
		init => this.ChangeSetting(ref this.comparerProvider, value);
	}

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
	/// Gets the <see cref="Nerdbank.MessagePack.ConverterCache"/> object based on this configuration.
	/// </summary>
	internal ConverterCache ConverterCache => this.converterCache ??= new ConverterCache(this);

	private bool ChangeSetting<T>(ref T location, T value)
	{
		if (!EqualityComparer<T>.Default.Equals(location, value))
		{
			this.converterCache = null;
			location = value;
			return true;
		}

		return false;
	}
}
