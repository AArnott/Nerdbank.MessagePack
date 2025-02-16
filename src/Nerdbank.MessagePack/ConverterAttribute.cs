// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.PolySerializer;

/// <summary>
/// A class applied to a custom data type to prescribe a custom <see cref="Converter{T}"/>
/// implementation to use for serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ConverterAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ConverterAttribute"/> class.
	/// </summary>
	/// <param name="converterType">
	/// A type that implements <see cref="Converter{T}"/>
	/// where <c>T</c> is a type argument matching the type to which this attribute is applied.
	/// </param>
	public ConverterAttribute(Type converterType)
	{
		this.ConverterType = converterType;
	}

	/// <summary>
	/// Gets the type that implements <see cref="Converter{T}"/>.
	/// </summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type ConverterType { get; }
}
