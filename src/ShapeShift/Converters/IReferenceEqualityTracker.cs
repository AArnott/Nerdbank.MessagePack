// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Converters;

/// <summary>
/// Reads and writes objects or their references when they already appeared in a (de)serialization stream.
/// </summary>
internal interface IReferenceEqualityTracker : IPoolableObject
{
	/// <summary>
	/// Gets the <see cref="IReferencePreservingManager"/> that is associated with this tracker.
	/// </summary>
	IReferencePreservingManager Manager { get; }

	/// <summary>
	/// Reads an object or its reference.
	/// </summary>
	/// <typeparam name="T">The type of object to read.</typeparam>
	/// <param name="reader">The reader.</param>
	/// <param name="inner">The converter to use for an actual object.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>The deserialized object.</returns>
	T ReadObject<T>(ref Reader reader, Converter<T> inner, SerializationContext context);

	/// <inheritdoc cref="ReadObject{T}(ref Reader, Converter{T}, SerializationContext)"/>
	ValueTask<T> ReadObjectAsync<T>(AsyncReader reader, Converter<T> inner, SerializationContext context);

	/// <summary>
	/// Writes an object or its reference.
	/// </summary>
	/// <typeparam name="T">The type of object to write.</typeparam>
	/// <param name="writer">The writer.</param>
	/// <param name="value">The object to be written.</param>
	/// <param name="inner">The converter to use for an actual object.</param>
	/// <param name="context">The serialization context.</param>
	void WriteObject<T>(ref Writer writer, T value, Converter<T> inner, SerializationContext context);

	/// <inheritdoc cref="WriteObject{T}(ref Writer, T, Converter{T}, SerializationContext)"/>
	ValueTask WriteObjectAsync<T>(AsyncWriter writer, T value, Converter<T> inner, SerializationContext context);
}
