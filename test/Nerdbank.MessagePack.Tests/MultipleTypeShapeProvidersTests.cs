// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Tests;

public partial class MultipleTypeShapeProvidersTests : MessagePackSerializerTestBase
{
	[Fact]
	public void ShapesFromTwoAssemblies_DirectAndTransitive()
	{
		// In this test, we carefully use the Serialize method directly
		// instead of one of our test helpers, since our test helpers may
		// use our local test assembly Witness class for both cases, which
		// defeats the test.

		// First, serialize the Extension type directly,
		// so that the dynamic resolver discovers the source-generated shape from
		// the other assembly.
		this.Serializer.Serialize(new Extension(3, new byte[0]), TestContext.Current.CancellationToken);

		// Now serialize a type from our own assembly, that references Extension,
		// which forces the serializer to deal with a second shape that represents the type
		// for which a converter was already generated above.
		this.Serializer.Serialize(new Outer(new Extension(5, new byte[0])), TestContext.Current.CancellationToken);
	}

	[Fact]
	public void ShapesFromTwoAssemblies_BothDirect()
	{
		this.Serializer.Serialize(new Extension(3, new byte[0]), TestContext.Current.CancellationToken);
		this.Serializer.Serialize(new Extension(3, new byte[0]), PolyType.SourceGenerator.TypeShapeProvider_Nerdbank_MessagePack_Tests.Default.Extension, TestContext.Current.CancellationToken);
	}

	[Fact]
	public void ShapesFromTwoAssemblies_BothDirectOneProvider()
	{
		this.Serializer.Serialize(new Extension(3, new byte[0]), TestContext.Current.CancellationToken);
		this.Serializer.Serialize(new Extension(3, new byte[0]), Witness.GeneratedTypeShapeProvider, TestContext.Current.CancellationToken);
	}

	[GenerateShape]
	internal partial record Outer(Extension Inner);

	[GenerateShapeFor<Extension>]
	private partial class Witness;
}
