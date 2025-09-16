// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
[Experimental("DuckTyping")]
public class DerivedTypeDuckTyping : DerivedTypeUnion
{
	private readonly ITypeShape baseShape;
	private readonly ReadOnlyMemory<ITypeShape> derivedTypeShapes;
	private readonly Dictionary<Type, ITypeShape> typeToShapeMap;
	private readonly List<Filter> steps;

	/// <summary>
	/// Initializes a new instance of the <see cref="DerivedTypeDuckTyping"/> class.
	/// </summary>
	/// <param name="baseShape">The shape of the base type.</param>
	/// <param name="derivedTypeShapes">The shapes of the derived types.</param>
	public DerivedTypeDuckTyping(ITypeShape baseShape, params ReadOnlySpan<ITypeShape> derivedTypeShapes)
	{
		Requires.NotNull(baseShape);

		this.baseShape = baseShape;

		// Make sure we have an immutable copy
		this.derivedTypeShapes = derivedTypeShapes.ToArray();

		this.typeToShapeMap = new(derivedTypeShapes.Length + 1);
		if (baseShape.Type is { IsAbstract: false, IsInterface: false })
		{
			this.typeToShapeMap.Add(baseShape.Type, baseShape);
		}

		foreach (ITypeShape derivedTypeShape in derivedTypeShapes)
		{
			Requires.Argument(derivedTypeShape.Type is { IsAbstract: false, IsInterface: false }, nameof(derivedTypeShape), $"Derived types must be concrete, but {derivedTypeShape.Type.FullName} is not.");
			this.typeToShapeMap.Add(derivedTypeShape.Type, derivedTypeShape);
		}

		Requires.Argument(this.typeToShapeMap.Count > 1, nameof(derivedTypeShapes), "At least two type shapes must be provided. The base shape only counts if it is a concrete type.");

		this.steps = BuildMapping(derivedTypeShapes) ?? throw new ArgumentException("The type shapes given do not include (enough) unique characteristics.");
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
	internal ReadOnlyMemory<ITypeShape> DerivedShapes => this.derivedTypeShapes;

	/// <summary>
	/// Attempts to identify the runtime type based on MessagePack data.
	/// </summary>
	/// <param name="reader">A reader positioned at the start of the value to analyze.</param>
	/// <param name="serializationContext">The serialization context.</param>
	/// <param name="typeShape">The identified type shape, if successful.</param>
	/// <returns>True if exactly one matching type was identified; false otherwise.</returns>
#pragma warning disable NBMsgPack050 // Use ref parameters for ref structs -- acts as a peek reader
	internal bool TryIdentifyType(in MessagePackReader reader, in SerializationContext serializationContext, [NotNullWhen(true)] out ITypeShape? typeShape)
#pragma warning restore NBMsgPack050 // Use ref parameters for ref structs
	{
		DuckTypingContext context = new(reader, serializationContext, new HashSet<Type>(this.typeToShapeMap.Keys));

		foreach (Filter step in this.steps)
		{
			step.Execute(ref context);

			if (context.RemainingCandidateTypes.Count == 1)
			{
				typeShape = this.typeToShapeMap[context.RemainingCandidateTypes.First()];
				return true;
			}
		}

		typeShape = null;
		return false;
	}

	/// <inheritdoc/>
	internal override void InternalDerivationsOnly() => throw new NotImplementedException();

	private static List<Filter> BuildMapping(ReadOnlySpan<ITypeShape> typeShapes)
	{
		List<Filter> steps = [];

		AnalyzeRequiredProperties(typeShapes, steps);

		return steps;
	}

	private static void AnalyzeRequiredProperties(ReadOnlySpan<ITypeShape> typeShapes, List<Filter> steps)
	{
		Dictionary<string, (HashSet<Type> WithProperty, HashSet<Type> WithoutProperty)> propertyAnalysis = new(StringComparer.Ordinal);

		foreach (ITypeShape typeShape in typeShapes)
		{
			if (typeShape is not IObjectTypeShape { Constructor.Parameters: { Count: > 0 } parameters })
			{
				continue;
			}

			List<string> requiredProperties = [];
			foreach (IParameterShape parameter in parameters)
			{
				if (parameter.IsRequired)
				{
					requiredProperties.Add(parameter.Name);
				}
			}

			if (requiredProperties.Count > 0)
			{
				steps.Add(new RequiredPropertyStep(typeShape.Type, requiredProperties));
			}
		}
	}

	private ref struct DuckTypingContext
	{
		private readonly MessagePackReader reader;
		private readonly SerializationContext context;
		private Dictionary<string, MessagePackType>? propertyTypes;
		private HashSet<Type> remainingCandidateTypes;

		internal DuckTypingContext(in MessagePackReader reader, in SerializationContext context, HashSet<Type> remainingCandidateTypes)
		{
			this.reader = reader;
			this.context = context;
			this.remainingCandidateTypes = remainingCandidateTypes;
		}

		[UnscopedRef]
		internal ref readonly MessagePackReader Reader => ref this.reader;

		[UnscopedRef]
		internal ref readonly SerializationContext Context => ref this.context;

		internal IReadOnlyCollection<Type> RemainingCandidateTypes => this.remainingCandidateTypes;

		internal bool HasProperty(string propertyName) => this.TryGetPropertyType(propertyName, out _);

		internal bool TryGetPropertyType(string propertyName, out MessagePackType propertyType)
		{
			this.InitProperties();
			return this.propertyTypes.TryGetValue(propertyName, out propertyType);
		}

		internal bool RemoveCandidateType(Type type) => this.remainingCandidateTypes.Remove(type);

		[MemberNotNull(nameof(propertyTypes))]
		private void InitProperties()
		{
			if (this.propertyTypes is null)
			{
				MessagePackReader peekReader = this.reader.CreatePeekReader();

				this.propertyTypes = [];
				if (peekReader.NextMessagePackType == MessagePackType.Map)
				{
					int count = peekReader.ReadMapHeader();
					for (int i = 0; i < count; i++)
					{
						string? propertyName;
						if (peekReader.NextMessagePackType == MessagePackType.String)
						{
							propertyName = peekReader.ReadString()!;

							// Record the type of this property's value.
							this.propertyTypes.Add(propertyName, peekReader.NextMessagePackType);
						}
						else
						{
							propertyName = null;

							// Non-string key, skip it.
							peekReader.Skip(this.context);
						}

						// Skip the value.
						peekReader.Skip(this.context);
					}
				}
			}
		}
	}

	/// <summary>
	/// A filtering step that narrows down candidate types.
	/// </summary>
	private abstract class Filter
	{
		/// <summary>
		/// Executes the filter to hopefully narrow down the candidate types to be deserialized.
		/// </summary>
		/// <param name="context">The duck typing context.</param>
		internal abstract void Execute(ref DuckTypingContext context);
	}

	/// <summary>
	/// A step that checks for the presence or absence of required properties.
	/// </summary>
	private class RequiredPropertyStep(Type type, IReadOnlyCollection<string> requiredPropertyNames) : Filter
	{
		/// <inheritdoc/>
		internal override void Execute(ref DuckTypingContext context)
		{
			foreach (string propertyName in requiredPropertyNames)
			{
				if (!context.HasProperty(propertyName))
				{
					context.RemoveCandidateType(type);
					break;
				}
			}
		}
	}
}
