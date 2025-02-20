// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace ShapeShift.Json;

internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters, SerializeDefaultValuesPolicy defaultValuesPolicy, JsonFormatter formatter)
	: Converters.ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(serializable, argStateCtor, ctor, parameters, defaultValuesPolicy)
{
	private readonly FrozenDictionary<string, DeserializableProperty<TArgumentState>>? parametersByName = parameters.Readers?.Entries.ToFrozenDictionary(p => formatter.Encoding.GetString(p.Key.Span), p => p.Value, StringComparer.Ordinal);

	protected override bool TryLookupProperty(ref Reader reader, out DeserializableProperty<TArgumentState> deserializableArg)
	{
		string? propertyName = reader.ReadString();
		if (propertyName is not null && this.parametersByName is not null)
		{
			return this.parametersByName.TryGetValue(propertyName, out deserializableArg);
		}

		deserializableArg = default;
		return false;
	}
}
