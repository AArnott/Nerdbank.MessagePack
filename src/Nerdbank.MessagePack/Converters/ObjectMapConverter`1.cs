// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? constructor) : MessagePackConverter<T>
{
	public override void Serialize(ref MessagePackWriter writer, ref T? value)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteMapHeader(serializable.Properties.Count);
		foreach (var property in serializable.Properties)
		{
			writer.WriteRaw(property.RawPropertyNameString.Span);
			property.Write(ref value, ref writer);
		}
	}

	public override T? Deserialize(ref MessagePackReader reader)
	{
		if (reader.TryReadNil())
		{
			return default;
		}

		if (constructor is null || deserializable is null)
		{
			throw new NotSupportedException($"No constructor for {typeof(T).Name}.");
		}

		T value = constructor();
		int count = reader.ReadMapHeader();
		for (int i = 0; i < count; i++)
		{
			ReadOnlySpan<byte> propertyName = CodeGenHelpers.ReadStringSpan(ref reader);
			if (deserializable.Value.Readers.TryGetValue(propertyName, out DeserializeProperty<T>? deserialize))
			{
				deserialize(ref value, ref reader);
			}
			else
			{
				reader.Skip();
			}
		}

		return value;
	}
}

internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters) : ObjectMapConverter<TDeclaringType>(serializable, null, null)
{
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
