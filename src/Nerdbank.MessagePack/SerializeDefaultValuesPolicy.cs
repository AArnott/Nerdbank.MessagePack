// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer;

/// <summary>
/// Specifies the policy for serializing default values.
/// </summary>
[Flags]
public enum SerializeDefaultValuesPolicy
{
	/// <summary>
	/// Do not serialize any default values.
	/// </summary>
	Never = 0x0,

	/// <summary>
	/// Serialize default values when they are required by the schema.
	/// </summary>
	/// <remarks>
	/// Properties are considered required when they have the <c>required</c> modifier on them
	/// or they appear as parameters in the deserializing constructor without a default value specified.
	/// </remarks>
	Required = 0x1,

	/// <summary>
	/// Serialize default values for value types.
	/// </summary>
	/// <remarks>
	/// This means values such as <c>0</c> and <see langword="false" /> will be serialized,
	/// but <see langword="null"/> will not be serialized.
	/// </remarks>
	ValueTypes = 0x2,

	/// <summary>
	/// Serialize default values for reference types.
	/// </summary>
	/// <remarks>
	/// This means that properties with <see langword="null"/> values will be serialized.
	/// </remarks>
	ReferenceTypes = 0x4,

	/// <summary>
	/// Serialize all properties, regardless of their values.
	/// </summary>
	Always = Required | ValueTypes | ReferenceTypes,
}
