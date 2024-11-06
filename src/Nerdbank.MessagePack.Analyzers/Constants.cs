// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Nerdbank.MessagePack.Analyzers;

public static class Constants
{
	public static class PropertyShapeAttribute
	{
		public const string TypeName = "PropertyShapeAttribute";
		public const string IgnoreProperty = "Ignore";
		public static readonly ImmutableArray<string> Namespace = ["TypeShape"];
	}

	public static class KeyAttribute
	{
		public const string TypeName = "KeyAttribute";
		public const string IndexProperty = "Index";
		public static readonly ImmutableArray<string> Namespace = ["Nerdbank", "MessagePack"];
	}
}
