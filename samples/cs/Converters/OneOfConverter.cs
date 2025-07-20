// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;

namespace Samples.Converters;

#region Converter
/// <summary>
/// Converts a <see cref="OneOf{T0, T1}"/> object to and from MessagePack.
/// </summary>
/// <remarks>
/// <para>
/// OneOf is a discriminated union type found in the "OneOf" package.
/// This converter serializes OneOf values as a 2-element array where:
/// </para>
/// <list type="bullet">
/// <item>The first element is an integer indicating which type is active (0 for T0, 1 for T1)</item>
/// <item>The second element contains the actual value of the active type</item>
/// </list>
/// <para>
/// This converter can be extended to support OneOf variants with more type parameters
/// by following the same pattern and handling additional type indices.
/// </para>
/// </remarks>
/// <typeparam name="T0">The first possible type.</typeparam>
/// <typeparam name="T1">The second possible type.</typeparam>
internal class OneOfConverter<T0, T1> : MessagePackConverter<OneOf<T0, T1>>
{
    public override void Write(ref MessagePackWriter writer, in OneOf<T0, T1> value, SerializationContext context)
    {
        context.DepthStep();
        writer.WriteArrayHeader(2);
        writer.Write(value.Index);

        switch (value.Index)
        {
            case 0:
                context.GetConverter<T0>(null).Write(ref writer, value.AsT0, context);
                break;
            case 1:
                context.GetConverter<T1>(null).Write(ref writer, value.AsT1, context);
                break;
            default:
                throw new MessagePackSerializationException($"Invalid OneOf type index: {value.Index}. Expected 0 or 1.");
        }
    }

    public override OneOf<T0, T1> Read(ref MessagePackReader reader, SerializationContext context)
    {
        context.DepthStep();
        int arrayLength = reader.ReadArrayHeader();
        if (arrayLength != 2)
        {
            throw new MessagePackSerializationException($"Expected array length of 2, but got {arrayLength}.");
        }

        int typeIndex = reader.ReadInt32();
        return typeIndex switch
        {
            0 => context.GetConverter<T0>(null).Read(ref reader, context)!,
            1 => context.GetConverter<T1>(null).Read(ref reader, context)!,
            _ => throw new MessagePackSerializationException($"Invalid OneOf type index: {typeIndex}. Expected 0 or 1."),
        };
    }
}
#endregion
