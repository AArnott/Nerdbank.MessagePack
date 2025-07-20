// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf;
using Samples.Converters;

namespace Samples;

/// <summary>
/// Demonstrates serialization of OneOf discriminated union types.
/// </summary>
internal class OneOfSample
{
    /// <summary>
    /// Shows how to use the OneOf converters with MessagePack serialization.
    /// </summary>
    internal static void DemonstrateOneOfSerialization()
    {
        // Register the OneOf converters
        var serializer = MessagePackSerializer.CreateDefault() with
        {
            ConverterTypes = [typeof(OneOfConverter<,>), typeof(OneOfConverter<,,>)]
        };

        // Example with OneOf<string, int>
        OneOf<string, int> stringValue = "Hello, World!";
        OneOf<string, int> intValue = 42;

        // Serialize the values
        byte[] serializedString = serializer.Serialize(stringValue);
        byte[] serializedInt = serializer.Serialize(intValue);

        // Deserialize the values
        OneOf<string, int> deserializedString = serializer.Deserialize<OneOf<string, int>>(serializedString);
        OneOf<string, int> deserializedInt = serializer.Deserialize<OneOf<string, int>>(serializedInt);

        // Verify the results
        Console.WriteLine("Original string value:");
        stringValue.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        Console.WriteLine("Deserialized string value:");
        deserializedString.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        Console.WriteLine("Original int value:");
        intValue.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        Console.WriteLine("Deserialized int value:");
        deserializedInt.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        // Example with OneOf<string, int, bool>
        OneOf<string, int, bool> boolValue = true;
        byte[] serializedBool = serializer.Serialize(boolValue);
        OneOf<string, int, bool> deserializedBool = serializer.Deserialize<OneOf<string, int, bool>>(serializedBool);

        Console.WriteLine("Original bool value:");
        boolValue.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"),
            b => Console.WriteLine($"  Boolean: {b}"));

        Console.WriteLine("Deserialized bool value:");
        deserializedBool.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"),
            b => Console.WriteLine($"  Boolean: {b}"));
    }
}