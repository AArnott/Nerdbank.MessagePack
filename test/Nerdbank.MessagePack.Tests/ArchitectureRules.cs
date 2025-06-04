// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetArchTest.Rules;

public class ArchitectureRules
{
	/// <summary>
	/// Verifies that all optional converters are exposed via <see cref="OptionalConverters"/> rather than being public in the Converters namespace.
	/// </summary>
	[Fact]
	public void NoPublicConvertersNamespace()
	{
		Assert.Empty(Types.InAssembly(typeof(MessagePackSerializer).Assembly)
			.That().ResideInNamespaceMatching($"{typeof(MessagePackSerializer).Namespace}.Converters")
			.ShouldNot().BePublic()
			.GetResult().FailingTypes);
	}
}
