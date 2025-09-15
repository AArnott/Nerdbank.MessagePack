// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A shape-based union that can distinguish between union cases
/// based on their structural characteristics rather than explicit aliases.
/// </summary>
/// <remarks>
/// <para>
/// This union strategy analyzes the provided type shapes to identify distinguishing characteristics such as:
/// </para>
/// <list type="bullet">
/// <item>Required properties that appear only in specific union cases</item>
/// <item>Properties with incompatible types across union cases</item>
/// </list>
/// <para>
/// The resulting converter does not use explicit type aliases and instead inspects the MessagePack structure
/// during deserialization to determine the appropriate union case to deserialize into.
/// </para>
/// <para>
/// Note that this approach may be slower than alias-based unions
/// as it may require buffering the entire value for analysis.
/// </para>
/// </remarks>
public class DerivedTypeDuckTyping : DerivedTypeUnion
{
	private readonly ITypeShape baseShape;
	private readonly IReadOnlyList<ITypeShape> derivedTypeShapes;

	/// <summary>
	/// Initializes a new instance of the <see cref="DerivedTypeDuckTyping"/> class.
	/// </summary>
	/// <param name="baseShape">The shape of the base type.</param>
	/// <param name="derivedTypeShapes">The shapes of the derived types.</param>
	public DerivedTypeDuckTyping(ITypeShape baseShape, params IReadOnlyList<ITypeShape> derivedTypeShapes)
	{
		Requires.NotNull(baseShape);
		Requires.NotNull(derivedTypeShapes);
		Requires.Argument(derivedTypeShapes is not [], nameof(derivedTypeShapes), "Non-empty list of union cases is required.");

		this.baseShape = baseShape;

		// TODO: make sure we have an immutable copy
		this.derivedTypeShapes = derivedTypeShapes;

		this.Rules = AnalyzeShapes(derivedTypeShapes) ?? throw new ArgumentException("The type shapes given do not include (enough) unique characteristics.");
	}

	/// <inheritdoc/>
	public override Type BaseType => this.baseShape.Type;

	/// <summary>
	/// Gets the shape of the base type.
	/// </summary>
	internal ITypeShape BaseShape => this.baseShape;

	/// <summary>
	/// Gets a list of the derived type shapes.
	/// </summary>
	internal IReadOnlyList<ITypeShape> DerivedShapes => this.derivedTypeShapes;

	/// <summary>
	/// Gets the rules by which duck type testing is performed.
	/// </summary>
	private ShapeBasedUnionMapping Rules { get; }

	/// <summary>
	/// Looks up the shape for a given type if it is part of this union.
	/// </summary>
	/// <param name="type">The type whose shape is sought.</param>
	/// <param name="shape">Receives the shape if one is found.</param>
	/// <returns>A value indicating whether a shape could be found.</returns>
	internal bool TryGetShape(Type type, [NotNullWhen(true)] out ITypeShape? shape) => this.Rules.TypeShapeMapping.TryGetValue(type, out shape);

#pragma warning disable NBMsgPack050 // Use ref parameters for ref structs (this acts as a peek reader).
	/// <inheritdoc cref="ShapeBasedUnionMapping.TryIdentifyType(ref MessagePackReader, SerializationContext, out ITypeShape?)"/>
	internal bool TryIdentifyType(MessagePackReader reader, SerializationContext context, [NotNullWhen(true)] out ITypeShape? typeShape) => this.Rules.TryIdentifyType(ref reader, context, out typeShape);
#pragma warning restore NBMsgPack050 // Use ref parameters for ref structs (this acts as a peek reader).

	/// <inheritdoc/>
	internal override void InternalDerivationsOnly() => throw new NotImplementedException();

