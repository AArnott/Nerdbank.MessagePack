// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Verifies that projects with their own version.json get its revision-level assembly version,
/// rather than the root version.json's x.y.0.0 (e.g. when a root GitVersionBaseDirectory overrides it).
/// </summary>
public class AssemblyVersionTests
{
	[Test]
	public void NerdbankMessagePackAnalyzers() => AssertRevisionIsNonZero(typeof(global::Nerdbank.MessagePack.Analyzers.MigrationAnalyzer).Assembly);

	[Test]
	public void NerdbankMessagePackAnalyzersCodeFixes() => AssertRevisionIsNonZero(typeof(global::Nerdbank.MessagePack.Analyzers.CodeFixes.MigrationCodeFix).Assembly);

	private static void AssertRevisionIsNonZero(System.Reflection.Assembly assembly)
	{
		System.Reflection.AssemblyName name = assembly.GetName();
		if (name.Version!.Revision == 0)
		{
			throw new InvalidOperationException($"{name.Name} has assembly version {name.Version}, but its own version.json should give it a non-zero revision.");
		}
	}
}
