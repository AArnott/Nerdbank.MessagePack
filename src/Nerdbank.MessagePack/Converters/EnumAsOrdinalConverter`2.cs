// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes <see langword="enum" /> types as their underlying integral type.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <typeparam name="TUnderlyingType">The underlying integer type.</typeparam>
internal class EnumAsOrdinalConverter<TEnum, TUnderlyingType>(MessagePackConverter<TUnderlyingType> primitiveConverter) : MessagePackConverter<TEnum>
{
	/// <inheritdoc/>
	public override TEnum? Deserialize(ref MessagePackReader reader, SerializationContext context) => (TEnum?)(object?)primitiveConverter.Deserialize(ref reader, context);

	/// <inheritdoc/>
	public override void Serialize(ref MessagePackWriter writer, ref TEnum? value, SerializationContext context)
	{
		TUnderlyingType? intValue = (TUnderlyingType?)(object?)value;
		primitiveConverter.Serialize(ref writer, ref intValue, context);
	}
}
