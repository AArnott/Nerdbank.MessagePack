// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

public partial class SecurityTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	/// <summary>
	/// Verifies that the serializer will guard against stack overflow attacks for map-formatted objects.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Serialize()
	{
		// Prepare a very deep structure, designed to blow the stack.
		Nested outer = this.ConstructDeepObjectGraph(this.Serializer.MaxDepth + 1);

		// Try serializing that structure. This should throw for security reasons.
		Sequence<byte> buffer = new();
		Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(buffer, outer));
	}

	/// <summary>
	/// Verifies that the serializer will allow depths within the prescribed limit.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Serialize_WithinLimit()
	{
		// Prepare a very deep structure, designed to blow the stack.
		Nested outer = this.ConstructDeepObjectGraph(this.Serializer.MaxDepth);

		// Try serializing that structure. This should throw for security reasons.
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, outer);
	}

	/// <summary>
	/// Verifies that the deserializer will guard against stack overflow attacks for map-formatted objects.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Deserialize()
	{
		// Prepare a very deep structure, designed to blow the stack.
		ReadOnlySequence<byte> buffer = this.FormatDeepMsgPackMap(this.Serializer.MaxDepth + 1);

		// Try deserializing that structure. This should throw for security reasons.
		Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<Nested>(buffer));
	}

	/// <summary>
	/// Verifies that the deserializer will allow depths within the prescribed limit.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Deserialize_WithinLimit()
	{
		// Prepare a very deep structure, designed to blow the stack.
		ReadOnlySequence<byte> buffer = this.FormatDeepMsgPackMap(this.Serializer.MaxDepth);

		// Try deserializing that structure. This should throw for security reasons.
		this.Serializer.Deserialize<Nested>(buffer);
	}

	/// <summary>
	/// Verifies that the dictionaries created by the deserializer use collision resistant key hashes.
	/// </summary>
	[Fact(Skip = "Not yet implemented.")]
	public void CollisionResistantHashMaps()
	{
	}

	private Nested ConstructDeepObjectGraph(int depth)
	{
		Nested outer = new();
		for (int i = 0; i < depth - 1; i++)
		{
			outer = new() { Another = outer };
		}

		return outer;
	}

	private ReadOnlySequence<byte> FormatDeepMsgPackMap(int depth)
	{
		Sequence<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		for (int i = 0; i < depth; i++)
		{
			writer.WriteMapHeader(1);
			writer.Write(nameof(Nested.Another));
		}

		writer.WriteNil();
		writer.Flush();
		return buffer;
	}

	[GenerateShape]
	public partial class Nested
	{
		public Nested? Another { get; set; }
	}
}
