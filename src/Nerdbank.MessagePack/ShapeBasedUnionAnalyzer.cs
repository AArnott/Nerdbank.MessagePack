// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Analyzes type shapes to determine distinguishing characteristics that can be used
/// to identify union members without explicit aliases.
/// </summary>
public class ShapeBasedUnionAnalyzer
{
    /// <summary>
    /// Analyzes a collection of type shapes to build a decision tree for distinguishing between them.
    /// </summary>
    /// <param name="typeShapes">The type shapes to analyze.</param>
    /// <returns>A <see cref="ShapeBasedUnionMapping"/> that can distinguish between the shapes, or null if no distinguishing characteristics are found.</returns>
    public static ShapeBasedUnionMapping? AnalyzeShapes(IReadOnlyCollection<ITypeShape> typeShapes)
    {
        Requires.NotNull(typeShapes);
        
        if (typeShapes.Count < 2)
        {
            return null;
        }

        var builder = new ShapeAnalysisBuilder(typeShapes);
        return builder.BuildMapping();
    }

    /// <summary>
    /// Represents the analysis result for shape-based union distinction.
    /// </summary>
    public class ShapeBasedUnionMapping
    {
        internal ShapeBasedUnionMapping(IReadOnlyList<DistinguishingStep> steps, FrozenDictionary<Type, ITypeShape> typeShapeMapping)
        {
            this.Steps = steps;
            this.TypeShapeMapping = typeShapeMapping;
        }

        /// <summary>
        /// Gets the sequence of steps to execute to distinguish between types.
        /// </summary>
        public IReadOnlyList<DistinguishingStep> Steps { get; }

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
        public bool TryIdentifyType(ref MessagePackReader reader, SerializationContext context, [NotNullWhen(true)] out ITypeShape? typeShape)
        {
            var candidateTypes = new HashSet<Type>(this.TypeShapeMapping.Keys);
            
            foreach (var step in this.Steps)
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
    public abstract class DistinguishingStep
    {
        /// <summary>
        /// Executes this step against the MessagePack data to narrow down candidate types.
        /// </summary>
        /// <param name="reader">A reader positioned at the start of the value to analyze.</param>
        /// <param name="candidateTypes">The set of candidate types, which this method should modify to remove types that don't match.</param>
        /// <returns>True if the analysis can continue; false if an error occurred.</returns>
        public abstract bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context);
    }

    /// <summary>
    /// A step that checks for the presence or absence of required properties.
    /// </summary>
    public class RequiredPropertyStep : DistinguishingStep
    {
        internal RequiredPropertyStep(string propertyName, HashSet<Type> typesWithProperty, HashSet<Type> typesWithoutProperty)
        {
            this.PropertyName = propertyName;
            this.TypesWithProperty = typesWithProperty;
            this.TypesWithoutProperty = typesWithoutProperty;
        }

        /// <summary>
        /// Gets the name of the property to check.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the types that have this property as required.
        /// </summary>
        internal HashSet<Type> TypesWithProperty { get; }

        /// <summary>
        /// Gets the types that do not have this property.
        /// </summary>
        internal HashSet<Type> TypesWithoutProperty { get; }

        /// <inheritdoc/>
        public override bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context)
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
                        presentProperties.Add(peekReader.ReadString());
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
    public class IncompatibleTypeStep : DistinguishingStep
    {
        internal IncompatibleTypeStep(string propertyName, Dictionary<Type, HashSet<Type>> typesByPropertyType)
        {
            this.PropertyName = propertyName;
            this.TypesByPropertyType = typesByPropertyType;
        }

        /// <summary>
        /// Gets the name of the property to check.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the mapping from property types to the union member types that have properties of that type.
        /// </summary>
        internal Dictionary<Type, HashSet<Type>> TypesByPropertyType { get; }

        /// <inheritdoc/>
        public override bool Execute(ref MessagePackReader reader, HashSet<Type> candidateTypes, SerializationContext context)
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
                        string propertyName = peekReader.ReadString();
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
                                _ => null
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

    /// <summary>
    /// Internal helper class for building shape analysis.
    /// </summary>
    private class ShapeAnalysisBuilder
    {
        private readonly IReadOnlyCollection<ITypeShape> typeShapes;
        private readonly List<DistinguishingStep> steps = [];

        public ShapeAnalysisBuilder(IReadOnlyCollection<ITypeShape> typeShapes)
        {
            this.typeShapes = typeShapes;
        }

        public ShapeBasedUnionMapping? BuildMapping()
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
            var propertyAnalysis = new Dictionary<string, (HashSet<Type> WithProperty, HashSet<Type> WithoutProperty)>();

            foreach (ITypeShape typeShape in this.typeShapes)
            {
                if (typeShape is IObjectTypeShape objectShape)
                {
                    var propertiesInThisType = new HashSet<string>();
                    
                    foreach (IPropertyShape property in objectShape.Properties)
                    {
                        propertiesInThisType.Add(property.Name);
                        
                        if (!propertyAnalysis.TryGetValue(property.Name, out var analysis))
                        {
                            analysis = ([], []);
                            propertyAnalysis[property.Name] = analysis;
                        }
                        
                        analysis.WithProperty.Add(typeShape.Type);
                    }

                    // Add this type to the "without" set for properties it doesn't have
                    foreach (var (propertyName, analysis) in propertyAnalysis)
                    {
                        if (!propertiesInThisType.Contains(propertyName))
                        {
                            analysis.WithoutProperty.Add(typeShape.Type);
                        }
                    }
                }
            }

            // Create steps for properties that can distinguish between types
            foreach (var (propertyName, (withProperty, withoutProperty)) in propertyAnalysis)
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
                        if (!propertyTypeAnalysis.TryGetValue(property.Name, out var typeMapping))
                        {
                            typeMapping = [];
                            propertyTypeAnalysis[property.Name] = typeMapping;
                        }

                        Type propertyType = property.PropertyType.Type;
                        if (!typeMapping.TryGetValue(propertyType, out var typesWithThisPropertyType))
                        {
                            typesWithThisPropertyType = [];
                            typeMapping[propertyType] = typesWithThisPropertyType;
                        }

                        typesWithThisPropertyType.Add(typeShape.Type);
                    }
                }
            }

            // Create steps for properties with incompatible types
            foreach (var (propertyName, typeMapping) in propertyTypeAnalysis)
            {
                if (typeMapping.Keys.Count > 1) // Multiple different types for same property name
                {
                    this.steps.Add(new IncompatibleTypeStep(propertyName, typeMapping));
                }
            }
        }
    }
}