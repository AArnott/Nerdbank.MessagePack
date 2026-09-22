// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetArchTest.Rules;

public class ArchitectureRules
{
	/// <summary>
	/// Verifies that all optional converters are exposed via <see cref="OptionalConverters"/> rather than being public in the Converters namespace.
	/// </summary>
	[Test]
	public void NoPublicConvertersNamespace()
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			// NetArchTest (via Mono.Cecil) reads assemblies from disk by their Assembly.Location,
			// which is always empty when assemblies are compiled into a NativeAOT native executable.
			Skip.Test("NetArchTest requires reading assemblies from disk by location, which is not available under NativeAOT.");
			return;
		}
#endif

		Assert.Empty(Types.InAssembly(typeof(MessagePackSerializer).Assembly)
			.That().ResideInNamespaceMatching($"{typeof(MessagePackSerializer).Namespace}.Converters")
			.ShouldNot().BePublic()
			.GetResult().FailingTypes);
	}
}
