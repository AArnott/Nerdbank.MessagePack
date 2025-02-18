// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetArchTest.Rules;

public class ArchitectureRules(ITestOutputHelper logger)
{
	[Fact]
	public void FormatAgnosticism()
	{
		IReadOnlyList<string>? failingTypeNames =
			Types.InAssembly(typeof(MessagePackSerializer).Assembly)
				.That()
				.ResideInNamespace("Nerdbank.PolySerializer")
				.And()
				.DoNotResideInNamespace("Nerdbank.PolySerializer.MessagePack")
				.And()
				.DoNotHaveCustomAttribute(typeof(MessagePackSerializer).Assembly.GetType("Nerdbank.PolySerializer.GeneralWithFormatterSpecialCasingAttribute"))
				.ShouldNot()
				.HaveDependencyOnAny("Nerdbank.PolySerializer.MessagePack")
				.GetResult()
				.FailingTypeNames;

		if (failingTypeNames is not null)
		{
			foreach (string name in failingTypeNames)
			{
				logger.WriteLine(name);
			}

			Assert.Empty(failingTypeNames);
		}
	}
}
