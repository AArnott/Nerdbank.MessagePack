// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack;

/// <summary>
/// Specifies a comparer to be used for the keyed collections on the decorated member.
/// </summary>
/// <example>
/// <para>
/// The following snippet demonstrates a common pattern for properly using this attribute.
/// </para>
/// <code source="../../samples/cs/CustomizingSerialization.cs" region="CustomComparerOnMember" lang="C#" />
/// <para>
/// Notice how the comparer is specified both by attribute (which influences deserialized instances) and
/// in the property initializer (which influences new instantiations created by user code).
/// </para>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public class UseComparerAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UseComparerAttribute"/> class with the specified comparer type.
	/// </summary>
	/// <param name="comparerType">The type of the comparer to be used. This type must implement the <see cref="IComparer{T}"/> or <see cref="IEqualityComparer{T}"/> interface, as appropriate for the decorated collection.</param>
	public UseComparerAttribute(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type comparerType)
	{
		this.ComparerType = comparerType;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UseComparerAttribute"/> class with the specified comparer source type
	/// and member name.
	/// </summary>
	/// <param name="comparerSource">The type that provides the comparer.</param>
	/// <param name="memberName">
	/// The name of the member within the <paramref name="comparerSource"/> type that is used to obtain the comparer.
	/// The member must be a public property, and may be a static or an instance member.
	/// If an instance member, the <paramref name="comparerSource"/> type must have a public default constructor.
	/// </param>
	public UseComparerAttribute(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type comparerSource,
		string memberName)
	{
		this.ComparerType = comparerSource;
		this.MemberName = memberName;
	}

	/// <summary>
	/// Gets the type that provides the comparer to be used for the decorated member.
	/// </summary>
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public Type ComparerType { get; }

	/// <summary>
	/// Gets the name of the member on <see cref="ComparerType"/> that provides the comparer, if applicable.
	/// </summary>
	/// <remarks>
	/// If <see langword="null" />, the <see cref="ComparerType"/> itself is the comparer.
	/// </remarks>
	public string? MemberName { get; }
}
