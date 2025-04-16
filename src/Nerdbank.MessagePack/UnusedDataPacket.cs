// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack;

/// <summary>
/// When used as the type of a property or field on a data type,
/// captures deserialized data that could not be assigned to a known property.
/// </summary>
/// <example>
/// <para>The following data type recognizes its own declared properties and will avoid data loss when round-tripping data from a different declaration of the class that has additional properties.</para>
/// <code source="../../samples/cs/CustomizingSerialization.cs" region="VersionSafeObject" lang="C#" />
/// </example>
[TypeShape(Kind = TypeShapeKind.None)]
public abstract class UnusedDataPacket
{
	/// <summary>
	/// A stub method to ensure that no one outside this assembly can derive from this class.
	/// </summary>
	private protected abstract void NoExternalDerivation();

	/// <summary>
	/// Stores the data from deserializing an object that was serialized as a map of property names to values, specifically for the unrecognized property names.
	/// </summary>
	internal sealed class Map : UnusedDataPacket
	{
		private static readonly InternedBuffers Buffers = new();

		/// <summary>
		/// Gets the raw data that was not deserialized into a known property for an object serialized as a map.
		/// </summary>
		private readonly Dictionary<ReadOnlyMemory<byte>, RawMessagePack> values = new(ByteMemoryEqualityComparer.Ordinal);

		/// <summary>
		/// Gets the number of properties in this packet.
		/// </summary>
		internal int Count => this.values.Count;

		/// <summary>
		/// Interns a UTF-8 encoded property name to a heap-friendly memory buffer.
		/// This avoids repeated allocations of the same property name when it is used multiple times.
		/// </summary>
		/// <param name="propertyName">The UTF-8 encoded property name.</param>
		/// <returns>A permanent memory buffer representing the interned property name.</returns>
		internal static ReadOnlyMemory<byte> GetPropertyNameMemory(ReadOnlySpan<byte> propertyName) => Buffers.Intern(propertyName);

		/// <summary>
		/// Adds a new value to the unused data packet.
		/// </summary>
		/// <param name="propertyName">The name of the unrecognized property.</param>
		/// <param name="value">The property value.</param>
		internal void Add(ReadOnlySpan<byte> propertyName, in RawMessagePack value) => this.values.Add(GetPropertyNameMemory(propertyName), value.ToOwned());

		/// <inheritdoc cref="Add(ReadOnlySpan{byte}, in RawMessagePack)"/>
		internal void Add(ReadOnlyMemory<byte> propertyName, in RawMessagePack value) => this.values.Add(propertyName, value.ToOwned());

		/// <summary>
		/// Writes the property name/value pairs to the writer.
		/// </summary>
		/// <param name="writer">The writer to use.</param>
		internal void WriteTo(ref MessagePackWriter writer)
		{
			foreach (KeyValuePair<ReadOnlyMemory<byte>, RawMessagePack> kvp in this.values)
			{
				writer.WriteString(kvp.Key.Span);
				writer.WriteRaw(kvp.Value);
			}
		}

		/// <inheritdoc/>
		private protected override void NoExternalDerivation() => throw new NotImplementedException();
	}

	/// <summary>
	/// Stores the data from deserializing an object that was serialized as an array of values at unrecognized indices.
	/// </summary>
	internal sealed class Array : UnusedDataPacket
	{
		/// <summary>
		/// Gets the raw data that was not deserialized into a known property for an object serialized as an array.
		/// </summary>
		/// <remarks>
		/// The key in the dictionary is the zero-based index of the array element.
		/// </remarks>
		private readonly SortedDictionary<int, RawMessagePack> values = new();

		/// <summary>
		/// Gets the maximum index of an array element that was added to this packet.
		/// </summary>
		internal int MaxIndex { get; private set; }

		/// <summary>
		/// Adds a new value to the unused data packet.
		/// </summary>
		/// <param name="index">The index for the unused value.</param>
		/// <param name="value">The unused value.</param>
		internal void Add(int index, in RawMessagePack value)
		{
			if (index > this.MaxIndex)
			{
				this.MaxIndex = index;
			}

			this.values.Add(index, value.ToOwned());
		}

		/// <summary>
		/// Retrieves a value from the unused data packet, if one at a given index is known.
		/// </summary>
		/// <param name="index">The index for the value.</param>
		/// <param name="value">Receives the value at the specified <paramref name="index"/>.</param>
		/// <returns>A value indicating whether a value at the given index was found.</returns>
		internal bool TryGetValue(int index, out RawMessagePack value) => this.values.TryGetValue(index, out value);

		/// <summary>
		/// Gets a value indicating whether a value at the given index exists.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns><see langword="true"/> if a value is defined at the index; <see langword="false" /> otherwise.</returns>
		internal bool ContainsKey(int index) => this.values.ContainsKey(index);

		/// <summary>
		/// Writes the index/value pairs to the writer (for map mode).
		/// </summary>
		/// <param name="writer">The writer to use.</param>
		internal void WriteMapTo(ref MessagePackWriter writer)
		{
			foreach (KeyValuePair<int, RawMessagePack> kvp in this.values)
			{
				writer.Write(kvp.Key);
				writer.WriteRaw(kvp.Value);
			}
		}

		/// <inheritdoc/>
		private protected override void NoExternalDerivation() => throw new NotImplementedException();
	}
}
