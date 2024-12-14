// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Diagnostics;

namespace Nerdbank.MessagePack;

#if NET

/// <summary>
/// Specifies that where the class to which this attribute is applied is the declared type in an object graph
/// that certain derived types are recorded in the serialized data as well and allowed to be deserialized back
/// as their derived types.
/// </summary>
/// <typeparam name="TSubType">A class derived from the one to which this attribute is affixed.</typeparam>
/// <typeparam name="TShapeProvider">The class that serves as the shape provider for <typeparamref name="TSubType"/>.</typeparam>
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
public class KnownSubTypeAttribute<TSubType, TShapeProvider> : KnownSubTypeAttribute
	where TShapeProvider : IShapeable<TSubType>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KnownSubTypeAttribute{TSubType, TShapeProvider}"/> class.
	/// </summary>
	/// <param name="alias"><inheritdoc cref="KnownSubTypeAttribute(int, Type)" path="/param[@name='alias']" /></param>
	public KnownSubTypeAttribute(int alias)
#pragma warning disable CS0618 // Type or member is obsolete
		: base(alias, typeof(TSubType))
#pragma warning restore CS0618 // Type or member is obsolete
	{
	}

	/// <inheritdoc cref="KnownSubTypeAttribute{TSubType, TShapeProvider}.KnownSubTypeAttribute(int)" />
	public KnownSubTypeAttribute(string alias)
#pragma warning disable CS0618 // Type or member is obsolete
		: base(alias, typeof(TSubType))
#pragma warning restore CS0618 // Type or member is obsolete
	{
	}

	/// <inheritdoc/>
	public override ITypeShape? Shape => TShapeProvider.GetShape();

	/// <summary>
	/// Gets the value for the <see cref="DebuggerDisplayAttribute"/>.
	/// </summary>
	private protected string DebuggerDisplay => $"Union: {this.Alias}, {typeof(TSubType).Name}";
}

/// <inheritdoc cref="KnownSubTypeAttribute{TSubType, TShapeProvider}"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
[DebuggerDisplay($"{{{nameof(DebuggerDisplay)},nq}}")]
public class KnownSubTypeAttribute<TSubType> : KnownSubTypeAttribute<TSubType, TSubType>
	where TSubType : IShapeable<TSubType>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="KnownSubTypeAttribute{TSubType}"/> class.
	/// </summary>
	/// <param name="alias"><inheritdoc cref="KnownSubTypeAttribute(int, Type)" path="/param[@name='alias']" /></param>
	public KnownSubTypeAttribute(int alias)
		: base(alias)
	{
	}

	/// <inheritdoc cref="KnownSubTypeAttribute{TSubType}.KnownSubTypeAttribute(int)" />
	public KnownSubTypeAttribute(string alias)
		: base(alias)
	{
	}
}

#endif

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
/// Each type referenced by this attribute must have <see cref="GenerateShapeAttribute"/> applied to it or a witness class.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
public class KnownSubTypeAttribute : Attribute
{
	/// <summary>
	/// The name of the <see cref="KnownSubTypeAttribute"/> type.
	/// </summary>
	internal const string TypeName = nameof(KnownSubTypeAttribute);

	/// <summary>
	/// Initializes a new instance of the <see cref="KnownSubTypeAttribute"/> class.
	/// </summary>
	/// <param name="alias">A value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.</param>
	/// <param name="subType">The derived-type that the <paramref name="alias"/> represents.</param>
#if NET
	[Obsolete("Use the generic version of this attribute instead.")]
#endif
	public KnownSubTypeAttribute(int alias, Type subType)
	{
		this.Alias = alias;
		this.SubType = subType;
	}

	/// <inheritdoc cref="KnownSubTypeAttribute(int, Type)" />
#if NET
	[Obsolete("Use the generic version of this attribute instead.")]
#endif
	public KnownSubTypeAttribute(string alias, Type subType)
	{
		this.Alias = alias;
		this.SubType = subType;
	}

	/// <summary>
	/// Gets the sub-type.
	/// </summary>
	public Type SubType { get; }

	/// <summary>
	/// Gets the shape that describes the subtype.
	/// </summary>
	public virtual ITypeShape? Shape => null;

	/// <summary>
	/// Gets a value that identifies the subtype in the serialized data. Must be unique among all the attributes applied to the same class.
	/// </summary>
	internal SubTypeAlias Alias { get; }
}
