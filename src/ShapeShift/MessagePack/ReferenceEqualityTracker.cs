// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft;
using Microsoft.NET.StringTools;
using ShapeShift.MessagePack.Converters;

namespace ShapeShift.MessagePack;

/// <summary>
/// Tracks the state for a particular serialization/deserialization operation to preserve object references.
/// </summary>
internal class ReferenceEqualityTracker : IPoolableObject, IReferenceEqualityTracker
{
	private readonly Dictionary<object, int> serializedObjects = new(ReferenceEqualityComparer.Instance);
	private readonly List<object?> deserializedObjects = new();
	private int serializingObjectCounter;

	/// <summary>
	/// Gets or sets the owning serializer for this instance.
	/// </summary>
	public MessagePackSerializer? Owner { get; set; }

	/// <inheritdoc/>
	SerializerBase? IPoolableObject.Owner
	{
		get => this.Owner;
		set => this.Owner = (MessagePackSerializer?)value;
	}

	/// <summary>
	/// Gets the reference preserving manager for this instance.
	/// </summary>
	public IReferencePreservingManager Manager => MsgPackReferencePreservingManager.Instance;

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
	public void WriteObject<T>(ref Writer writer, T value, Converter<T> inner, SerializationContext context)
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
			uint packLength = (uint)writer.Formatter.GetEncodedLength(referenceId);
			MessagePackFormatter formatter = (MessagePackFormatter)writer.Formatter;
			formatter.Write(ref writer.Buffer, new ExtensionHeader(this.Owner.LibraryExtensionTypeCodes.ObjectReference, packLength));
			writer.Write(referenceId);
		}
		else
		{
			this.serializedObjects.Add(value, this.serializingObjectCounter++);
			inner.Write(ref writer, value, context);
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
	public async ValueTask WriteObjectAsync<T>(AsyncWriter writer, T value, Converter<T> inner, SerializationContext context)
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
			uint packLength = (uint)writer.Formatter.GetEncodedLength(referenceId);
			Writer syncWriter = writer.CreateWriter();
			MessagePackFormatter formatter = (MessagePackFormatter)writer.Formatter;
			formatter.Write(ref syncWriter.Buffer, new ExtensionHeader(this.Owner.LibraryExtensionTypeCodes.ObjectReference, packLength));
			syncWriter.Write(referenceId);
			writer.ReturnWriter(ref syncWriter);
			await writer.FlushIfAppropriateAsync(context).ConfigureAwait(false);
		}
		else
		{
			this.serializedObjects.Add(value, this.serializingObjectCounter++);
			await inner.WriteAsync(writer, value, context).ConfigureAwait(false);
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
	/// <exception cref="SerializationException">Thrown if there is a dependency cycle detected or the <paramref name="inner"/> converter returned null unexpectedly.</exception>
	public T ReadObject<T>(ref Reader reader, Converter<T> inner, SerializationContext context)
	{
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		MessagePackDeformatter deformatter = (MessagePackDeformatter)reader.Deformatter;
		if (deformatter.PeekNextMessagePackType(reader) == MessagePackType.Extension)
		{
			Reader provisionaryReader = reader;
			ExtensionHeader extensionHeader = deformatter.ReadExtensionHeader(ref provisionaryReader);
			if (extensionHeader.TypeCode == this.Owner.LibraryExtensionTypeCodes.ObjectReference)
			{
				int id = provisionaryReader.ReadInt32();
				reader = provisionaryReader;
				return (T?)this.deserializedObjects[id] ?? throw new SerializationException("Unexpected null element in shared object array. Dependency cycle?");
			}
		}

		// Reserve our position in the array.
		int reservation = this.deserializedObjects.Count;
		this.deserializedObjects.Add(null);
		T value = inner.Read(ref reader, context) ?? throw new SerializationException("Converter returned null for non-null value.");
		this.deserializedObjects[reservation] = value;
		return value;
	}

	/// <summary>
	/// Reads an object or its reference from the stream.
	/// </summary>
	/// <typeparam name="T">The type of object to read.</typeparam>
	/// <param name="reader">The reader.</param>
	/// <param name="inner">The converter to use to deserialize the object if it is not a reference.</param>
	/// <param name="context">The serialization context.</param>
	/// <returns>The reference to an object, whether it was deserialized fresh or just referenced.</returns>
	/// <exception cref="SerializationException">Thrown if there is a dependency cycle detected or the <paramref name="inner"/> converter returned null unexpectedly.</exception>
	[Experimental("NBMsgPackAsync")]
	public async ValueTask<T> ReadObjectAsync<T>(AsyncReader reader, Converter<T> inner, SerializationContext context)
	{
		Verify.Operation(this.Owner is not null, $"{nameof(this.Owner)} must be set before use.");

		StreamingReader streamingReader = reader.CreateStreamingReader();
		MessagePackDeformatter deformatter = (MessagePackDeformatter)reader.Deformatter;
		MessagePackType peekType;
		while (deformatter.StreamingDeformatter.TryPeekNextCode(streamingReader.Reader, out peekType).NeedsMoreBytes())
		{
			streamingReader = new(await streamingReader.FetchMoreBytesAsync().ConfigureAwait(false));
		}

		reader.ReturnReader(ref streamingReader);
		if (peekType == MessagePackType.Extension)
		{
			await reader.BufferNextStructureAsync(context).ConfigureAwait(false);
			Reader syncReader = reader.CreateBufferedReader();
			Reader provisionaryReader = syncReader;
			ExtensionHeader extensionHeader = deformatter.ReadExtensionHeader(ref provisionaryReader);
			if (extensionHeader.TypeCode == this.Owner.LibraryExtensionTypeCodes.ObjectReference)
			{
				int id = provisionaryReader.ReadInt32();
				syncReader = provisionaryReader;
				reader.ReturnReader(ref syncReader);
				return (T?)this.deserializedObjects[id] ?? throw new SerializationException("Unexpected null element in shared object array. Dependency cycle?");
			}

			reader.ReturnReader(ref syncReader);
		}

		// Reserve our position in the array.
		int reservation = this.deserializedObjects.Count;
		this.deserializedObjects.Add(null);
		T value = (await inner.ReadAsync(reader, context).ConfigureAwait(false)) ?? throw new SerializationException("Converter returned null for non-null value.");
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
