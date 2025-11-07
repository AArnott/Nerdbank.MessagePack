// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.ReflectionProvider;
using PolyType.Utilities;

namespace Nerdbank.MessagePack.Tests;

/// <summary>
/// Verifies various scenarios in which type shapes come from multiple shape providers.
/// </summary>
/// <remarks>
/// In this test, we carefully use the Serialize method directly
/// instead of one of our test helpers, since our test helpers may
/// use our local test assembly Witness class for both cases, which
/// defeats the test.
/// </remarks>
public partial class MultipleTypeShapeProvidersTests : MessagePackSerializerTestBase
{
	private static readonly Extension SomeExtension = new(5, Array.Empty<byte>());

	[Fact]
	public void ShapesFromTwoAssemblies_DirectAndTransitive()
	{
		// First, serialize the Extension type directly,
		// so that the dynamic resolver discovers the source-generated shape from
		// the other assembly.
		this.Serializer.Serialize(SomeExtension, TestContext.Current.CancellationToken);

		// Now serialize a type from our own assembly, that references Extension,
		// which forces the serializer to deal with a second shape that represents the type
		// for which a converter was already generated above.
		this.Serializer.Serialize(new Outer(SomeExtension), TestContext.Current.CancellationToken);
	}

	[Fact]
	public void ShapesFromTwoAssemblies_BothDirect()
	{
		this.Serializer.Serialize(SomeExtension, TestContext.Current.CancellationToken);
		this.Serializer.Serialize(SomeExtension, SourceGenProvider.Extension, TestContext.Current.CancellationToken);
	}

	[Fact]
	public void AggregatingShapeProvider()
	{
		AggregatingTypeShapeProvider provider = new(Witness.GeneratedTypeShapeProvider, ReflectionTypeShapeProvider.Default);

		// First, serialize Extension via what should resolve as a shape from the main library's source generated provider.
		this.Serializer.Serialize(SomeExtension, provider.GetTypeShapeOrThrow<Extension>(), TestContext.Current.CancellationToken);

		// Now serialize a type that will have to come from the reflection provider, and references an Extension.
		this.Serializer.Serialize(new TypeWithNoSourceGeneratedShape(SomeExtension), provider.GetTypeShapeOrThrow<TypeWithNoSourceGeneratedShape>(), TestContext.Current.CancellationToken);
	}

	[GenerateShape]
	internal partial record Outer(Extension Inner);

	internal record TypeWithNoSourceGeneratedShape(Extension Inner);

	[GenerateShapeFor<Extension>]
	private partial class Witness;
}
