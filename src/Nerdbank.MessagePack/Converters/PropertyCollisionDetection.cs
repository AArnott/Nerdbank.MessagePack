// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Tracks a default constructor object's initialization of properties to verify that no property is set more than once.
/// </summary>
/// <remarks>
/// A security threat that has been exploited in the wild exploits a double-set trick where a property might first be set
/// to an acceptable value and later set to an illegal value, such that some security filtering parsers will accept the input
/// based on the first value and then later a deserializer would take the last value, exploiting some vulnerability.
/// </remarks>
internal struct PropertyCollisionDetection
{
	private readonly IReadOnlyList<IPropertyShape> properties;
	private readonly uint length;
	private readonly BitArray? largeSet;
	private ulong smallSet;

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyCollisionDetection"/> struct.
	/// </summary>
	/// <param name="properties">The list of properties to track.</param>
	internal PropertyCollisionDetection(IReadOnlyList<IPropertyShape> properties)
	{
		this.properties = properties;
		this.length = (uint)properties.Count;
		if (this.length > 64)
		{
			this.largeSet = new BitArray((int)this.length);
		}
	}

	/// <summary>
	/// Marks the property as read or throws an exception if the property has already been added.
	/// </summary>
	/// <param name="propertyIndex">The index of the property we're marking as read.</param>
	internal void MarkAsRead(int propertyIndex)
	{
		if (!this.TryMarkAsRead(propertyIndex))
		{
			this.ThrowAlreadyAssigned(propertyIndex);
		}
	}

	/// <summary>
	/// Attempts to mark the property as read without throwing an exception.
	/// </summary>
	/// <param name="propertyIndex">The index of the property to mark as read.</param>
	/// <returns><see langword="true" /> if this property has not previously been deserialized; <see langword="false" /> otherwise.</returns>
	internal bool TryMarkAsRead(int propertyIndex)
	{
		if ((uint)propertyIndex >= this.length)
		{
			Throw();
			static void Throw() => throw new ArgumentOutOfRangeException(nameof(propertyIndex), "Index is out of range.");
		}

		bool isUnset;
		if (this.largeSet is BitArray bitArray)
		{
			isUnset = !bitArray[propertyIndex];
			bitArray[propertyIndex] = true;
		}
		else
		{
			ulong flag = 1UL << propertyIndex;
			isUnset = (this.smallSet & flag) == 0;
			this.smallSet |= flag;
		}

		return isUnset;
	}

	[DoesNotReturn]
	private readonly void ThrowAlreadyAssigned(int index)
	{
		throw new MessagePackSerializationException($"The property '{this.properties[index].Name}' has already been assigned a value.")
		{
			Code = MessagePackSerializationException.ErrorCode.DoublePropertyAssignment,
		};
	}
}
