// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType;

internal static class ReferencesHelper
{
	internal static ReferenceAssemblies DefaultTargetFrameworkReferences = ReferenceAssemblies.Net.Net80;

	internal static IEnumerable<MetadataReference> GetReferences()
	{
		yield return MetadataReference.CreateFromFile(typeof(MessagePackSerializer).Assembly.Location);
		yield return MetadataReference.CreateFromFile(typeof(GenerateShapeAttribute).Assembly.Location);
	}
}
