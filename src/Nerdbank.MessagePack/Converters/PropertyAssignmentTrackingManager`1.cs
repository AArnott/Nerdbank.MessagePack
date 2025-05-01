// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Manages tracking of property assignments for a given type of object.
/// </summary>
/// <typeparam name="T">The type of object to be deserialized.</typeparam>
internal class PropertyAssignmentTrackingManager<T>
{
	private readonly ThreadLocal<Stack<BitArray>>? trackers;

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyAssignmentTrackingManager{T}"/> class.
	/// </summary>
	/// <param name="typeShape">The shape of the type.</param>
	internal PropertyAssignmentTrackingManager(IObjectTypeShape<T> typeShape)
	{
		this.TypeShape = typeShape;
		if (typeShape.Properties.Count > 64)
		{
			this.trackers = new(() => new());
		}
	}

	/// <summary>
	/// Gets the type shape.
	/// </summary>
	private IObjectTypeShape<T> TypeShape { get; }

	/// <summary>
	/// Creates a tracker for a new object to be deserialized.
	/// </summary>
	/// <returns>The tracker.</returns>
	internal Tracker CreateTracker() => new(this);

	private BitArray TakeBitArray()
	{
		Assumes.NotNull(this.trackers);
		if (!this.trackers.Value!.TryPop(out BitArray? result))
		{
			result = new(this.TypeShape.Properties.Count - 64);
		}

		return result;
	}

	private void ReturnBitArray(BitArray bitArray)
	{
#if NET
		if (bitArray.HasAnySet())
#endif
		{
			for (int i = 0; i < bitArray.Length; i++)
			{
				bitArray[i] = false;
			}
		}

		Assumes.NotNull(this.trackers);
		this.trackers.Value!.Push(bitArray);
	}

	/// <summary>
	/// Tracks an individual object deserialization.
	/// </summary>
	internal struct Tracker
	{
		private readonly PropertyAssignmentTrackingManager<T> owner;
		private ulong properties64;
		private BitArray? propertiesBitArray;

		/// <summary>
		/// Initializes a new instance of the <see cref="Tracker"/> struct.
		/// </summary>
		/// <param name="owner">The owner of this tracker.</param>
		internal Tracker(PropertyAssignmentTrackingManager<T> owner)
		{
			this.owner = owner;

			if (owner.TypeShape.Properties.Count > 64)
			{
				this.propertiesBitArray = owner.TakeBitArray();
			}
		}

		/// <summary>
		/// Gets the number of properties in the type shape.
		/// </summary>
		public int Count => this.owner.TypeShape.Properties.Count;

		/// <summary>
		/// Records that a property at the specified index has been assigned a value.
		/// </summary>
		/// <param name="index">The index of the property. Must be within the range [0, <see cref="Count" />).</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is outside the allowed range.</exception>"
		/// <exception cref="InvalidOperationException">Thrown if the indexed property has already been set.</exception>
		internal void ReportPropertyAssignment(int index)
		{
			Requires.Range(index >= 0 && index < this.Count, nameof(index));

			if (index < 64)
			{
				ulong bit = 1UL << index;
				ulong existing = this.properties64;
				if ((existing & bit) != 0)
				{
					this.ThrowAlreadyAssigned(index);
				}

				this.properties64 = existing | bit;
			}
			else
			{
				Assumes.NotNull(this.propertiesBitArray);
				int offset = index - 64;
				if (this.propertiesBitArray[offset])
				{
					this.ThrowAlreadyAssigned(index);
				}

				this.propertiesBitArray[offset] = true;
			}
		}

		/// <summary>
		/// Reports that deserialization of the object is complete.
		/// </summary>
		internal void ReportDeserializationComplete()
		{
			if (this.propertiesBitArray is not null)
			{
				this.owner.ReturnBitArray(this.propertiesBitArray);
				this.propertiesBitArray = null;
			}
		}

		[DoesNotReturn]
		private readonly void ThrowAlreadyAssigned(int index)
		{
			throw new InvalidOperationException($"The property '{this.owner.TypeShape.Properties[index].Name}' has already been assigned.");
		}
	}
}
