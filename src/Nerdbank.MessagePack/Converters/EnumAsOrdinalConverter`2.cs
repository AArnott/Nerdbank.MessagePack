// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Converters;

/// <summary>
/// Serializes <see langword="enum" /> types as their underlying integral type.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <typeparam name="TUnderlyingType">The underlying integer type.</typeparam>
internal class EnumAsOrdinalConverter<TEnum, TUnderlyingType>(MessagePackConverter<TUnderlyingType> primitiveConverter) : MessagePackConverter<TEnum>
	where TEnum : struct, Enum
{
	/// <inheritdoc/>
	public override TEnum Read(ref MessagePackReader reader, SerializationContext context) => (TEnum)(object)primitiveConverter.Read(ref reader, context)!;

	/// <inheritdoc/>
	public override void Write(ref MessagePackWriter writer, in TEnum value, SerializationContext context)
	{
		primitiveConverter.Write(ref writer, (TUnderlyingType)(object)value, context);
	}
}