	/// <summary>
	/// Analyzes a collection of type shapes to build a decision tree for distinguishing between them.
	/// </summary>
	/// <param name="typeShapes">The type shapes to analyze.</param>
	/// <returns>A <see cref="ShapeBasedUnionMapping"/> that can distinguish between the shapes, or null if no distinguishing characteristics are found.</returns>
	private static ShapeBasedUnionMapping? AnalyzeShapes(IReadOnlyList<ITypeShape> typeShapes)
	{
		if (typeShapes.Count < 2)
		{
			return null;
		}

		var builder = new ShapeAnalysisBuilder(typeShapes);
		return builder.BuildMapping();
	}

	/// <summary>
	/// Internal helper class for building shape analysis.
	/// </summary>
	private struct ShapeAnalysisBuilder
	{
		private readonly IReadOnlyList<ITypeShape> typeShapes;
		private readonly List<DistinguishingStep> steps = [];

		internal ShapeAnalysisBuilder(IReadOnlyList<ITypeShape> typeShapes)
		{
			this.typeShapes = typeShapes;
		}

		internal ShapeBasedUnionMapping? BuildMapping()
		{
			// Analyze for required property differences
			this.AnalyzeRequiredProperties();

			// Analyze for incompatible member types
			this.AnalyzeIncompatibleMemberTypes();

			if (this.steps.Count == 0)
			{
				return null; // No distinguishing characteristics found
			}

			var typeShapeMapping = this.typeShapes.ToFrozenDictionary(ts => ts.Type, ts => ts);
			return new ShapeBasedUnionMapping(this.steps, typeShapeMapping);
		}

		private void AnalyzeRequiredProperties()
		{
			Dictionary<string, (HashSet<Type> WithProperty, HashSet<Type> WithoutProperty)> propertyAnalysis = new(StringComparer.Ordinal);

			foreach (ITypeShape typeShape in this.typeShapes)
			{
				if (typeShape is IObjectTypeShape objectShape)
				{
					HashSet<string> propertiesInThisType = new(StringComparer.Ordinal);

					foreach (IPropertyShape property in objectShape.Properties)
					{
						propertiesInThisType.Add(property.Name);

						if (!propertyAnalysis.TryGetValue(property.Name, out (HashSet<Type> WithProperty, HashSet<Type> WithoutProperty) analysis))
						{
							analysis = ([], []);
							propertyAnalysis[property.Name] = analysis;
						}

						analysis.WithProperty.Add(typeShape.Type);
					}

					// Add this type to the "without" set for properties it doesn't have
					foreach ((string? propertyName, (HashSet<Type> WithProperty, HashSet<Type> WithoutProperty) analysis) in propertyAnalysis)
					{
						if (!propertiesInThisType.Contains(propertyName))
						{
							analysis.WithoutProperty.Add(typeShape.Type);
						}
					}
				}
			}

			// Create steps for properties that can distinguish between types
			foreach ((string? propertyName, (HashSet<Type>? withProperty, HashSet<Type>? withoutProperty)) in propertyAnalysis)
			{
				if (withProperty.Count > 0 && withoutProperty.Count > 0)
				{
					this.steps.Add(new RequiredPropertyStep(propertyName, withProperty, withoutProperty));
				}
			}
		}

		private void AnalyzeIncompatibleMemberTypes()
		{
			var propertyTypeAnalysis = new Dictionary<string, Dictionary<Type, HashSet<Type>>>();

			foreach (ITypeShape typeShape in this.typeShapes)
			{
				if (typeShape is IObjectTypeShape objectShape)
				{
					foreach (IPropertyShape property in objectShape.Properties)
					{
						if (!propertyTypeAnalysis.TryGetValue(property.Name, out Dictionary<Type, HashSet<Type>>? typeMapping))
						{
							typeMapping = [];
							propertyTypeAnalysis[property.Name] = typeMapping;
						}

						Type propertyType = property.PropertyType.Type;
						if (!typeMapping.TryGetValue(propertyType, out HashSet<Type>? typesWithThisPropertyType))
						{
							typesWithThisPropertyType = [];
							typeMapping[propertyType] = typesWithThisPropertyType;
						}

						typesWithThisPropertyType.Add(typeShape.Type);
					}
				}
			}

			// Create steps for properties with incompatible types
			foreach ((string? propertyName, Dictionary<Type, HashSet<Type>>? typeMapping) in propertyTypeAnalysis)
			{
				if (typeMapping.Keys.Count > 1)
				{
					// Multiple different types for same property name.
					this.steps.Add(new IncompatibleTypeStep(propertyName, typeMapping));
				}
			}
		}
	}

