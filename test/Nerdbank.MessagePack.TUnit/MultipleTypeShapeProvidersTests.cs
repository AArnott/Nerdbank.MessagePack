// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using PolyType.ReflectionProvider;
using PolyType.Utilities;

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

	[Test]
	public void ShapesFromTwoAssemblies_DirectAndTransitive()
	{
		// First, serialize the Extension type directly,
		// so that the dynamic resolver discovers the source-generated shape from
		// the other assembly.
		this.Serializer.Serialize(SomeExtension, this.TimeoutToken);

		// Now serialize a type from our own assembly, that references Extension,
		// which forces the serializer to deal with a second shape that represents the type
		// for which a converter was already generated above.
		this.Serializer.Serialize(new Outer(SomeExtension), this.TimeoutToken);
	}

	[Test]
	public void ShapesFromTwoAssemblies_BothDirect()
	{
		this.Serializer.Serialize(SomeExtension, this.TimeoutToken);
		this.Serializer.Serialize(SomeExtension, this.TimeoutToken);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void AggregatingShapeProvider()
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("PolyType's reflection-based shape provider requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		AggregatingTypeShapeProvider provider = new(Witness.GeneratedTypeShapeProvider, ReflectionTypeShapeProvider.Default);

		// First, serialize Extension via what should resolve as a shape from the main library's source generated provider.
		this.Serializer.Serialize(SomeExtension, provider.GetTypeShapeOrThrow<Extension>(), this.TimeoutToken);

		// Now serialize a type that will have to come from the reflection provider, and references an Extension.
		this.Serializer.Serialize(new TypeWithNoSourceGeneratedShape(SomeExtension), provider.GetTypeShapeOrThrow<TypeWithNoSourceGeneratedShape>(), this.TimeoutToken);
	}

	[GenerateShape]
	internal partial record Outer(Extension Inner);

	internal record TypeWithNoSourceGeneratedShape(Extension Inner);

	[GenerateShapeFor<Extension>]
	private partial class Witness;
}
