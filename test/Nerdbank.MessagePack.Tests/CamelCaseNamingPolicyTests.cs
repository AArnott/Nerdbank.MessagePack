// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class CamelCaseNamingPolicyTests
{
	[Fact]
	public void ConvertName()
	{
		Assert.NotNull(NamingPolicy.CamelCase);
		Assert.Equal("foo", NamingPolicy.CamelCase.ConvertName("Foo"));
		Assert.Equal("foo", NamingPolicy.CamelCase.ConvertName("foo"));
		Assert.Equal("fooBar", NamingPolicy.CamelCase.ConvertName("FooBar"));
		Assert.Equal("fOOBAR", NamingPolicy.CamelCase.ConvertName("FOOBAR"));
		Assert.Equal(string.Empty, NamingPolicy.CamelCase.ConvertName(string.Empty));
	}
}
