// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Microsoft;

namespace Nerdbank.MessagePack;

/// <summary>
/// Manages searching for a sequence within a MessagePack stream and deserializing its elements asynchronously, producing each element as it is deserialized.
/// </summary>
/// <typeparam name="TElement">The type of element to be deserialized.</typeparam>
/// <param name="serializer">The serializer.</param>
/// <param name="rootShape">The shape of the type at the start of the expression.</param>
/// <param name="reader">The async reader of the msgpack data.</param>
/// <param name="context">Deserialization context.</param>
internal readonly struct StreamingDeserializer<TElement>(MessagePackSerializer serializer, ITypeShape rootShape, MessagePackAsyncReader reader, SerializationContext context)
{
	/// <summary>
	/// Enumerates the elements of an array at the given path in the MessagePack stream.
	/// </summary>
	/// <param name="path">The path to the sequence to be enumerated.</param>
	/// <param name="throwOnUnreachableSequence"><see langword="true" /> to throw if the <paramref name="path"/> cannot be reached or it is null when we get there; <see langword="false" /> to produce an empty sequence in that situation instead.</param>
	/// <param name="skipTrailingBytes">A value indicating whether to bother fast-forwarding after completing the enumeration to position the reader at the EOF or next top-level structure.</param>
	/// <returns>The async enumeration.</returns>
	internal async IAsyncEnumerable<TElement?> EnumerateArrayAsync(Expression path, bool throwOnUnreachableSequence, bool skipTrailingBytes)
	{
		// Navigate to the sequence.
		ITypeShape leafShape;
		{
			Result<ITypeShape, Expression> result = await this.VisitAsync(path).ConfigureAwait(false);
			if (result is { Success: false, Error: { } incompleteExpression })
			{
				// The path was not found. We probably encountered a null or absent member along the path.
				if (throwOnUnreachableSequence)
				{
					throw SkipToPathViaExpression.IncompletePathException(incompleteExpression);
				}

				yield break;
			}

			leafShape = ((IEnumerableTypeShape)result.Value).ElementType;
		}

		// Enumerate the actual sequence.
		MessagePackConverter<TElement> elementConverter = (MessagePackConverter<TElement>)serializer.ConverterCache.GetOrAddConverter(leafShape).ValueOrThrow;
		{
			MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();

			bool isNil;
			while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}

			if (isNil)
			{
				if (throwOnUnreachableSequence)
				{
					throw SkipToPathViaExpression.IncompletePathException(path is LambdaExpression lambda ? lambda.Body : path);
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
							MessagePackReader bufferedReader = reader.CreateBufferedReader();
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
						MessagePackReader bufferedReader = reader.CreateBufferedReader();
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
	}

	private static Result<ITypeShape, Expression> Ok(ITypeShape tailShape) => Result<ITypeShape, Expression>.Ok(tailShape);

	private static Result<ITypeShape, Expression> Err(Expression incompleteExpression) => Result<ITypeShape, Expression>.Err(incompleteExpression);

	private ValueTask<Result<ITypeShape, Expression>> VisitAsync(Expression expression)
	{
		context.CancellationToken.ThrowIfCancellationRequested();
		return expression switch
		{
			LambdaExpression lambdaExpression => this.VisitAsync(lambdaExpression.Body),
			ParameterExpression => new(Ok(rootShape)), // We've reached the root of the expression.
			MemberExpression memberExpression => this.VisitMember(memberExpression),
			BinaryExpression arrayIndexExpression when expression.NodeType == ExpressionType.ArrayIndex => this.VisitIndex(arrayIndexExpression),
			MethodCallExpression methodCallExpression => this.VisitMethodCall(methodCallExpression),
			_ => throw new NotSupportedException($"{expression.NodeType} is not a supported expression type."),
		};
	}

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
	private async ValueTask<Result<ITypeShape, Expression>> VisitMethodCall(MethodCallExpression expression)
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
		Result<ITypeShape, Expression> parentResult = await this.VisitAsync(expression.Object).ConfigureAwait(false);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		// If the visit worked, but the next value is nil, we return the expression that led us to the nil value.
		if (await this.IsNilAsync().ConfigureAwait(false))
		{
			return Err(expression);
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

		return await converter.SkipToIndexValueAsync(reader, indexArg, context).ConfigureAwait(false)
			? Ok(resultShape)
			: Err(expression);
	}

	private async ValueTask<Result<ITypeShape, Expression>> VisitIndex(BinaryExpression expression)
	{
		if (expression.Right is not ConstantExpression { Value: int skipElements })
		{
			throw new NotSupportedException("Unsupported index expression.");
		}

		// First navigate to the expression.
		Result<ITypeShape, Expression> parentResult = await this.VisitAsync(expression.Left).ConfigureAwait(false);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		// If the visit worked, but the next value is nil, we return the expression that led us to the nil value.
		if (await this.IsNilAsync().ConfigureAwait(false))
		{
			return Err(expression);
		}

		var enumerableTypeShape = (IEnumerableTypeShape)parentResult.Value;

		// Skip the given number of elements.
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
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

		SerializationContext localContext = context;
		for (int i = 0; i < skipElements; i++)
		{
			while (streamingReader.TrySkip(ref localContext).NeedsMoreBytes())
			{
				streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
			}
		}

		reader.ReturnReader(ref streamingReader);
		return Ok(enumerableTypeShape.ElementType);
	}

	private async ValueTask<Result<ITypeShape, Expression>> VisitMember(MemberExpression expression)
	{
		Requires.Argument(expression.Expression is not null, nameof(expression), "Expression must not be null.");

		// First navigate to the expression.
		Result<ITypeShape, Expression> parentResult = await this.VisitAsync(expression.Expression).ConfigureAwait(false);
		if (parentResult is { Success: false } err)
		{
			return err.Error;
		}

		// If the visit worked, but the next value is nil, we return the expression that led us to the nil value.
		if (await this.IsNilAsync().ConfigureAwait(false))
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

		return await converter.SkipToPropertyValueAsync(reader, propertyShape, context).ConfigureAwait(false) ? Ok(propertyShape.PropertyType) : Err(expression);

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
		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		bool isNil;
		while (streamingReader.TryReadNil(out isNil).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		return isNil;
	}
}
