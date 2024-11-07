// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class PascalCaseNamingPolicyTests
{
	[Fact]
	public void ConvertName()
	{
		Assert.NotNull(MessagePackNamingPolicy.PascalCase);
		Assert.Equal("Foo", MessagePackNamingPolicy.PascalCase.ConvertName("Foo"));
		Assert.Equal("Foo", MessagePackNamingPolicy.PascalCase.ConvertName("foo"));
		Assert.Equal("FooBar", MessagePackNamingPolicy.PascalCase.ConvertName("fooBar"));
		Assert.Equal("FOOBAR", MessagePackNamingPolicy.PascalCase.ConvertName("fOOBAR"));
		Assert.Equal(string.Empty, MessagePackNamingPolicy.PascalCase.ConvertName(string.Empty));
	}
}
