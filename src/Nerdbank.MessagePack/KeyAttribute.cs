// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer;

/// <summary>
/// Specifies an ordinal key that may be used when the object serializes its properties for a faster and/or more compact binary representation.
/// </summary>
/// <remarks>
/// <para>
/// Once this key is applied to <em>any</em> field or property of a type,
/// it must be applied to <em>all</em> fields and properties of that type that are candidates for serialization.
/// Reassigning a key to a different member of the same type is an unversioned breaking change in the serialization schema and should be avoided
/// when reading or writing across versions is supported.
/// Unassigned indexes in the array will be left as nil during serialization and ignored during deserialization.
/// </para>
/// <para>
/// An object that uses this attribute may be serialized as an array of values where the index given to this attribute becomes the index into the array for the value of the applied property,
/// or the object may be serialized as a map where the index given to this attribute provides the key instead of the property name, and the map value is set to the value of the property.
/// Whether an object serializes as a map or an array is determined at runtime.
/// When <see cref="SerializerBase.SerializeDefaultValues"/> is not <see cref="SerializeDefaultValuesPolicy.Always"/> and there are "holes" in the would-be array
/// (due to properties with default values or unused indexes), the map format will be chosen when a quick estimate determines that it will be a more compact representation
/// than an array with holes in it.
/// Deserializers should always be prepared for either the map or the array representation of the object.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class KeyAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KeyAttribute"/> class.
	/// </summary>
	/// <param name="index">The index into the array where the value of the annotated member will be stored.</param>
	public KeyAttribute(int index)
	{
		this.Index = index;
	}

	/// <summary>
	/// Gets the array index where the value of the annotated member will be stored.
	/// </summary>
	public int Index { get; }
}
