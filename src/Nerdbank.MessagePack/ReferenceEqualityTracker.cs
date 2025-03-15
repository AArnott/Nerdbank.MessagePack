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
	private readonly Dictionary<object, (int, bool)> serializedObjects = new(ReferenceEqualityComparer.Instance);
	private readonly List<object?> deserializedObjects = new();
	private int serializingObjectCounter;

	/// <inheritdoc/>
	public MessagePackSerializer? Owner { get; set; }

	/// <summary>
	/// Gets the active preservation mode.
	/// </summary>
	private ReferencePreservationMode Mode => this.Owner?.PreserveReferences ?? throw new InvalidOperationException("No owner set.");

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

		if (this.TryGetSerializedObject(value, out int referenceId))
		{
			// This object has already been written. Skip it this time.
			uint packLength = (uint)MessagePackWriter.GetEncodedLength(referenceId);
			writer.Write(new ExtensionHeader(this.Owner.LibraryExtensionTypeCodes.ObjectReference, packLength));
			writer.Write(referenceId);
		}
		else
		{
			int assignedIndex = this.serializingObjectCounter++;
			this.serializedObjects.Add(value, (assignedIndex, false));
			inner.Write(ref writer, value, context);
			this.serializedObjects[value] = (assignedIndex, true);
		}
	}

	/// <summary>
	/// Writes an object to the stream, replacing the object with a reference if the object has been seen in this serialization before.
	/// </summary>
	/// <typeparam name="T">The type of value to be written.</typeparam>
	/// <param name="writer">The writer.</param>
	/// <param name="value">The object to write.</param>
	/// <param name="inner">The converter to use to write the object if it has not already been written.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>An async task.</returns>
	[Experimental("NBMsgPackAsync")]
	internal async ValueTask WriteObjectAsync<T>(MessagePackAsyncWriter writer, T value, MessagePackConverter<T> inner, SerializationContext context)
	{
		Requires.NotNullAllowStructs(value);
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		if (this.Owner.InternStrings && value is string)
		{
			value = (T)(object)Strings.WeakIntern((string)(object)value);
		}

		if (this.TryGetSerializedObject(value, out int referenceId))
		{
			// This object has already been written. Skip it this time.
			uint packLength = (uint)MessagePackWriter.GetEncodedLength(referenceId);
			MessagePackWriter syncWriter = writer.CreateWriter();
			syncWriter.Write(new ExtensionHeader(this.Owner.LibraryExtensionTypeCodes.ObjectReference, packLength));
			syncWriter.Write(referenceId);
			writer.ReturnWriter(ref syncWriter);
			await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
		}
		else
		{
			int assignedIndex = this.serializingObjectCounter++;
			this.serializedObjects.Add(value, (assignedIndex, false));
			await inner.WriteAsync(writer, value, context).ConfigureAwait(false);
			this.serializedObjects[value] = (assignedIndex, true);
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
				return (T?)this.GetDeserializedObject(id)!;
			}
		}

		// Reserve our position in the array.
		context.ReferenceIndex = this.deserializedObjects.Count;
		this.deserializedObjects.Add(null);
		T value = inner.Read(ref reader, context) ?? throw new MessagePackSerializationException("Converter returned null for non-null value.");
		this.deserializedObjects[context.ReferenceIndex] = value;
		return value;
	}

	/// <summary>
	/// Reports the construction of an object and stores it if cycles are allowed.
	/// </summary>
	/// <param name="value">The constructed object to be reported and stored for reference.</param>
	/// <param name="referenceIndex">Indicates the position in the collection where the object should be stored.</param>
	internal void ReportObjectConstructed(object? value, int referenceIndex)
	{
		if (this.Mode == ReferencePreservationMode.AllowCycles)
		{
			Verify.Operation(this.deserializedObjects[referenceIndex] is null, "The object was already constructed and should not be reported again.");
			this.deserializedObjects[referenceIndex] = value;
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
	[Experimental("NBMsgPackAsync")]
	internal async ValueTask<T> ReadObjectAsync<T>(MessagePackAsyncReader reader, MessagePackConverter<T> inner, SerializationContext context)
	{
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		MessagePackStreamingReader streamingReader = reader.CreateStreamingReader();
		MessagePackType peekType;
		while (streamingReader.TryPeekNextMessagePackType(out peekType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (peekType == MessagePackType.Extension)
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			MessagePackReader syncReader = reader.CreateBufferedReader();
			MessagePackReader provisionaryReader = syncReader.CreatePeekReader();
			ExtensionHeader extensionHeader = provisionaryReader.ReadExtensionHeader();
			if (extensionHeader.TypeCode == this.Owner.LibraryExtensionTypeCodes.ObjectReference)
			{
				int id = provisionaryReader.ReadInt32();
				syncReader = provisionaryReader;
				reader.ReturnReader(ref syncReader);
				return (T?)this.GetDeserializedObject(id)!;
			}

			reader.ReturnReader(ref syncReader);
		}

		// Reserve our position in the array.
		context.ReferenceIndex = this.deserializedObjects.Count;
		this.deserializedObjects.Add(null);
		T value = (await inner.ReadAsync(reader, context).ConfigureAwait(false)) ?? throw new MessagePackSerializationException("Converter returned null for non-null value.");
		this.deserializedObjects[context.ReferenceIndex] = value;
		return value;
	}

	private object? GetDeserializedObject(int id)
	{
		if (this.deserializedObjects[id] is object result)
		{
			// No cycle detected.
			return result;
		}
		else if (this.Mode == ReferencePreservationMode.AllowCycles)
		{
			// Reference cycle detected and allowed.
			// But we don't have the object yet, because the converter responsible for creating it has not or cannot report the object's reference back (yet).
			// This may be because the object has a non-default constructor (or "required" properties) and
			// and the cycle implicates the properties that must be constructed before the object itself.
			throw new NotSupportedException("A reference cycle cannot be reconstructed due to a limitation in the converters or the data types themselves.");
		}
		else
		{
			// Reference cycle detected and not allowed.
			throw new MessagePackSerializationException("Disallowed reference cycle detected.");
		}
	}

	private bool TryGetSerializedObject(object value, out int referenceId)
	{
		if (!this.serializedObjects.TryGetValue(value, out (int ReferenceId, bool Done) slot))
		{
			referenceId = 0;
			return false;
		}

		if (!slot.Done && this.Mode != ReferencePreservationMode.AllowCycles)
		{
			throw new MessagePackSerializationException("Disallowed reference cycle detected.");
		}

		referenceId = slot.ReferenceId;
		return true;
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
