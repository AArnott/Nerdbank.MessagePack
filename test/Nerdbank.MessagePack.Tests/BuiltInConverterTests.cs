// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class BuiltInConverterTests : MessagePackSerializerTestBase
{
	[Fact]
	public void Guid()
	{
		// Test that Guid serialization works by default (using binary format)
		Guid value = System.Guid.NewGuid();
		Console.WriteLine($"Randomly generated guid: {value}");
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new HasGuid(value));
		Assert.True(this.DataMatchesSchema(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<HasGuid>()));
	}

	[Theory, PairwiseData]
	public void Guid_StringFormats(OptionalConverters.GuidStringFormat format)
	{
		this.Serializer = this.Serializer.WithGuidConverter(format);
		Guid value = System.Guid.NewGuid();
		Console.WriteLine($"Randomly generated guid: {value}");
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new HasGuid(value));
		Assert.True(this.DataMatchesSchema(msgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<HasGuid>()));
	}

	[GenerateShape]
	public partial record HasGuid(Guid Value);

	[GenerateShapeFor<HasGuid>]
	private partial class Witness;
}
