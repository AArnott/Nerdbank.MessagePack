// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType;
using ShapeShift;

internal static class ReferencesHelper
{
#if NET
	internal static ReferenceAssemblies References = ReferenceAssemblies.Net.Net80;
#else
	internal static ReferenceAssemblies References = ReferenceAssemblies.NetStandard.NetStandard20
		.WithPackages([
			new PackageIdentity("System.Memory", "4.6.0"),
			new PackageIdentity("System.Text.Json", "9.0.0"),
		]);
#endif

	internal static IEnumerable<MetadataReference> GetReferences()
	{
		yield return MetadataReference.CreateFromFile(typeof(MessagePackSerializer).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(GenerateShapeAttribute).Assembly.Location);
	}
}