	/// <summary>
	/// Represents the analysis result for shape-based union distinction.
	/// </summary>
	private class ShapeBasedUnionMapping
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ShapeBasedUnionMapping"/> class.
		/// </summary>
		/// <param name="steps">The sequence of distinguishing steps to execute for type identification.</param>
		/// <param name="typeShapeMapping">The mapping from types to their corresponding type shapes.</param>
		internal ShapeBasedUnionMapping(IReadOnlyList<DistinguishingStep> steps, FrozenDictionary<Type, ITypeShape> typeShapeMapping)
		{
			this.Steps = steps;
			this.TypeShapeMapping = typeShapeMapping;
		}

		/// <summary>
		/// Gets the sequence of steps to execute to distinguish between types.
		/// </summary>
		internal IReadOnlyList<DistinguishingStep> Steps { get; }

		/// <summary>
		/// Gets the mapping from types to their shapes.
		/// </summary>
		internal FrozenDictionary<Type, ITypeShape> TypeShapeMapping { get; }

		/// <summary>
		/// Attempts to identify the runtime type based on MessagePack data.
		/// </summary>
		/// <param name="reader">A reader positioned at the start of the value to analyze.</param>
		/// <param name="context">The serialization context.</param>
		/// <param name="typeShape">The identified type shape, if successful.</param>
		/// <returns>True if exactly one matching type was identified; false otherwise.</returns>
		internal bool TryIdentifyType(ref MessagePackReader reader, SerializationContext context, [NotNullWhen(true)] out ITypeShape? typeShape)
		{
			var candidateTypes = new HashSet<Type>(this.TypeShapeMapping.Keys);

			foreach (DistinguishingStep step in this.Steps)
			{
				if (!step.Execute(ref reader, candidateTypes, context))
				{
					typeShape = null;
					return false;
				}

				if (candidateTypes.Count == 1)
				{
					typeShape = this.TypeShapeMapping[candidateTypes.First()];
					return true;
				}
			}

			// Multiple candidates remain
			typeShape = null;
			return false;
		}
	}

	/// <summary>
	/// Represents a single step in the type identification process.
	/// </summary>
	private abstract class DistinguishingStep
	{
		/// <summary>
		/// Executes this step against the MessagePack data to narrow down candidate types.
		/// </summary>
		/// <param name="reader">A reader positioned at the start of the value to analyze.</param>
		/// <param name="candidateTypes">The set of candidate types, which this method should modify to remove types that don't match.</param>
		/// <param name="context">The serialization context.</param>
		/// <returns>True if the analysis can continue; false if an error occurred.</returns>
		internal abstract bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context);
	}

	/// <summary>
	/// A step that checks for the presence or absence of required properties.
	/// </summary>
	private class RequiredPropertyStep : DistinguishingStep
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RequiredPropertyStep"/> class.
		/// </summary>
		/// <param name="propertyName">The name of the property to check for presence or absence.</param>
		/// <param name="typesWithProperty">The set of union member types that have this property as required.</param>
		/// <param name="typesWithoutProperty">The set of union member types that do not have this property.</param>
		internal RequiredPropertyStep(string propertyName, HashSet<Type> typesWithProperty, HashSet<Type> typesWithoutProperty)
		{
			this.PropertyName = propertyName;
			this.TypesWithProperty = typesWithProperty;
			this.TypesWithoutProperty = typesWithoutProperty;
		}

		/// <summary>
		/// Gets the name of the property to check.
		/// </summary>
		internal string PropertyName { get; }

		/// <summary>
		/// Gets the types that have this property as required.
		/// </summary>
		internal HashSet<Type> TypesWithProperty { get; }

		/// <summary>
		/// Gets the types that do not have this property.
		/// </summary>
		internal HashSet<Type> TypesWithoutProperty { get; }

		/// <inheritdoc/>
		internal override bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context)
		{
			// Create a peek reader to examine the structure without consuming data
			MessagePackReader peekReader = reader.CreatePeekReader();

			if (!peekReader.TryReadNil() && peekReader.NextMessagePackType == MessagePackType.Map)
			{
				int mapSize = peekReader.ReadMapHeader();
				var presentProperties = new HashSet<string>();

				for (int i = 0; i < mapSize; i++)
				{
					if (peekReader.NextMessagePackType == MessagePackType.String)
					{
						presentProperties.Add(peekReader.ReadString()!);
						peekReader.Skip(context); // Skip the value
					}
					else
					{
						// Non-string key, skip both key and value
						peekReader.Skip(context);
						peekReader.Skip(context);
					}
				}

				// Filter candidates based on property presence
				bool propertyPresent = presentProperties.Contains(this.PropertyName);
				if (propertyPresent)
				{
					candidateTypes.IntersectWith(this.TypesWithProperty);
				}
				else
				{
					candidateTypes.IntersectWith(this.TypesWithoutProperty);
				}
			}

			return candidateTypes.Count > 0;
		}
	}

	/// <summary>
	/// A step that checks for incompatible types on properties with the same name.
	/// </summary>
	private class IncompatibleTypeStep : DistinguishingStep
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IncompatibleTypeStep"/> class.
		/// </summary>
		/// <param name="propertyName">
		/// The name of the property to check for incompatible types across union member types.
		/// </param>
		/// <param name="typesByPropertyType">
		/// A mapping from property types to the set of union member types that have this property type.
		/// </param>
		internal IncompatibleTypeStep(string propertyName, Dictionary<Type, HashSet<Type>> typesByPropertyType)
		{
			this.PropertyName = propertyName;
			this.TypesByPropertyType = typesByPropertyType;
		}

		/// <summary>
		/// Gets the name of the property to check.
		/// </summary>
		internal string PropertyName { get; }

		/// <summary>
		/// Gets the mapping from property types to the union member types that have properties of that type.
		/// </summary>
		internal Dictionary<Type, HashSet<Type>> TypesByPropertyType { get; }

		/// <inheritdoc/>
		internal override bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context)
		{
			// This is a simplified implementation - real implementation would need
			// to deeply analyze the MessagePack structure to determine the property type
			MessagePackReader peekReader = reader.CreatePeekReader();

			if (!peekReader.TryReadNil() && peekReader.NextMessagePackType == MessagePackType.Map)
			{
				int mapSize = peekReader.ReadMapHeader();

				for (int i = 0; i < mapSize; i++)
				{
					if (peekReader.NextMessagePackType == MessagePackType.String)
					{
						string? propertyName = peekReader.ReadString();
						if (propertyName == this.PropertyName)
						{
							// Analyze the value type
							MessagePackType valueType = peekReader.NextMessagePackType;

							// For now, only handle primitive type distinctions
							Type? detectedType = valueType switch
							{
								MessagePackType.Integer => typeof(int),
								MessagePackType.String => typeof(string),
								MessagePackType.Boolean => typeof(bool),
								MessagePackType.Float => typeof(double),
								_ => null,
							};

							if (detectedType != null && this.TypesByPropertyType.TryGetValue(detectedType, out HashSet<Type>? compatibleTypes))
							{
								candidateTypes.IntersectWith(compatibleTypes);
								break;
							}
						}
						else
						{
							peekReader.Skip(context); // Skip the value
						}
					}
					else
					{
						// Non-string key, skip both key and value
						peekReader.Skip(context);
						peekReader.Skip(context);
					}
				}
			}

			return candidateTypes.Count > 0;
		}
	}
}
