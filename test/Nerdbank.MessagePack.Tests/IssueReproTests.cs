// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType;
using Xunit;

namespace Nerdbank.MessagePack.Tests;

public enum TestEnum
{
	First = 0,
	Second = 1,
}

public partial class IssueReproTests
{
	[Fact]
	public void TestEnumFieldWithInitializer_WithDefaultValue()
	{
		// This test verifies the original issue is resolved when DefaultValue attribute is added
		TestClassWithDefaultValue original = new();
		original.MyEnum = TestEnum.First;
		MessagePackSerializer serializer = new();
		byte[] bytes = serializer.Serialize(original);
		TestClassWithDefaultValue deserialized = serializer.Deserialize<TestClassWithDefaultValue>(bytes);
		Assert.Equal(TestEnum.First, deserialized.MyEnum);
	}

	[GenerateShape]
	public partial class TestClassWithDefaultValue
	{
		[System.ComponentModel.DefaultValue(TestEnum.Second)]
		public TestEnum MyEnum = TestEnum.Second;
	}
}
