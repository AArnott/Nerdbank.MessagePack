﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ShapeShift.Analyzers;

public static class Constants
{
	public static class PropertyShapeAttribute
	{
		public const string TypeName = "PropertyShapeAttribute";
		public const string IgnoreProperty = "Ignore";
		public static readonly ImmutableArray<string> Namespace = ["PolyType"];
	}

	public static class KeyAttribute
	{
		public const string TypeName = "KeyAttribute";
		public const string IndexProperty = "Index";
		public static readonly ImmutableArray<string> Namespace = ["ShapeShift"];
	}

	public static class PreferDotNetAlternativeApiAttribute
	{
		public const string TypeName = "PreferDotNetAlternativeApiAttribute";
		public static readonly ImmutableArray<string> Namespace = ["ShapeShift"];
	}
}
