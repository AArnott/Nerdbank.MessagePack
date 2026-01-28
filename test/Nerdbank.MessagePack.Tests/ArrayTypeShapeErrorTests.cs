// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable NBMsgPack051 // Suppress warnings about missing shape for array types in this test file, since we are testing the runtime behavior for such violations.

/// <summary>
/// Tests for improved error messages when serializing or deserializing array types without proper witness types.
/// </summary>
public partial class ArrayTypeShapeErrorTests : MessagePackSerializerTestBase
{
	/// <summary>
	/// Verifies that attempting to deserialize an array type without a witness type
	/// throws a NotSupportedException with helpful guidance.
	/// </summary>
	[Fact]
	public void DeserializeArrayWithoutWitness_ThrowsHelpfulException()
	{
		// Create some test data
		var testData = new TestItem[] { new() { Name = "Test", Value = 42 } };

		// Serialize with witness type (this should work)
		byte[] msgpack = this.Serializer.Serialize<TestItem[], Witness>(testData, TestContext.Current.CancellationToken);

		// Try to deserialize WITHOUT witness type using extension method (should fail with helpful message)
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.Deserialize<TestItem[]>(msgpack, TestContext.Current.CancellationToken));

		// Verify the error message contains helpful information
		this.Logger.WriteLine(ex.Message);
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
	[Fact]
	public void SerializeArrayWithoutWitness_ThrowsHelpfulException()
	{
		var testData = new TestItem[] { new() { Name = "Test", Value = 42 } };

		// Try to serialize WITHOUT witness type using extension method (should fail with helpful message)
		NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
			this.Serializer.Serialize<TestItem[]>(testData, TestContext.Current.CancellationToken));

		// Verify the error message contains helpful information
		this.Logger.WriteLine(ex.Message);
		Assert.Contains("does not have a generated shape", ex.Message);
		Assert.Contains("array", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("GenerateShapeFor", ex.Message);
		Assert.Contains("witness", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("https://aarnott.github.io/Nerdbank.MessagePack/docs/type-shapes.html", ex.Message);
	}

	/// <summary>
	/// Verifies that serializing and deserializing arrays WITH a witness type works correctly.
	/// </summary>
	[Fact]
	public void ArrayWithWitness_Roundtrips()
	{
		var testData = new TestItem[] { new() { Name = "Test1", Value = 42 }, new() { Name = "Test2", Value = 84 } };

		// Serialize with witness type
		byte[] msgpack = this.Serializer.Serialize<TestItem[], Witness>(testData, TestContext.Current.CancellationToken);

		// Deserialize with witness type
		TestItem[]? result = this.Serializer.Deserialize<TestItem[], Witness>(msgpack, TestContext.Current.CancellationToken);

		// Verify the results
		Assert.NotNull(result);
		Assert.Equal(2, result.Length);
		Assert.Equal("Test1", result[0].Name);
		Assert.Equal(42, result[0].Value);
		Assert.Equal("Test2", result[1].Name);
		Assert.Equal(84, result[1].Value);
	}

	[GenerateShape]
	public partial class TestItem
	{
		public string? Name { get; set; }

		public int Value { get; set; }
	}

	[GenerateShapeFor<TestItem[]>]
	private partial class Witness;
}
