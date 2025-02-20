// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace ShapeShift.Json;

/// <summary>
/// An object map converter for the JSON serializer, for types without a default constructor.
/// </summary>
/// <typeparam name="TDeclaringType"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/typeparam[@name='TDeclaringType']"/></typeparam>
/// <typeparam name="TArgumentState"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/typeparam[@name='TArgumentState']"/></typeparam>
/// <param name="serializable"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='serializable']"/></param>
/// <param name="argStateCtor"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='argStateCtor']"/></param>
/// <param name="ctor"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='ctor']"/></param>
/// <param name="parameters"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='parameters']"/></param>
/// <param name="defaultValuesPolicy"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='defaultValuesPolicy']"/></param>
/// <param name="formatter">The formatter used for the serializer.</param>
internal class ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(MapSerializableProperties<TDeclaringType> serializable, Func<TArgumentState> argStateCtor, Constructor<TArgumentState, TDeclaringType> ctor, MapDeserializableProperties<TArgumentState> parameters, SerializeDefaultValuesPolicy defaultValuesPolicy, JsonFormatter formatter)
	: Converters.ObjectMapWithNonDefaultCtorConverter<TDeclaringType, TArgumentState>(serializable, argStateCtor, ctor, parameters, defaultValuesPolicy)
{
	private readonly FrozenDictionary<string, DeserializableProperty<TArgumentState>>? parametersByName = parameters.Readers?.Entries.ToFrozenDictionary(p => formatter.Encoding.GetString(p.Key.Span), p => p.Value, StringComparer.Ordinal);

	/// <inheritdoc/>
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
