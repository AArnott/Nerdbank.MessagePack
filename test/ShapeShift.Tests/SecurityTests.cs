// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SecurityTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	/// <summary>
	/// Verifies that the serializer will guard against stack overflow attacks for map-formatted objects.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Serialize()
	{
		// Prepare a very deep structure, designed to blow the stack.
		Nested outer = this.ConstructDeepObjectGraph(this.Serializer.StartingContext.MaxDepth + 1);

		// Try serializing that structure. This should throw for security reasons.
		Sequence<byte> buffer = new();
		Assert.Throws<SerializationException>(() => this.Serializer.Serialize(buffer, outer, TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// Verifies that the serializer will allow depths within the prescribed limit.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Serialize_WithinLimit()
	{
		// Prepare a very deep structure, designed to blow the stack.
		Nested outer = this.ConstructDeepObjectGraph(this.Serializer.StartingContext.MaxDepth);

		// Try serializing that structure. This should throw for security reasons.
		Sequence<byte> buffer = new();
		this.Serializer.Serialize(buffer, outer, TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// Verifies that the deserializer will guard against stack overflow attacks for map-formatted objects.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Deserialize()
	{
		// Prepare a very deep structure, designed to blow the stack.
		ReadOnlySequence<byte> buffer = this.FormatDeepMsgPackMap(this.Serializer.StartingContext.MaxDepth + 1);

		// Try deserializing that structure. This should throw for security reasons.
		Assert.Throws<SerializationException>(() => this.Serializer.Deserialize<Nested>(buffer, TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// Verifies that the deserializer will allow depths within the prescribed limit.
	/// </summary>
	[Fact]
	public void StackGuard_ObjectMap_Deserialize_WithinLimit()
	{
		// Prepare a very deep structure, designed to blow the stack.
		ReadOnlySequence<byte> buffer = this.FormatDeepMsgPackMap(this.Serializer.StartingContext.MaxDepth);

		// Try deserializing that structure. This should throw for security reasons.
		this.Serializer.Deserialize<Nested>(buffer, TestContext.Current.CancellationToken);
	}

	[Fact]
	public void ManualHashCollisionResistance()
	{
		// This doesn't really test for hash collision resistance directly.
		// But it ensures that a type that controls its own collection's hash function can be deserialized.
		this.AssertRoundtrip(new HashCollisionResistance
		{
			Dictionary = { { "a", "b" }, { "c", "d" } },
			HashSet = { "c" },
		});
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
		Writer writer = new(buffer, MessagePackFormatter.Default);
		for (int i = 0; i < depth; i++)
		{
			writer.WriteStartMap(1);
			writer.Write(nameof(Nested.Another));
		}

		writer.WriteNull();
		writer.Flush();
		return buffer;
	}

	[GenerateShape]
	public partial class Nested
	{
		public Nested? Another { get; set; }
	}

	[GenerateShape]
	public partial class HashCollisionResistance : IEquatable<HashCollisionResistance>
	{
		public HashCollisionResistance()
		{
			// Theoretically these instances would be created with hash-collision resistant equality comparers.
			// In this particular case, it turns out that the string equality comparer *is* hash collision resistant.
			this.Dictionary = new(EqualityComparer<string>.Default);
			this.HashSet = new(EqualityComparer<string>.Default);
		}

		public Dictionary<string, string> Dictionary { get; }

		public HashSet<string> HashSet { get; }

		public bool Equals(HashCollisionResistance? other) =>
			StructuralEquality.Equal(this.Dictionary, other?.Dictionary) &&
			StructuralEquality.Equal(this.HashSet, other?.HashSet);
	}
}
