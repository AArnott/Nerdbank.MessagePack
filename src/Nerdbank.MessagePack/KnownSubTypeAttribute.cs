// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Nerdbank.MessagePack;

/// <summary>
/// Specifies that where the class to which this attribute is applied is the declared type in an object graph
/// that certain derived types are recorded in the serialized data as well and allowed to be deserialized back
/// as their derived types.
/// </summary>
/// <remarks>
/// <para>
/// A type with one or more of these attributes applied serializes to a different schema than the same type
/// without any attributes applied. The serialized data will include a special header that indicates the runtime type.
/// Consider version compatibility issues when adding the first or removing the last attribute from a type.
/// </para>
/// <para>
/// Each type referenced by this attribute must have <see cref="GenerateShapeAttribute"/> applied to it
/// when using that attribute on the root of the serialization tree.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
[DebuggerDisplay($"Union: {{{nameof(Alias)}}}, {{{nameof(SubType.Name)},nq}}")]
public class KnownSubTypeAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KnownSubTypeAttribute"/> class.
	/// </summary>
	/// <param name="alias">A value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.</param>
	/// <param name="subType">The class derived from the one to which this attribute is affixed.</param>
	public KnownSubTypeAttribute(int alias, Type subType)
	{
		this.Alias = alias;
		this.SubType = subType;
	}

	/// <summary>
	/// Gets a value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.
	/// </summary>
	public int Alias { get; }

	/// <summary>
	/// Gets the class derived from the one to which this attribute is affixed.
	/// </summary>
	public Type SubType { get; }
}
