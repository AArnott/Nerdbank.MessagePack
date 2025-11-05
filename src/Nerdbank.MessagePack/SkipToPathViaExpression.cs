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
/// <param name="rootShape">The shape of the type at the start of the expression.</param>
/// <param name="context">Serialization context.</param>
internal struct SkipToPathViaExpression(MessagePackSerializer serializer, ITypeShape rootShape, SerializationContext context)
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
	/// <returns>The expression that could not be reached, or the <see cref="ITypeShape"/> of the leaf element if navigation was successful.</returns>
	internal Result<ITypeShape, Expression> NavigateToMember(ref MessagePackReader reader, Expression path) => this.Visit(ref reader, path);

	private static Result<ITypeShape, Expression> Ok(ITypeShape tailShape) => Result<ITypeShape, Expression>.Ok(tailShape);

	private static Result<ITypeShape, Expression> Err(Expression incompleteExpression) => Result<ITypeShape, Expression>.Err(incompleteExpression);

	private Result<ITypeShape, Expression> Visit(ref MessagePackReader reader, Expression? expression)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		return expression switch
		{
			LambdaExpression lambdaExpression => this.Visit(ref reader, lambdaExpression.Body),
			ParameterExpression => Ok(rootShape), // We've reached the root of the expression.
			MemberExpression memberExpression => this.VisitMember(ref reader, memberExpression),
			BinaryExpression arrayIndexExpression when expression.NodeType == ExpressionType.ArrayIndex => this.VisitIndex(ref reader, arrayIndexExpression),
			MethodCallExpression methodCallExpression => this.VisitMethodCall(ref reader, methodCallExpression),
			null => default,
			_ => throw new NotSupportedException($"{expression.NodeType} is not a supported expression type."),
		};
	}

	private Result<ITypeShape, Expression> VisitMethodCall(ref MessagePackReader reader, MethodCallExpression expression)
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
		Result<ITypeShape, Expression> parentResult = this.Visit(ref reader, expression.Object);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		ITypeShape resultShape = parentResult.Value switch
		{
			IDictionaryTypeShape dict => dict.ValueType,
			IEnumerableTypeShape enumerable => enumerable.ElementType,
			_ => throw new NotSupportedException($"Unsupported shape type {parentResult.Value.GetType().Name}."),
		};

		// Ask the converter to retrieve the element at the given index.
		// We use a converter to do the actual skipping.
		MessagePackConverter converter = serializer.GetConverter(parentResult.Value).ValueOrThrow;

		return converter.SkipToIndexValue(ref reader, indexArg, context)
			? Ok(resultShape)
			: Err(expression);
	}

	private Result<ITypeShape, Expression> VisitIndex(ref MessagePackReader reader, BinaryExpression expression)
	{
		if (expression.Right is not ConstantExpression { Value: int skipElements })
		{
			throw new NotSupportedException("Unsupported index expression.");
		}

		// First navigate to the expression.
		Result<ITypeShape, Expression> parentResult = this.Visit(ref reader, expression.Left);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		// We can't navigate to a member if this object is null.
		if (reader.NextMessagePackType == MessagePackType.Nil)
		{
			return Err(expression.Left);
		}

		var enumerableTypeShape = (IEnumerableTypeShape)parentResult.Value;

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

		return Ok(enumerableTypeShape.ElementType);
	}

	private Result<ITypeShape, Expression> VisitMember(ref MessagePackReader reader, MemberExpression expression)
	{
		Requires.Argument(expression.Expression is not null, nameof(expression), "Expression must not be null.");

		// First navigate to the expression.
		Result<ITypeShape, Expression> parentResult = this.Visit(ref reader, expression.Expression);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		// We can't navigate to a member if this object is null.
		if (reader.NextMessagePackType == MessagePackType.Nil)
		{
			return Err(expression.Expression);
		}

		// Now navigate to the member.
		// Find the matching property shape.
		if (parentResult.Value is not IObjectTypeShape objectTypeShape ||
			FindPropertyByName(objectTypeShape, expression.Member.Name) is not IPropertyShape propertyShape)
		{
			throw Requires.Fail("The expression does not refer to a serialized property.");
		}

		// We use a converter to do the actual skipping.
		MessagePackConverter converter = serializer.GetConverter(parentResult.Value).ValueOrThrow;

		return converter.SkipToPropertyValue(ref reader, propertyShape, context) ? Ok(propertyShape.PropertyType) : Err(expression);

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
