// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft;

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Manages tracking of property assignments for a given type of object.
/// </summary>
/// <typeparam name="T">The type of object to be deserialized.</typeparam>
internal class PropertyAssignmentTrackingManager<T> : IPropertyAssignmentTrackingManager
{
	private readonly ThreadLocal<Stack<BitArray>>? trackers;
	private readonly ImmutableArray<IPropertyShape> properties;
	private readonly ImmutableArray<IParameterShape> parameters;
	private readonly ulong requiredParameters64;
	private readonly BitArray? requiredParametersBitArray;
	private readonly bool verifyRequiredPropertiesSet;

	/// <summary>
	/// Initializes a new instance of the <see cref="PropertyAssignmentTrackingManager{T}"/> class.
	/// </summary>
	/// <param name="typeShape">The shape of the type.</param>
	/// <param name="verifyRequiredPropertiesSet"><see langword="true" /> to throw when a deserialized object doesn't have explicit values for each required property.</param>
	internal PropertyAssignmentTrackingManager(IObjectTypeShape<T> typeShape, bool verifyRequiredPropertiesSet)
	{
		this.TypeShape = typeShape;

		bool requiresBitArray;
		if (typeShape.Constructor is { Parameters: { Count: > 0 } parameters })
		{
			ImmutableArray<IParameterShape>.Builder parametersBuilder = ImmutableArray.CreateBuilder<IParameterShape>(parameters.Count);
			ulong requiredParameters64 = 0;
			BitArray? requiredParametersBitArray = verifyRequiredPropertiesSet && parameters.Count > 64 ? new(parameters.Count - 64) : null;
			bool anyRequired = false;
			foreach (IParameterShape parameter in parameters)
			{
				if (parameter is { ParameterType: UnusedDataPacket })
				{
					continue;
				}

				int index = parametersBuilder.Count;
				parametersBuilder.Add(parameter);
				if (verifyRequiredPropertiesSet && parameter.IsRequired)
				{
					anyRequired = true;
					if (index <= 64)
					{
						requiredParameters64 |= 1UL << index;
					}
					else
					{
						requiredParametersBitArray![index - 64] = true;
					}
				}
			}

			this.parameters = parametersBuilder.Count == parametersBuilder.Capacity ? parametersBuilder.MoveToImmutable() : parametersBuilder.ToImmutable();
			this.requiredParameters64 = requiredParameters64;
			this.requiredParametersBitArray = requiredParametersBitArray;
			requiresBitArray = this.parameters.Length > 64;
			this.verifyRequiredPropertiesSet = anyRequired;
		}
		else
		{
			ImmutableArray<IPropertyShape>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<IPropertyShape>(typeShape.Properties.Count);
			foreach (IPropertyShape property in typeShape.Properties)
			{
				if (property is { PropertyType: UnusedDataPacket })
				{
					continue;
				}

				propertiesBuilder.Add(property);
			}

			this.properties = propertiesBuilder.Count == propertiesBuilder.Capacity ? propertiesBuilder.MoveToImmutable() : propertiesBuilder.ToImmutable();
			requiresBitArray = this.properties.Length > 64;
		}

		if (requiresBitArray)
		{
			this.trackers = new(() => new());
		}
	}

	/// <summary>
	/// Gets the type shape.
	/// </summary>
	private IObjectTypeShape<T> TypeShape { get; }

	/// <inheritdoc/>
	public int GetPropertyAssignmentIndex(IPropertyShape property) => this.properties.IsDefault ? -1 : this.properties.IndexOf(property);

	/// <inheritdoc/>
	public int GetParameterAssignmentIndex(IParameterShape parameter) => this.parameters.IndexOf(parameter);

	/// <summary>
	/// Creates a tracker for a new object to be deserialized.
	/// </summary>
	/// <returns>The tracker.</returns>
	internal Tracker CreateTracker() => new(this);

	private BitArray? TakeBitArray()
	{
		if (this.trackers is null)
		{
			return null;
		}

		if (!this.trackers.Value!.TryPop(out BitArray? result))
		{
			result = new(this.TypeShape.Properties.Count - 64);
		}

		return result;
	}

