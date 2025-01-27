// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using Microsoft.NET.StringTools;

namespace Nerdbank.MessagePack;

/// <summary>
/// Tracks the state for a particular serialization/deserialization operation to preserve object references.
/// </summary>
internal class ReferenceEqualityTracker : IPoolableObject
{
	private readonly Dictionary<object, int> serializedObjects = new(ReferenceEqualityComparer.Instance);
	private readonly List<object?> deserializedObjects = new();
	private int serializingObjectCounter;

	/// <inheritdoc/>
	public MessagePackSerializer? Owner { get; set; }

	/// <inheritdoc/>
	void IPoolableObject.Recycle()
	{
		this.serializingObjectCounter = 0;
		this.serializedObjects.Clear();
		this.deserializedObjects.Clear();
	}

	/// <summary>
	/// Writes an object to the stream, replacing the object with a reference if the object has been seen in this serialization before.
	/// </summary>
	/// <typeparam name="T">The type of value to be written.</typeparam>
	/// <param name="writer">The writer.</param>
	/// <param name="value">The object to write.</param>
	/// <param name="inner">The converter to use to write the object if it has not already been written.</param>
	/// <param name="context">The serialization context.</param>
	internal void WriteObject<T>(ref MessagePackWriter writer, T value, MessagePackConverter<T> inner, SerializationContext context)
	{
		Requires.NotNullAllowStructs(value);
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		if (this.Owner.InternStrings && value is string)
		{
			value = (T)(object)Strings.WeakIntern((string)(object)value);
		}

		if (this.serializedObjects.TryGetValue(value, out int referenceId))
		{
			// This object has already been written. Skip it this time.
			uint packLength = (uint)MessagePackWriter.GetEncodedLength(referenceId);
			writer.Write(new ExtensionHeader(this.Owner.LibraryExtensionTypeCodes.ObjectReference, packLength));
			writer.Write(referenceId);
		}
		else
		{
			this.serializedObjects.Add(value, this.serializingObjectCounter++);
			inner.Write(ref writer, value, context);
		}
	}

	/// <summary>
	/// Reads an object or its reference from the stream.
	/// </summary>
	/// <typeparam name="T">The type of object to read.</typeparam>
	/// <param name="reader">The reader.</param>
	/// <param name="inner">The converter to use to deserialize the object if it is not a reference.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>The reference to an object, whether it was deserialized fresh or just referenced.</returns>
	/// <exception cref="MessagePackSerializationException">Thrown if there is a dependency cycle detected or the <paramref name="inner"/> converter returned null unexpectedly.</exception>
	internal T ReadObject<T>(ref MessagePackReader reader, MessagePackConverter<T> inner, SerializationContext context)
	{
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		if (reader.NextMessagePackType == MessagePackType.Extension)
		{
			MessagePackReader provisionaryReader = reader.CreatePeekReader();
			ExtensionHeader extensionHeader = provisionaryReader.ReadExtensionHeader();
			if (extensionHeader.TypeCode == this.Owner.LibraryExtensionTypeCodes.ObjectReference)
			{
				int id = provisionaryReader.ReadInt32();
				reader = provisionaryReader;
				return (T?)this.deserializedObjects[id] ?? throw new MessagePackSerializationException("Unexpected null element in shared object array. Dependency cycle?");
			}
		}

		// Reserve our position in the array.
		int reservation = this.deserializedObjects.Count;
		this.deserializedObjects.Add(null);
		T? value = default;
		inner.Read(ref reader, ref value, context);
		if (value is null)
		{
			throw new MessagePackSerializationException("Converter returned null for non-null value.");
		}

		this.deserializedObjects[reservation] = value;
		return value;
	}

	/// <summary>
	/// An <see cref="IEqualityComparer{T}"/> that explicitly disregards any chance of by-value equality or hash code computation,
	/// since we very explicitly want to preserve <em>references</em> without accidentally combining two distinct objects that happen to be considered equal.
	/// </summary>
	private class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		internal static readonly ReferenceEqualityComparer Instance = new();

		private ReferenceEqualityComparer()
		{
		}

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		public int GetHashCode([DisallowNull] object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
