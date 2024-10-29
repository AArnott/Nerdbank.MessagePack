// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Specifies an ordinal key that may be used when the object serializes its properties as an array of values instead of a map of property names to values.
/// </summary>
/// <remarks>
/// Once this key is applied to <em>any</em> field or property of a type,
/// it must be applied to <em>all</em> fields and properties of that type that are candidates for serialization.
/// Reassigning a key to a different member of the same type is an unversioned breaking change in the serialization schema and should be avoided
/// when reading or writing across versions is supported.
/// Unassigned indexes in the array will be left as nil during serialization and ignored during deserialization.
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
