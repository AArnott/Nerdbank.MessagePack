// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Diagnostics;

namespace Nerdbank.MessagePack;

/// <summary>
/// A non-generic interface that allows access to the members of the generic attributes that implement it.
/// </summary>
internal interface IKnownSubTypeAttribute
{
	/// <summary>
	/// Gets a value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.
	/// </summary>
	int Alias { get; }

	/// <summary>
	/// Gets the shape that describes the subtype.
	/// </summary>
	ITypeShape Shape { get; }
}

/// <summary>
/// Specifies that where the class to which this attribute is applied is the declared type in an object graph
/// that certain derived types are recorded in the serialized data as well and allowed to be deserialized back
/// as their derived types.
/// </summary>
/// <typeparam name="TSubType">A class derived from the one to which this attribute is affixed.</typeparam>
/// <typeparam name="TShapeProvider">The class that serves as the shape provider for <typeparamref name="TSubType"/>.</typeparam>
/// <param name="alias">A value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.</param>
/// <remarks>
/// <para>
/// A type with one or more of these attributes applied serializes to a different schema than the same type
/// without any attributes applied. The serialized data will include a special header that indicates the runtime type.
/// Consider version compatibility issues when adding the first or removing the last attribute from a type.
/// </para>
/// <para>
/// Each type referenced by this attribute must have <see cref="GenerateShapeAttribute"/> applied to it or a witness class.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class KnownSubTypeAttribute<TSubType, TShapeProvider>(int alias) : Attribute, IKnownSubTypeAttribute
	where TShapeProvider : IShapeable<TSubType>
{
	/// <inheritdoc/>
	public int Alias => alias;

	/// <inheritdoc/>
	ITypeShape IKnownSubTypeAttribute.Shape => TShapeProvider.GetShape();

	/// <summary>
	/// Gets the value for the <see cref="DebuggerDisplayAttribute"/>.
	/// </summary>
	private protected string DebuggerDisplay => $"Union: {this.Alias}, {typeof(TSubType).Name}";
}

/// <inheritdoc cref="KnownSubTypeAttribute{TSubType, TShapeProvider}"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class KnownSubTypeAttribute<TSubType>(int alias) : KnownSubTypeAttribute<TSubType, TSubType>(alias)
	where TSubType : IShapeable<TSubType>
{
}

/// <summary>
/// A non-generic type for getting the name of the attribute, for use in error messages.
/// </summary>
internal static class KnownSubTypeAttribute
{
	/// <summary>
	/// The name of the <see cref="KnownSubTypeAttribute"/> type.
	/// </summary>
	internal const string TypeName = nameof(KnownSubTypeAttribute);
}
