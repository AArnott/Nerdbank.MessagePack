// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// A helper that can synchronously skip to a given structural position within msgpack data,
/// without deserializing it.
/// </summary>
/// <param name="serializer">The serializer from which to obtain converters that will be used to skip forward.</param>
/// <param name="provider">The type shape provider that may be used to create the required converters.</param>
/// <param name="context">Serialization context.</param>
internal struct SkipToPathViaExpression(MessagePackSerializer serializer, ITypeShapeProvider provider, SerializationContext context)
{
	/// <summary>
	/// Creates an exception suitable for throwing to describe a path through msgpack data that could not be completed.
	/// </summary>
	/// <param name="incompletePath">
	/// The path that could not be followed.
	/// The last segment in the path should be the first step that could not be followed.
	/// </param>
	/// <returns>The exception the caller may throw.</returns>
	internal static Exception IncompletePathException(Expression incompletePath)
		=> new MessagePackSerializationException($"The path to the data to be deserialized could not be followed. {incompletePath} is missing or has a null value.");

	/// <summary>
	/// Navigates to a member at the given path in the MessagePack data.
	/// </summary>
	/// <param name="reader">The reader to advance along the path.</param>
	/// <param name="path">The path to navigate to.</param>
	/// <returns>The expression that could not be reached, or <see langword="null"/> if navigation was successful.</returns>
	internal Expression? NavigateToMember(ref MessagePackReader reader, Expression path) => this.Visit(ref reader, path);

	private Expression? Visit(ref MessagePackReader reader, Expression? expression)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		Expression? result = expression switch
		{
			LambdaExpression lambdaExpression => this.VisitLambda(ref reader, lambdaExpression),
			ParameterExpression parameterExpression => this.VisitParameter(parameterExpression),
			MemberExpression memberExpression => this.VisitMember(ref reader, memberExpression),
			BinaryExpression arrayIndexExpression when expression.NodeType == ExpressionType.ArrayIndex => this.VisitIndex(ref reader, arrayIndexExpression),
			MethodCallExpression methodCallExpression => this.VisitMethodCall(ref reader, methodCallExpression),
			null => default,
			_ => throw new NotSupportedException($"{expression.NodeType} is not a supported expression type."),
		};

		// If the visit worked, but the next value is nil, we return the expression that led us to the nil value.
		if (result is null && reader.TryReadNil())
		{
			return expression;
		}

		return result;
	}

	private Expression? VisitMethodCall(ref MessagePackReader reader, MethodCallExpression expression)
	{
		if (expression is not { Object: not null, Method: { Name: "get_Item", IsSpecialName: true }, Arguments.Count: 1 })
		{
			throw new NotSupportedException("Unsupported method call expression.");
		}

		object? indexArg = expression.Arguments[0] switch
		{
			ConstantExpression constantExpression => constantExpression.Value,
			MemberExpression memberExpression when memberExpression.Expression is ConstantExpression owner => memberExpression.Member switch
			{
				FieldInfo fieldInfo => fieldInfo.GetValue(owner.Value),
				PropertyInfo propertyInfo => propertyInfo.GetValue(owner.Value),
				_ => throw new NotSupportedException("Unsupported index expression."),
			},
			_ => throw new NotSupportedException("Unsupported index expression."),
		};

		// First navigate to the object.
		if (this.Visit(ref reader, expression.Object) is Expression incompletePath)
		{
			return incompletePath;
		}

		// Ask the converter to retrieve the element at the given index.
		// We use a converter to do the actual skipping.
		ITypeShape? typeShape = provider.GetTypeShape(expression.Object.Type);
		Requires.Argument(typeShape is not null, nameof(expression), "The expression does not have a known type shape.");
		MessagePackConverter converter = serializer.GetConverter(typeShape).ValueOrThrow;

		return converter.SkipToIndexValue(ref reader, indexArg, context) ? null : expression;
	}

	private Expression? VisitLambda(ref MessagePackReader reader, LambdaExpression lambdaExpression)
	{
		return this.Visit(ref reader, lambdaExpression.Body);
	}

	private Expression? VisitParameter(ParameterExpression expression)
	{
		// Do nothing.
		// We've reached the root of the expression.
		return null;
	}

	private Expression? VisitIndex(ref MessagePackReader reader, BinaryExpression expression)
	{
		if (expression.Right is not ConstantExpression { Value: int skipElements })
		{
			throw new NotSupportedException("Unsupported index expression.");
		}

		// First navigate to the expression.
		if (this.Visit(ref reader, expression.Left) is Expression incompletePath)
		{
			return incompletePath;
		}

		// Skip the given number of elements.
		int count = reader.ReadArrayHeader();
		if (count < skipElements + 1)
		{
			return expression;
		}

		for (int i = 0; i < skipElements; i++)
		{
			reader.Skip(context);
		}

		return null;
	}

	private Expression? VisitMember(ref MessagePackReader reader, MemberExpression expression)
	{
		Requires.Argument(expression.Expression is not null, nameof(expression), "Expression must not be null.");

		// First navigate to the expression.
		if (this.Visit(ref reader, expression.Expression) is Expression incompletePath)
		{
			return incompletePath;
		}

		// Now navigate to the member.

		// Find the matching property shape.
		ITypeShape? typeShape = provider.GetTypeShape(expression.Expression.Type);
		Requires.Argument(typeShape is not null, nameof(expression), "The expression does not have a known type shape.");

		IPropertyShape? propertyShape = typeShape switch
		{
			IObjectTypeShape objectTypeShape => FindPropertyByName(objectTypeShape, expression.Member.Name),
			_ => null,
		};
		Requires.Argument(propertyShape is not null, nameof(expression), "The expression does not refer to a serialized property.");

		// We use a converter to do the actual skipping.
		MessagePackConverter converter = serializer.GetConverter(typeShape).ValueOrThrow;

		return converter.SkipToPropertyValue(ref reader, propertyShape, context) ? null : expression;

		static IPropertyShape? FindPropertyByName(IObjectTypeShape typeShape, string name)
		{
			foreach (IPropertyShape property in typeShape.Properties)
			{
				if (property.Name == name)
				{
					return property;
				}
			}

			return null;
		}
	}
}
