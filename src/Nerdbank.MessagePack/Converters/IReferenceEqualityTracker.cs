// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.PolySerializer.Converters;

interface IReferenceEqualityTracker : IPoolableObject
{
	IReferencePreservingManager Manager { get; }

	T ReadObject<T>(ref Reader reader, Converter<T> inner, SerializationContext context);

	ValueTask<T> ReadObjectAsync<T>(AsyncReader reader, Converter<T> inner, SerializationContext context);

	void WriteObject<T>(ref Writer writer, T value, Converter<T> inner, SerializationContext context);

	ValueTask WriteObjectAsync<T>(AsyncWriter writer, T value, Converter<T> inner, SerializationContext context);
}
