// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// Associates constructor arguments with properties, preserving exact matches before considering case-insensitive matches.
/// </summary>
internal sealed class ConstructorParameterMapping
{
	private readonly Dictionary<IPropertyShape, IParameterShape> parametersByProperty = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstructorParameterMapping"/> class.
	/// </summary>
	/// <param name="constructor">The constructor whose arguments should be mapped.</param>
	/// <param name="getSerializedName">A function that supplies serialized property names for diagnostics, or <see langword="null"/> to use shape names.</param>
	internal ConstructorParameterMapping(IConstructorShape constructor, Func<IPropertyShape, string>? getSerializedName = null)
	{
		List<(IParameterShape Parameter, IPropertyShape? Property)> parameters = new();
		Dictionary<IPropertyShape, int> indexes = new();
		foreach (IParameterShape parameter in constructor.Parameters)
		{
			IPropertyShape? property = FindProperty(constructor, parameter);
			if (property is not null && indexes.TryGetValue(property, out int index))
			{
				IParameterShape previous = parameters[index].Parameter;
				IParameterShape? preferred = (previous.Kind, parameter.Kind) switch
				{
					(ParameterKind.MethodParameter, ParameterKind.MemberInitializer) => previous,
					(ParameterKind.MemberInitializer, ParameterKind.MethodParameter) => parameter,
					_ => null,
				};
				IParameterShape discarded = preferred == previous ? parameter : previous;
				string target = $"property '{property.Name}' ({property.PropertyType.Type.FullName}, serialized key '{getSerializedName?.Invoke(property) ?? property.Name}')";
				if (previous.ParameterType.Type != property.PropertyType.Type || parameter.ParameterType.Type != property.PropertyType.Type)
				{
					throw CreateConflictException(constructor, previous, parameter, target, "The argument and property types differ, so the member initializer cannot safely be discarded.");
				}

				if (preferred is null || discarded.IsRequired)
				{
					throw CreateConflictException(constructor, previous, parameter, target, preferred is null ? "Multiple arguments of the same kind cannot share a property." : "A required member initializer cannot be discarded in favor of a constructor parameter.");
				}

				// Leaving the optional initializer unset preserves the value established by the constructor.
				parameters[index] = (preferred, property);
			}
			else
			{
				if (property is not null)
				{
					indexes.Add(property, parameters.Count);
				}

				parameters.Add((parameter, property));
			}
		}

		this.Parameters = parameters;
		foreach ((IParameterShape parameter, IPropertyShape? property) in parameters)
		{
			if (property is not null)
			{
				this.parametersByProperty.Add(property, parameter);
			}
		}
	}

	/// <summary>
	/// Gets the arguments to deserialize and their associated properties, if any.
	/// </summary>
	internal IReadOnlyList<(IParameterShape Parameter, IPropertyShape? Property)> Parameters { get; }

	/// <summary>
	/// Creates an exception describing conflicting constructor arguments.
	/// </summary>
	/// <param name="constructor">The constructor.</param>
	/// <param name="first">The first argument.</param>
	/// <param name="second">The second argument.</param>
	/// <param name="target">The conflicting property or serialized key.</param>
	/// <param name="reason">Why the conflict cannot be resolved.</param>
	/// <returns>The exception.</returns>
	internal static MessagePackSerializationException CreateConflictException(IConstructorShape constructor, IParameterShape first, IParameterShape second, string target, string reason)
		=> new($"The type '{constructor.DeclaringType.Type.FullName}' maps constructor arguments '{first.Name}' ({first.Kind}, {first.ParameterType.Type.FullName}) and '{second.Name}' ({second.Kind}, {second.ParameterType.Type.FullName}) to the same {target}. {reason} Rename the constructor parameters or use PropertyShapeAttribute/ParameterShapeAttribute to give them unambiguous names, or write a custom converter.");

	/// <summary>
	/// Gets the selected constructor argument for a property.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>The associated argument, or <see langword="null"/>.</returns>
	internal IParameterShape? GetParameter(IPropertyShape property) => this.parametersByProperty.TryGetValue(property, out IParameterShape? parameter) ? parameter : null;

	/// <summary>
	/// Finds the property associated with an argument, rejecting ambiguous matches.
	/// </summary>
	/// <param name="constructor">The constructor.</param>
	/// <param name="parameter">The argument.</param>
	/// <returns>The associated property, if any.</returns>
	private static IPropertyShape? FindProperty(IConstructorShape constructor, IParameterShape parameter)
	{
		IReadOnlyList<IPropertyShape> properties = constructor.DeclaringType.Properties;
		return FindMatch(StringComparison.Ordinal) ?? FindMatch(StringComparison.OrdinalIgnoreCase);

		IPropertyShape? FindMatch(StringComparison comparison)
		{
			IPropertyShape? result = null;
			foreach (IPropertyShape property in properties)
			{
				if (string.Equals(parameter.Name, property.Name, comparison))
				{
					if (result is not null)
					{
						throw new MessagePackSerializationException($"The type '{constructor.DeclaringType.Type.FullName}' has constructor argument '{parameter.Name}' ({parameter.Kind}, {parameter.ParameterType.Type.FullName}) that ambiguously matches properties '{result.Name}' ({result.PropertyType.Type.FullName}) and '{property.Name}' ({property.PropertyType.Type.FullName}). Rename the argument or use PropertyShapeAttribute/ParameterShapeAttribute to disambiguate the names, or write a custom converter.");
					}

					result = property;
				}
			}

			return result;
		}
	}
}
