// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack051 // Suppress warnings about missing shape for array types in this test file, since we are testing the runtime behavior for such violations.

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Tests for improved error messages when serializing or deserializing array types without proper witness types.
/// </summary>
public partial class ArrayTypeShapeErrorTests : MessagePackSerializerTestBase
{
	/// <summary>
	/// Verifies that attempting to deserialize an array type without a witness type
	/// throws a NotSupportedException with helpful guidance.
	/// </summary>
	[Test]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The library is guaranteed to throw NotSupportedException before reaching any dynamic code path when no witness type is provided.")]
#endif
	public void DeserializeArrayWithoutWitness_ThrowsHelpfulException()
	{
		// Create some test data
		var testData = new TestItem[] { new() { Name = "Test", Value = 42 } };

		// Serialize with witness type (this should work)
		byte[] msgpack = this.Serializer.Serialize<TestItem[], Witness>(testData, this.TimeoutToken);

		// Try to deserialize WITHOUT witness type using extension method (should fail with helpful message)
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.Deserialize<TestItem[]>(msgpack, this.TimeoutToken));

		// Verify the error message contains helpful information
		Console.WriteLine(ex.Message);
		Assert.Contains("does not have a generated shape", ex.Message);
		Assert.Contains("array", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("GenerateShapeFor", ex.Message);
		Assert.Contains("witness", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html", ex.Message);
	}

	/// <summary>
	/// Verifies that attempting to serialize an array type without a witness type
	/// throws a NotSupportedException with helpful guidance.
	/// </summary>
	[Test]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The library is guaranteed to throw NotSupportedException before reaching any dynamic code path when no witness type is provided.")]
#endif
	public void SerializeArrayWithoutWitness_ThrowsHelpfulException()
	{
		var testData = new TestItem[] { new() { Name = "Test", Value = 42 } };

		// Try to serialize WITHOUT witness type using extension method (should fail with helpful message)
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.Serialize<TestItem[]>(testData, this.TimeoutToken));

		// Verify the error message contains helpful information
		Console.WriteLine(ex.Message);
		Assert.Contains("does not have a generated shape", ex.Message);
		Assert.Contains("array", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("GenerateShapeFor", ex.Message);
		Assert.Contains("witness", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html", ex.Message);
	}

	/// <summary>
	/// Verifies that serializing and deserializing arrays WITH a witness type works correctly.
	/// </summary>
	[Test]
	public void ArrayWithWitness_Roundtrips()
	{
		var testData = new TestItem[] { new() { Name = "Test1", Value = 42 }, new() { Name = "Test2", Value = 84 } };

		// Serialize with witness type
		byte[] msgpack = this.Serializer.Serialize<TestItem[], Witness>(testData, this.TimeoutToken);

		// Deserialize with witness type
		TestItem[]? result = this.Serializer.Deserialize<TestItem[], Witness>(msgpack, this.TimeoutToken);

		// Verify the results
		Assert.NotNull(result);
		Assert.Equal(2, result.Length);
		Assert.Equal("Test1", result[0].Name);
		Assert.Equal(42, result[0].Value);
		Assert.Equal("Test2", result[1].Name);
		Assert.Equal(84, result[1].Value);
	}

	/// <summary>
	/// Verifies that attempting to use a witness type that doesn't have the correct [GenerateShapeFor] attribute
	/// throws a NotSupportedException with helpful guidance.
	/// </summary>
	[Test]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The library is guaranteed to throw NotSupportedException before reaching any dynamic code path when the witness type is incomplete.")]
#endif
	public void ArrayWithIncorrectWitness_ThrowsHelpfulException()
	{
		var testData = new TestItem[] { new() { Name = "Test", Value = 42 } };

		// Try to serialize with a witness type that doesn't have [GenerateShapeFor<TestItem[]>]
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.Serialize<TestItem[], IncompleteWitness>(testData, this.TimeoutToken));

		// Verify the error message contains helpful information
		Console.WriteLine(ex.Message);
		Assert.Contains("does not have a generated shape", ex.Message);
		Assert.Contains("witness", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("GenerateShapeFor", ex.Message);
		Assert.Contains("IncompleteWitness", ex.Message);
		Assert.Contains("https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html", ex.Message);
	}

	[GenerateShape]
	public partial class TestItem
	{
		public string? Name { get; set; }

		public int Value { get; set; }
	}

	[GenerateShapeFor<TestItem[]>]
	private partial class Witness;

	// Witness type that's missing the [GenerateShapeFor<TestItem[]>] attribute
	private partial class IncompleteWitness;
}