	private void ReturnBitArray(BitArray bitArray)
	{
		Assumes.NotNull(this.trackers);
		bitArray.SetAll(false);
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
			this.propertiesBitArray = owner.TakeBitArray();
		}

		/// <summary>
		/// Gets the number of properties in the type shape.
		/// </summary>
		public int Count => this.owner.parameters.IsDefault ? this.owner.properties.Length : this.owner.parameters.Length;

		/// <summary>
		/// Records that a property at the specified index has been assigned a value.
		/// </summary>
		/// <param name="index">The index of the property. Must be within the range [-1, <see cref="Count" />).</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is outside the allowed range.</exception>"
		/// <exception cref="InvalidOperationException">Thrown if the indexed property has already been set.</exception>
		internal void ReportPropertyAssignment(int index)
		{
			if (index == -1)
			{
				// This property is not meant to be tracked.
				return;
			}

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
			// Verify that required properties have been set.
			MessagePackSerializationException? missingRequiredPropertiesException = null;
			if (this.owner.verifyRequiredPropertiesSet)
			{
				ulong missingIndexes64 = this.owner.requiredParameters64 & ~this.properties64;
				BitArray? missingIndexesBitArray = this.owner.requiredParametersBitArray is not null && this.propertiesBitArray is not null ? this.propertiesBitArray.Not().And(this.owner.requiredParametersBitArray) : null;

				if (missingIndexes64 != 0 || missingIndexesBitArray?.HasAnySet() is true)
				{
					// Some required properties have not been set.
					// Assemble a message that indicates which arguments are missing.
					StringBuilder builder = new("Missing values for required properties: ");

					if (missingIndexes64 != 0)
					{
						for (int i = 0; i < 64 && i < this.Count; i++)
						{
							if ((missingIndexes64 & (1UL << i)) != 0)
							{
								builder.Append(this.GetPropertyOrParameterName(i));
								builder.Append(", ");
							}
						}
					}

					if (missingIndexesBitArray is not null)
					{
						for (int i = 0; i < missingIndexesBitArray.Length && i + 64 < this.Count; i++)
						{
							if (missingIndexesBitArray[i])
							{
								builder.Append(this.GetPropertyOrParameterName(i + 64));
								builder.Append(", ");
							}
						}
					}

					builder.Length -= 2; // Remove the trailing comma and space.

					missingRequiredPropertiesException = new(builder.ToString()) { Code = MessagePackSerializationException.ErrorCode.MissingRequiredProperty };
				}
			}

			if (this.propertiesBitArray is not null)
			{
				this.owner.ReturnBitArray(this.propertiesBitArray);
				this.propertiesBitArray = null;
			}

			if (missingRequiredPropertiesException is not null)
			{
				throw missingRequiredPropertiesException;
			}
		}

		private string GetPropertyOrParameterName(int index) => this.owner.parameters.IsDefault ? this.owner.properties[index].Name : this.owner.parameters[index].Name;

		[DoesNotReturn]
		private readonly void ThrowAlreadyAssigned(int index)
		{
			throw new MessagePackSerializationException($"The property '{this.owner.TypeShape.Properties[index].Name}' has already been assigned.") { Code = MessagePackSerializationException.ErrorCode.DoublePropertyAssignment };
		}
	}
}

/// <summary>
/// A non-generic interface to provide index lookups for a <see cref="PropertyAssignmentTrackingManager{T}"/>.
/// </summary>
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "It is a helper to the class above it.")]
internal interface IPropertyAssignmentTrackingManager
{
	/// <summary>
	/// Gets a unique assignment tracking index for use with <see cref="PropertyAssignmentTrackingManager{T}.Tracker.ReportPropertyAssignment(int)"/> for the given property.
	/// </summary>
	/// <param name="property">The property.</param>
	/// <returns>A unique index.</returns>
	int GetPropertyAssignmentIndex(IPropertyShape property);

	/// <summary>
	/// Gets a unique assignment tracking index for use with <see cref="PropertyAssignmentTrackingManager{T}.Tracker.ReportPropertyAssignment(int)"/> for the given parameter.
	/// </summary>
	/// <param name="parameter">The parameter.</param>
	/// <returns>A unique index.</returns>
	int GetParameterAssignmentIndex(IParameterShape parameter);
}
