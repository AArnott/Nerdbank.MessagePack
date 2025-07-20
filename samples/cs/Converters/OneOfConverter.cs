// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
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
        
        value.Switch(
            value0 =>
            {
                writer.Write(0);
                context.GetConverter<T0>().Write(ref writer, value0, context);
            },
            value1 =>
            {
                writer.Write(1);
                context.GetConverter<T1>().Write(ref writer, value1, context);
            });
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
            0 => OneOf<T0, T1>.FromT0(context.GetConverter<T0>().Read(ref reader, context)),
            1 => OneOf<T0, T1>.FromT1(context.GetConverter<T1>().Read(ref reader, context)),
            _ => throw new MessagePackSerializationException($"Invalid OneOf type index: {typeIndex}. Expected 0 or 1.")
        };
    }
}

/// <summary>
/// Converts a <see cref="OneOf{T0, T1, T2}"/> object to and from MessagePack.
/// </summary>
/// <remarks>
/// This is an extension of the OneOf converter pattern to support 3 types.
/// The serialization format remains the same: [typeIndex, value].
/// </remarks>
/// <typeparam name="T0">The first possible type.</typeparam>
/// <typeparam name="T1">The second possible type.</typeparam>
/// <typeparam name="T2">The third possible type.</typeparam>
internal class OneOfConverter<T0, T1, T2> : MessagePackConverter<OneOf<T0, T1, T2>>
{
    public override void Write(ref MessagePackWriter writer, in OneOf<T0, T1, T2> value, SerializationContext context)
    {
        context.DepthStep();
        writer.WriteArrayHeader(2);
        
        value.Switch(
            value0 =>
            {
                writer.Write(0);
                context.GetConverter<T0>().Write(ref writer, value0, context);
            },
            value1 =>
            {
                writer.Write(1);
                context.GetConverter<T1>().Write(ref writer, value1, context);
            },
            value2 =>
            {
                writer.Write(2);
                context.GetConverter<T2>().Write(ref writer, value2, context);
            });
    }

    public override OneOf<T0, T1, T2> Read(ref MessagePackReader reader, SerializationContext context)
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
            0 => OneOf<T0, T1, T2>.FromT0(context.GetConverter<T0>().Read(ref reader, context)),
            1 => OneOf<T0, T1, T2>.FromT1(context.GetConverter<T1>().Read(ref reader, context)),
            2 => OneOf<T0, T1, T2>.FromT2(context.GetConverter<T2>().Read(ref reader, context)),
            _ => throw new MessagePackSerializationException($"Invalid OneOf type index: {typeIndex}. Expected 0, 1, or 2.")
        };
    }
}
#endregion