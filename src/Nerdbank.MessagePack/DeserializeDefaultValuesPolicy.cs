// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Enumerates policies that control how deserialization handles default values.
/// </summary>
[Flags]
public enum DeserializeDefaultValuesPolicy
{
	/// <summary>
	/// Deserialization will fail if any required properties have no specified values in the data stream,
	/// or when <see langword="null" /> values are encountered for non-nullable properties.
	/// </summary>
	/// <remarks>
	/// The non-nullability enforcement by the deserializer is limited to parameters, properties and fields
	/// that are <em>not</em> typed as generic type parameters.
	/// Collection elements (e.g. arrays, lists, or dictionary elements) that would not allow <see langword="null" /> values
	/// based on nullable ref annotations are not covered by this guarantee.
	/// After deserializing an object graph, you should be cautious that (for example) a <c>List&lt;string&gt;</c>
	/// may contain <see langword="null" /> elements even though the type argument is not <c>string?</c>.
	/// </remarks>
	Default = 0x0,

	/// <summary>
	/// Allow a <see langword="null" /> value to be deserialized into a non-nullable property.
	/// </summary>
	AllowNullValuesForNonNullableProperties = 0x1,

	/// <summary>
	/// Allows deserialization to succeed even if required properties are not specified in the data stream.
	/// These will be left at their default values.
	/// </summary>
	/// <remarks>
	/// This flag includes <see cref="AllowNullValuesForNonNullableProperties" />,
	/// since accepting missing values will tend to leave reference typed properties
	/// at their default <see langword="null" /> values.
	/// </remarks>
	AllowMissingValuesForRequiredProperties = 0x2 | AllowNullValuesForNonNullableProperties,
}
