// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft;

namespace Nerdbank.PolySerializer;

#pragma warning disable NBMsgPackAsync

/// <summary>
/// Manages searching for a sequence within a MessagePack stream and deserializing its elements asynchronously, producing each element as it is deserialized.
/// </summary>
/// <typeparam name="TElement">The type of element to be deserialized.</typeparam>
/// <param name="serializer">The serializer.</param>
/// <param name="provider">The provider to use to obtain the <see cref="ITypeShape"/> objects we need along the way.</param>
/// <param name="reader">The async reader of the msgpack data.</param>
/// <param name="context">Deserialization context.</param>
internal readonly struct StreamingDeserializer<TElement>(SerializerBase serializer, ITypeShapeProvider provider, AsyncReader reader, SerializationContext context)
{
	/// <summary>
	/// Enumerates the elements of an array at the given path in the MessagePack stream.
	/// </summary>
	/// <param name="path">The path to the sequence to be enumerated.</param>
	/// <param name="throwOnUnreachableSequence"><see langword="true" /> to throw if the <paramref name="path"/> cannot be reached or it is null when we get there; <see langword="false" /> to produce an empty sequence in that situation instead.</param>
	/// <param name="elementConverter">The shape of the element to be deserialized.</param>
	/// <param name="skipTrailingBytes">A value indicating whether to bother fast-forwarding after completing the enumeration to position the reader at the EOF or next top-level structure.</param>
	/// <returns>The async enumeration.</returns>
	internal async IAsyncEnumerable<TElement?> EnumerateArrayAsync(Expression path, bool throwOnUnreachableSequence, Converter<TElement> elementConverter, bool skipTrailingBytes)
	{
		// Navigate to the sequence.
		{
			if (await this.NavigateToMemberAsync(path).ConfigureAwait(false) is Expression incompleteExpression)
			{
				// The path was not found. We probably encountered a null or absent member along the path.
				if (throwOnUnreachableSequence)
				{
					throw IncompletePathException(incompleteExpression);
				}

				yield break;
			}
		}

		// Enumerate the actual sequence.
		{
			StreamingReader streamingReader = reader.CreateStreamingReader();

			bool isNil;
			while (streamingReader.TryReadNull(out isNil).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (isNil)
			{
				if (throwOnUnreachableSequence)
				{
					throw IncompletePathException(path is LambdaExpression lambda ? lambda.Body : path);
				}

				reader.ReturnReader(ref streamingReader);
				yield break;
			}

			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			int remaining = count;
			while (remaining > 0)
			{
				// Generally, we prefer synchronous deserialization because it's much faster.
				// Deserialize all the elements we can that are already in the buffer.
				if (elementConverter.PreferAsyncSerialization)
				{
					int bufferedCount = reader.GetBufferedStructuresCount(remaining, context, out bool reachedMaxCount);
					if (bufferedCount > 0)
					{
						for (int i = 0; i < bufferedCount; i++)
						{
							Reader bufferedReader = ((AsyncReader)reader).CreateBufferedReader();
							TElement? element = elementConverter.Read(ref bufferedReader, context);
							reader.ReturnReader(ref bufferedReader);
							yield return element;
							remaining--;
						}

						// If we reached the max count, there may be more items in the buffer still that were not counted.
						// In which case we should NOT potentially wait for more bytes to come as that can hang deserialization
						// while there are still items to yield.
						if (!reachedMaxCount && remaining > 0)
						{
							// We've proven that items *can* fit in the buffer, and that we've read all we can.
							// Try to read more bytes to see if we can keep synchronously deserializing.
							await reader.ReadAsync().ConfigureAwait(false);
						}
					}
					else
					{
						// We have less than one element in the buffer.
						// There's no telling how large the element is, and the user wants us to be async about it.
						// So deserialize asynchronously.
						// After it's done, we *may* find ourselves with a leftover buffer with more items, in which case
						// we'll try again to synchronously deserialize all we can.
						yield return await elementConverter.ReadAsync(reader, context).ConfigureAwait(false);
						remaining--;
					}
				}
				else
				{
					// Each element should be synchronously deserialized.
					// Buffer at least one structure, and notice how many structures we end up with in the buffer,
					// deserializing them in bursts.
					int bufferedCount = await reader.BufferNextStructuresAsync(1, remaining, context).ConfigureAwait(false);
					for (int i = 0; i < bufferedCount; i++)
					{
						Reader bufferedReader = ((AsyncReader)reader).CreateBufferedReader();
						TElement? element = elementConverter.Read(ref bufferedReader, context);
						reader.ReturnReader(ref bufferedReader);
						yield return element;
						remaining--;
					}
				}
			}
		}

		// Skip enough structures to effectively 'pop' the reader down to the end of the top-level msgpack structure we started with.
		// That way, the PipeReader is positioned to read the next structure or obviously at the EOF position.
		if (skipTrailingBytes)
		{
			await reader.AdvanceToEndOfTopLevelStructureAsync().ConfigureAwait(false);
		}

		static Exception IncompletePathException(Expression incompletePath)
			=> new SerializationException($"The path to the sequence could not be followed. {incompletePath} is missing or has a null value.");
	}

	private ValueTask<Expression?> NavigateToMemberAsync(Expression path)
	{
		return new AsyncExpressionVisitor(serializer, reader, provider, context).VisitAsync(path);
	}

	private struct AsyncExpressionVisitor(SerializerBase serializer, AsyncReader reader, ITypeShapeProvider provider, SerializationContext context)
	{
		internal async ValueTask<Expression?> VisitAsync(Expression? expression)
		{
			Expression? result = expression switch
			{
				LambdaExpression lambdaExpression => await this.VisitLambda(lambdaExpression).ConfigureAwait(false),
				ParameterExpression parameterExpression => await this.VisitParameter(parameterExpression).ConfigureAwait(false),
				MemberExpression memberExpression => await this.VisitMember(memberExpression).ConfigureAwait(false),
				BinaryExpression arrayIndexExpression when expression.NodeType == ExpressionType.ArrayIndex => await this.VisitIndex(arrayIndexExpression).ConfigureAwait(false),
				MethodCallExpression methodCallExpression => await this.VisitMethodCall(methodCallExpression).ConfigureAwait(false),
				null => default,
				_ => throw new NotSupportedException($"{expression.NodeType} is not a supported expression type."),
			};

			// If the visit worked, but the next value is nil, we return the expression that led us to the nil value.
			if (result is null && await this.IsNilAsync().ConfigureAwait(false))
			{
				return expression;
			}

			return result;
		}

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
		private async ValueTask<Expression?> VisitMethodCall(MethodCallExpression expression)
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
			if ((await this.VisitAsync(expression.Object).ConfigureAwait(false)) is Expression incompletePath)
			{
				return incompletePath;
			}

			// Ask the converter to retrieve the element at the given index.
			// We use a converter to do the actual skipping.
			ITypeShape? typeShape = provider.GetShape(expression.Object.Type);
			Requires.Argument(typeShape is not null, nameof(expression), "The expression does not have a known type shape.");
			Converter converter = serializer.GetConverter(typeShape);

			return await converter.SkipToIndexValueAsync(reader, indexArg, context).ConfigureAwait(false) ? null : expression;
		}

		private ValueTask<Expression?> VisitLambda(LambdaExpression lambdaExpression)
		{
			return this.VisitAsync(lambdaExpression.Body);
		}

		private ValueTask<Expression?> VisitParameter(ParameterExpression expression)
		{
			// Do nothing.
			// We've reached the root of the expression.
			return new((Expression?)null);
		}

		private async ValueTask<Expression?> VisitIndex(BinaryExpression expression)
		{
			if (expression.Right is not ConstantExpression { Value: int skipElements })
			{
				throw new NotSupportedException("Unsupported index expression.");
			}

			// First navigate to the expression.
			if ((await this.VisitAsync(expression.Left).ConfigureAwait(false)) is Expression incompletePath)
			{
				return incompletePath;
			}

			// Skip the given number of elements.
			StreamingReader streamingReader = reader.CreateStreamingReader();
			int count;
			while (streamingReader.TryReadArrayHeader(out count).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (count < skipElements + 1)
			{
				reader.ReturnReader(ref streamingReader);
				return expression;
			}

			for (int i = 0; i < skipElements; i++)
			{
				while (streamingReader.TrySkip(ref context).NeedsMoreBytes())
				{
					streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
				}
			}

			reader.ReturnReader(ref streamingReader);
			return null;
		}

		private async ValueTask<Expression?> VisitMember(MemberExpression expression)
		{
			Requires.Argument(expression.Expression is not null, nameof(expression), "Expression must not be null.");

			// First navigate to the expression.
			if ((await this.VisitAsync(expression.Expression).ConfigureAwait(false)) is Expression incompletePath)
			{
				return incompletePath;
			}

			// Now navigate to the member.

			// Find the matching property shape.
			ITypeShape? typeShape = provider.GetShape(expression.Expression.Type);
			Requires.Argument(typeShape is not null, nameof(expression), "The expression does not have a known type shape.");

			IPropertyShape? propertyShape = typeShape switch
			{
				IObjectTypeShape objectTypeShape => FindPropertyByName(objectTypeShape, expression.Member.Name),
				_ => null,
			};
			Requires.Argument(propertyShape is not null, nameof(expression), "The expression does not refer to a serialized property.");

			// We use a converter to do the actual skipping.
			Converter converter = serializer.GetConverter(typeShape);

			return await converter.SkipToPropertyValueAsync(reader, propertyShape, context).ConfigureAwait(false) ? null : expression;

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
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

		private async ValueTask<bool> IsNilAsync()
		{
			StreamingReader streamingReader = reader.CreateStreamingReader();
			PolySerializer.Converters.TypeCode peekType;
			while (streamingReader.TryPeekNextCode(out peekType).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			reader.ReturnReader(ref streamingReader);
			return peekType == PolySerializer.Converters.TypeCode.Nil;
		}
	}
}
