// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A shape-based union that distinguishes object cases by their required properties.
/// </summary>
/// <remarks>
/// <para>
/// A case matches a map when every required serialized property name is present.
/// Extra properties do not reject a case; the case converter skips them or stores them in <see cref="UnusedDataPacket"/>.
/// When several cases match, the case whose required names are a strict superset of the others is chosen.
/// If no match is more specific, deserialization throws <see cref="MessagePackSerializationException"/>.
/// A case with no required properties is a catch-all: at most one is allowed, and it is chosen only when no other case matches.
/// </para>
/// <para>
/// The concrete base type is a case. An abstract or interface base is not a case and is not used as a fallback.
/// A <see cref="DerivedTypeShapeAttribute"/> hierarchy is read through its object shape, so runtime duck typing can replace that attribute.
/// Serializing a runtime type that was not registered throws <see cref="MessagePackSerializationException"/>.
/// Nil deserializes as <see langword="null"/>. Any other non-map value throws.
/// </para>
/// <para>
/// Required names come from the same members the object converter requires: required constructor parameters
/// (including <see langword="required"/> properties and <see cref="PropertyShapeAttribute.IsRequired"/>),
/// using each property's serialized name.
/// <see cref="MessagePackSerializer.PropertyNamingPolicy"/> is applied when the converter is built.
/// An explicit <see cref="PropertyShapeAttribute.Name"/> is not rewritten by the policy.
/// Names are compared ordinally.
/// A <see cref="MessagePackSerializer.SerializeDefaultValues"/> policy that omits a required member can make a case fail to match, because a missing name is missing evidence.
/// The constructor rejects identical required-name sets and more than one catch-all using shape names.
/// The same check runs again with serialized names when the converter is built, so a naming policy that collapses two cases fails before any value is read.
/// </para>
/// <para>
/// Duck typing reads and writes name-keyed maps. Tuples, <see cref="KeyAttribute"/> layouts, and
/// <see cref="MessagePackSerializer.PerfOverSchemaStability"/> are not supported.
/// Identifying a case may buffer the value. This API is experimental.
/// </para>
/// </remarks>
[Experimental("DuckTyping")]
public class DerivedTypeDuckTyping : DerivedTypeUnion
{
	private readonly ITypeShape baseShape;
	private readonly ReadOnlyMemory<ITypeShape> derivedTypeShapes;

	/// <summary>
	/// Initializes a new instance of the <see cref="DerivedTypeDuckTyping"/> class.
	/// </summary>
	/// <param name="baseShape">The shape of the base type. A concrete base is one of the cases.</param>
	/// <param name="derivedTypeShapes">The shapes of the derived types. Each must be a concrete object assignable to <paramref name="baseShape"/>.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="baseShape"/> or a derived shape is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">
	/// Thrown when fewer than two concrete types are registered, a derived type is abstract, an interface, not assignable to the base, duplicated, or not an object,
	/// or when two cases require the same shape property names or more than one case has no required properties.
	/// </exception>
	public DerivedTypeDuckTyping(ITypeShape baseShape, params ReadOnlySpan<ITypeShape> derivedTypeShapes)
	{
		Requires.NotNull(baseShape);
		this.baseShape = baseShape;
		this.derivedTypeShapes = derivedTypeShapes.ToArray();

		List<ITypeShape> cases = new(derivedTypeShapes.Length + 1);
		HashSet<Type> seen = [];
		if (baseShape.Type is { IsAbstract: false, IsInterface: false })
		{
			seen.Add(baseShape.Type);
			cases.Add(baseShape);
		}

		foreach (ITypeShape derivedTypeShape in derivedTypeShapes)
		{
			if (derivedTypeShape is null)
			{
				throw new ArgumentNullException(nameof(derivedTypeShapes));
			}

			Requires.Argument(derivedTypeShape.Type is { IsAbstract: false, IsInterface: false }, nameof(derivedTypeShapes), $"Derived types must be concrete, but {derivedTypeShape.Type.FullName} is not.");
			Requires.Argument(baseShape.Type.IsAssignableFrom(derivedTypeShape.Type), nameof(derivedTypeShapes), $"Type '{derivedTypeShape.Type.FullName}' is not assignable to base type '{baseShape.Type.FullName}'.");
			Requires.Argument(seen.Add(derivedTypeShape.Type), nameof(derivedTypeShapes), $"The type '{derivedTypeShape.Type.FullName}' is registered more than once.");
			cases.Add(derivedTypeShape);
		}

		Requires.Argument(cases.Count > 1, nameof(derivedTypeShapes), "At least two concrete types must be provided. The base type only counts if it is concrete.");

		ObjectCaseClassifier.Case[] classified = new ObjectCaseClassifier.Case[cases.Count];
		for (int i = 0; i < cases.Count; i++)
		{
			string[] names = DuckTypingRequiredMembers.GetRequiredNames(cases[i], static property => property.Name);
			classified[i] = new ObjectCaseClassifier.Case(cases[i].Type, names);
		}

		// Shape names, before the serializer's naming policy. The converter repeats this check with serialized names.
		_ = new ObjectCaseClassifier(baseShape.Type, classified, namingPolicyApplied: false);
	}

	/// <inheritdoc/>
	public override Type BaseType => this.baseShape.Type;

	/// <summary>
	/// Gets the shape of the base type.
	/// </summary>
	internal ITypeShape BaseShape => this.baseShape;

	/// <summary>
	/// Gets the derived type shapes, not including the base type.
	/// </summary>
	internal ReadOnlyMemory<ITypeShape> DerivedShapes => this.derivedTypeShapes;

	/// <inheritdoc/>
	internal override void InternalDerivationsOnly() => throw new NotImplementedException();
}
