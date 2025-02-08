// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class PascalCaseNamingPolicyTests
{
	[Fact]
	public void ConvertName()
	{
		Assert.NotNull(NamingPolicy.PascalCase);
		Assert.Equal("Foo", NamingPolicy.PascalCase.ConvertName("Foo"));
		Assert.Equal("Foo", NamingPolicy.PascalCase.ConvertName("foo"));
		Assert.Equal("FooBar", NamingPolicy.PascalCase.ConvertName("fooBar"));
		Assert.Equal("FOOBAR", NamingPolicy.PascalCase.ConvertName("fOOBAR"));
		Assert.Equal(string.Empty, NamingPolicy.PascalCase.ConvertName(string.Empty));
	}
}
