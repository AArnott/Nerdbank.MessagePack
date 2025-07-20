// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
#if NET
internal class OneOfConverter<T0, T1> : MessagePackConverter<OneOf<T0, T1>>
#else
internal class OneOfConverter<T0, T1> : MessagePackConverter<OneOf<T0, T1>>
#endif
{
    public override void Write(ref MessagePackWriter writer, in OneOf<T0, T1> value, SerializationContext context)
    {
        context.DepthStep();
        writer.WriteArrayHeader(2);
        
        if (value.IsT0)
        {
            writer.Write(0);
            // For this sample, we'll handle common primitive types directly
            // For a production implementation, you'd want a more sophisticated type dispatch
            WriteValue(ref writer, value.AsT0, context);
        }
        else
        {
            writer.Write(1);
            WriteValue(ref writer, value.AsT1, context);
        }
    }

    private static void WriteValue<T>(ref MessagePackWriter writer, T value, SerializationContext context)
    {
        // Handle common primitive types directly
        switch (value)
        {
            case string s:
                writer.Write(s);
                break;
            case int i:
                writer.Write(i);
                break;
            case bool b:
                writer.Write(b);
                break;
            case double d:
                writer.Write(d);
                break;
            case float f:
                writer.Write(f);
                break;
            case long l:
                writer.Write(l);
                break;
            case null:
                writer.WriteNil();
                break;
            default:
                // For complex types, you would need to get a converter
                // This is a simplified sample implementation
                throw new NotSupportedException($"Type {typeof(T)} is not supported in this sample converter. For complex types, implement a more sophisticated converter that can handle type shapes.");
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
            0 => OneOf<T0, T1>.FromT0(ReadValue<T0>(ref reader, context)),
            1 => OneOf<T0, T1>.FromT1(ReadValue<T1>(ref reader, context)),
            _ => throw new MessagePackSerializationException($"Invalid OneOf type index: {typeIndex}. Expected 0 or 1.")
        };
    }

    private static T ReadValue<T>(ref MessagePackReader reader, SerializationContext context)
    {
        // Handle common primitive types directly
        if (typeof(T) == typeof(string))
        {
            return (T)(object)reader.ReadString()!;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)reader.ReadInt32();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)reader.ReadBoolean();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)reader.ReadDouble();
        }
        else if (typeof(T) == typeof(float))
        {
            return (T)(object)reader.ReadSingle();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)reader.ReadInt64();
        }
        else
        {
            // For complex types, you would need to get a converter
            // This is a simplified sample implementation
            throw new NotSupportedException($"Type {typeof(T)} is not supported in this sample converter. For complex types, implement a more sophisticated converter that can handle type shapes.");
        }
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
#if NET
internal class OneOfConverter<T0, T1, T2> : MessagePackConverter<OneOf<T0, T1, T2>>
#else
internal class OneOfConverter<T0, T1, T2> : MessagePackConverter<OneOf<T0, T1, T2>>
#endif
{
    public override void Write(ref MessagePackWriter writer, in OneOf<T0, T1, T2> value, SerializationContext context)
    {
        context.DepthStep();
        writer.WriteArrayHeader(2);
        
        if (value.IsT0)
        {
            writer.Write(0);
            WriteValue(ref writer, value.AsT0, context);
        }
        else if (value.IsT1)
        {
            writer.Write(1);
            WriteValue(ref writer, value.AsT1, context);
        }
        else
        {
            writer.Write(2);
            WriteValue(ref writer, value.AsT2, context);
        }
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
            0 => OneOf<T0, T1, T2>.FromT0(ReadValue<T0>(ref reader, context)),
            1 => OneOf<T0, T1, T2>.FromT1(ReadValue<T1>(ref reader, context)),
            2 => OneOf<T0, T1, T2>.FromT2(ReadValue<T2>(ref reader, context)),
            _ => throw new MessagePackSerializationException($"Invalid OneOf type index: {typeIndex}. Expected 0, 1, or 2.")
        };
    }

    private static void WriteValue<T>(ref MessagePackWriter writer, T value, SerializationContext context)
    {
        // Handle common primitive types directly
        switch (value)
        {
            case string s:
                writer.Write(s);
                break;
            case int i:
                writer.Write(i);
                break;
            case bool b:
                writer.Write(b);
                break;
            case double d:
                writer.Write(d);
                break;
            case float f:
                writer.Write(f);
                break;
            case long l:
                writer.Write(l);
                break;
            case null:
                writer.WriteNil();
                break;
            default:
                // For complex types, you would need to get a converter
                // This is a simplified sample implementation
                throw new NotSupportedException($"Type {typeof(T)} is not supported in this sample converter. For complex types, implement a more sophisticated converter that can handle type shapes.");
        }
    }

    private static T ReadValue<T>(ref MessagePackReader reader, SerializationContext context)
    {
        // Handle common primitive types directly
        if (typeof(T) == typeof(string))
        {
            return (T)(object)reader.ReadString()!;
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)reader.ReadInt32();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)reader.ReadBoolean();
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)reader.ReadDouble();
        }
        else if (typeof(T) == typeof(float))
        {
            return (T)(object)reader.ReadSingle();
        }
        else if (typeof(T) == typeof(long))
        {
            return (T)(object)reader.ReadInt64();
        }
        else
        {
            // For complex types, you would need to get a converter
            // This is a simplified sample implementation
            throw new NotSupportedException($"Type {typeof(T)} is not supported in this sample converter. For complex types, implement a more sophisticated converter that can handle type shapes.");
        }
    }
}

/// <summary>
/// Converter factory that creates OneOf converters for OneOf types.
/// </summary>
internal class OneOfConverterFactory : IMessagePackConverterFactory
{
    public MessagePackConverter<T>? CreateConverter<T>(ITypeShape<T> shape)
    {
        Type type = typeof(T);
        
        // Check if it's a OneOf type
        if (type.IsGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            
            // Handle OneOf<T0, T1>
            if (genericTypeDefinition == typeof(OneOf<,>))
            {
                Type[] typeArgs = type.GetGenericArguments();
                Type converterType = typeof(OneOfConverter<,>).MakeGenericType(typeArgs);
                return (MessagePackConverter<T>?)Activator.CreateInstance(converterType);
            }
            
            // Handle OneOf<T0, T1, T2>
            if (genericTypeDefinition == typeof(OneOf<,,>))
            {
                Type[] typeArgs = type.GetGenericArguments();
                Type converterType = typeof(OneOfConverter<,,>).MakeGenericType(typeArgs);
                return (MessagePackConverter<T>?)Activator.CreateInstance(converterType);
            }
        }
        
        return null;
    }
}
#endregion