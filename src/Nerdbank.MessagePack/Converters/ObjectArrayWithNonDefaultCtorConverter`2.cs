// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="MessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Data types with constructors and/or <see langword="init" /> properties may be deserialized.
/// </summary>
/// <typeparam name="TDeclaringType">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <typeparam name="TArgumentState">The state object that stores individual member values until the constructor delegate can be invoked.</typeparam>
/// <param name="properties">Property accessors, in array positions matching serialization indexes.</param>
/// <param name="argStateCtor">The constructor for the <typeparamref name="TArgumentState"/> that is later passed to the <typeparamref name="TDeclaringType"/> constructor.</param>
/// <param name="ctor">The data type's constructor helper.</param>
/// <param name="parameters">Constructor parameter initializers, in array positions matching serialization indexes.</param>
internal class ObjectArrayWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(
	PropertyAccessors<TDeclaringType>?[] properties,
	Func<TArgumentState> argStateCtor,
	Constructor<TArgumentState, TDeclaringType> ctor,
	DeserializableProperty<TArgumentState>?[] parameters) : ObjectArrayConverter<TDeclaringType>(properties, null)
{
	/// <inheritdoc/>
	public override TDeclaringType? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		context.DepthStep();
		TArgumentState argState = argStateCtor();

		int count = reader.ReadArrayHeader();
		for (int i = 0; i < count; i++)
		{
			if (parameters.Length > i && parameters[i] is { } deserialize)
			{
				deserialize.Read(ref argState, ref reader, context);
			}
			else
			{
				reader.Skip(context);
			}
		}

		return ctor(ref argState);
	}

	/// <inheritdoc/>
	[Experimental("NBMsgPackAsync")]
	public override async ValueTask<TDeclaringType?> ReadAsync(MessagePackAsyncReader reader, SerializationContext context, CancellationToken cancellationToken)
	{
		if (await reader.TryReadNilAsync(cancellationToken).ConfigureAwait(false))
		{
			return default;
		}

		context.DepthStep();
		TArgumentState argState = argStateCtor();

		int count = await reader.ReadArrayHeaderAsync(cancellationToken).ConfigureAwait(false);
		for (int i = 0; i < count; i++)
		{
			if (parameters.Length > i && parameters[i] is { } deserialize)
			{
				argState = await deserialize.ReadAsync(argState, reader, context, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await reader.SkipAsync(context, cancellationToken).ConfigureAwait(false);
			}
		}

		return ctor(ref argState);
	}
}
