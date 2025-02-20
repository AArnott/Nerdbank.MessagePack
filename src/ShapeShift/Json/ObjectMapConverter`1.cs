// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;

namespace ShapeShift.Json;

/// <summary>
/// An object map converter for the JSON serializer.
/// </summary>
/// <typeparam name="T"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/typeparam[@name='T']"/></typeparam>
/// <param name="serializable"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='serializable']"/></param>
/// <param name="deserializable"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='deserializable']"/></param>
/// <param name="ctor"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='ctor']"/></param>
/// <param name="defaultValuesPolicy"><inheritdoc cref="Converters.ObjectMapConverter{T}" path="/param[@name='defaultValuesPolicy']"/></param>
/// <param name="formatter">The formatter used for the serializer.</param>
internal class ObjectMapConverter<T>(MapSerializableProperties<T> serializable, MapDeserializableProperties<T>? deserializable, Func<T>? ctor, SerializeDefaultValuesPolicy defaultValuesPolicy, JsonFormatter formatter)
	: Converters.ObjectMapConverter<T>(serializable, deserializable, ctor, defaultValuesPolicy)
{
	private readonly FrozenDictionary<string, DeserializableProperty<T>>? propertiesByName = deserializable?.Readers?.Entries.ToFrozenDictionary(p => formatter.Encoding.GetString(p.Key.Span), p => p.Value, StringComparer.Ordinal);

	/// <inheritdoc/>
	protected override bool TryLookupProperty(ref Reader reader, out DeserializableProperty<T> deserializableArg)
	{
		string? propertyName = reader.ReadString();
		if (propertyName is not null && this.propertiesByName is not null)
		{
			return this.propertiesByName.TryGetValue(propertyName, out deserializableArg);
		}

		deserializableArg = default;
		return false;
	}
}
