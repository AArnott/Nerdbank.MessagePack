// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Nerdbank.MessagePack;
using OneOf;
using PolyType.ReflectionProvider;
using Samples.Converters;

namespace Samples;

/// <summary>
/// Demonstrates serialization of OneOf discriminated union types.
/// </summary>
internal partial class OneOfSample
{
    /// <summary>
    /// Represents an API response that can be either successful data or an error.
    /// </summary>
    [GenerateShape]
    public partial record ApiResponse(OneOf<string, ErrorInfo> Result);

    /// <summary>
    /// Represents error information.
    /// </summary>
    [GenerateShape]
    public partial record ErrorInfo(int Code, string Message);

    /// <summary>
    /// Shows how to use the OneOf converters with MessagePack serialization.
    /// </summary>
    internal static void DemonstrateOneOfSerialization()
    {
        // Register the OneOf converters by adding a converter factory
        MessagePackSerializer serializer = new()
        {
            ConverterFactories = [new OneOfConverterFactory()],
        };

        // Example 1: Basic OneOf usage with primitive types
        OneOf<string, int> stringValue = "Hello, World!";
        OneOf<string, int> intValue = 42;

#if NET
        // Serialize and deserialize using the reflection-based approach for external types
        byte[] serializedString = serializer.Serialize(stringValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
        byte[] serializedInt = serializer.Serialize(intValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());

        OneOf<string, int> deserializedString = serializer.Deserialize<OneOf<string, int>>(serializedString, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
        OneOf<string, int> deserializedInt = serializer.Deserialize<OneOf<string, int>>(serializedInt, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
#else
        // Serialize and deserialize using the reflection-based approach for external types (.NET Framework)
        byte[] serializedString = serializer.Serialize(stringValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
        byte[] serializedInt = serializer.Serialize(intValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());

        OneOf<string, int> deserializedString = serializer.Deserialize<OneOf<string, int>>(serializedString, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
        OneOf<string, int> deserializedInt = serializer.Deserialize<OneOf<string, int>>(serializedInt, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int>>());
#endif

        Console.WriteLine("Basic OneOf serialization:");
        deserializedString.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        deserializedInt.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"));

        // Example 2: Practical API response scenario (these types have GenerateShape so work normally)
        ApiResponse successResponse = new(Result: "Operation completed successfully");
        ApiResponse errorResponse = new(Result: new ErrorInfo(404, "Not found"));

        // Serialize API responses  
#if NET
        byte[] successBytes = serializer.Serialize(successResponse);
        byte[] errorBytes = serializer.Serialize(errorResponse);

        // Deserialize API responses
        ApiResponse deserializedSuccess = serializer.Deserialize<ApiResponse>(successBytes);
        ApiResponse deserializedError = serializer.Deserialize<ApiResponse>(errorBytes);
#else
        byte[] successBytes = serializer.Serialize(successResponse, ReflectionTypeShapeProvider.Default.GetShape<ApiResponse>());
        byte[] errorBytes = serializer.Serialize(errorResponse, ReflectionTypeShapeProvider.Default.GetShape<ApiResponse>());

        // Deserialize API responses
        ApiResponse deserializedSuccess = serializer.Deserialize<ApiResponse>(successBytes, ReflectionTypeShapeProvider.Default.GetShape<ApiResponse>());
        ApiResponse deserializedError = serializer.Deserialize<ApiResponse>(errorBytes, ReflectionTypeShapeProvider.Default.GetShape<ApiResponse>());
#endif

        Console.WriteLine("API Response scenarios:");
        Console.Write("Success response: ");
        deserializedSuccess.Result.Switch(
            success => Console.WriteLine($"Success - {success}"),
            error => Console.WriteLine($"Error {error.Code}: {error.Message}"));

        Console.Write("Error response: ");
        deserializedError.Result.Switch(
            success => Console.WriteLine($"Success - {success}"),
            error => Console.WriteLine($"Error {error.Code}: {error.Message}"));

        // Example 3: OneOf with three types
        OneOf<string, int, bool> boolValue = true;
#if NET
        byte[] serializedBool = serializer.Serialize(boolValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int, bool>>());
        OneOf<string, int, bool> deserializedBool = serializer.Deserialize<OneOf<string, int, bool>>(serializedBool, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int, bool>>());
#else
        byte[] serializedBool = serializer.Serialize(boolValue, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int, bool>>());
        OneOf<string, int, bool> deserializedBool = serializer.Deserialize<OneOf<string, int, bool>>(serializedBool, ReflectionTypeShapeProvider.Default.GetShape<OneOf<string, int, bool>>());
#endif

        Console.WriteLine("Three-type OneOf:");
        deserializedBool.Switch(
            str => Console.WriteLine($"  String: {str}"),
            num => Console.WriteLine($"  Number: {num}"),
            b => Console.WriteLine($"  Boolean: {b}"));
    }
}