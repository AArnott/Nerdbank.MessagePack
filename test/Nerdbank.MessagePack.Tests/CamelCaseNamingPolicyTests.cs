// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class CamelCaseNamingPolicyTests
{
	[Fact]
	public void ConvertName()
	{
		Assert.NotNull(MessagePackNamingPolicy.CamelCase);
		Assert.Equal("foo", MessagePackNamingPolicy.CamelCase.ConvertName("Foo"));
		Assert.Equal("foo", MessagePackNamingPolicy.CamelCase.ConvertName("foo"));
		Assert.Equal("fooBar", MessagePackNamingPolicy.CamelCase.ConvertName("FooBar"));
		Assert.Equal("fOOBAR", MessagePackNamingPolicy.CamelCase.ConvertName("FOOBAR"));
		Assert.Equal(string.Empty, MessagePackNamingPolicy.CamelCase.ConvertName(string.Empty));
	}
}
