// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// A <see cref="IMessagePackConverter{T}"/> that writes objects as maps of property names to values.
/// Data types with constructors and/or <see langword="init" /> properties may be deserialized.
/// </summary>
/// <typeparam name="TDeclaringType">The type of objects that can be serialized or deserialized with this converter.</typeparam>
/// <typeparam name="TArgumentState">The state object that stores individual member values until the constructor delegate can be invoked.</typeparam>
/// <param name="serializable">Tools for serializing individual property values.</param>
/// <param name="argStateCtor">The constructor for the <typeparamref name="TArgumentState"/> that is later passed to the <typeparamref name="TDeclaringType"/> constructor.</param>
/// <param name="ctor">The data type's constructor helper.</param>
/// <param name="parameters">Tools for deserializing individual property values.</param>
internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters) : ObjectMapConverter<TDeclaringType>(serializable, null, null)
{
	/// <inheritdoc/>
	public override TDeclaringType? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		TArgumentState argState = argStateCtor();
		int count = reader.ReadMapHeader();
		for (int i = 0; i < count; i++)
		{
			ReadOnlySpan<byte> propertyName = CodeGenHelpers.ReadStringSpan(ref reader);
			if (parameters.Readers.TryGetValue(propertyName, out DeserializeProperty<TArgumentState>? deserializeArg))
			{
				deserializeArg(ref argState, ref reader);
			}
			else
			{
				reader.Skip();
			}
		}

		return ctor(ref argState);
	}
}
