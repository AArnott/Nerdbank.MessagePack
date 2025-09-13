// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// A class applied to a custom data type to prescribe a custom <see cref="MessagePackConverter{T}"/>
/// implementation to use for serialization.
/// </summary>
/// <param name="converterType">
/// A type that implements <see cref="MessagePackConverter{T}"/>
/// where <c>T</c> is a type argument matching the type to which this attribute is applied.
/// </param>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
[AssociatedTypeAttribute(nameof(converterType), TypeShapeRequirements.Constructor)]
public class MessagePackConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType) : Attribute
{
	/// <summary>
	/// Gets the type that implements <see cref="MessagePackConverter{T}"/>.
	/// </summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type ConverterType => converterType;
}
