// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class MessagePackNamingPolicyTests
{
	[Theory]
	[InlineData("PropertyName", "propertyName")]
	[InlineData("propertyNAME", "propertyNAME")]
	[InlineData("URLValue", "urlValue")]
	[InlineData("URL", "url")]
	[InlineData("ID", "id")]
	[InlineData("I", "i")]
	[InlineData("Person", "person")]
	[InlineData("iPhone", "iPhone")]
	[InlineData("IPhone", "iPhone")]
	[InlineData("MyURL", "myURL")]
	[InlineData("MyURLValue", "myURLValue")]
	[InlineData("alreadyCamel", "alreadyCamel")]
	[InlineData("THIS_IS_A_TEST", "this_is_a_test")]
	[InlineData("ThisIsATest", "thisIsATest")]
	[InlineData(" ", " ")]
	[InlineData("", "")]
	public void CamelCaseNamingPolicy(string input, string expected)
	{
		string actual = MessagePackNamingPolicy.CamelCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}

	[Theory]
	[InlineData("propertyName", "PropertyName")]
	[InlineData("propertyNAME", "PropertyNAME")]
	[InlineData("urlValue", "UrlValue")]
	[InlineData("url", "Url")]
	[InlineData("id", "Id")]
	[InlineData("i", "I")]
	[InlineData("person", "Person")]
	[InlineData("iPhone", "IPhone")]
	[InlineData("i Phone", "I Phone")]
	[InlineData("myURL", "MyURL")]
	[InlineData("myURLValue", "MyURLValue")]
	[InlineData("AlreadyPascal", "AlreadyPascal")]
	[InlineData("THIS_IS_A_TEST", "THIS_IS_A_TEST")]
	[InlineData("thisIsATest", "ThisIsATest")]
	[InlineData(" ", " ")]
	[InlineData("", "")]
	public void PascalCaseNamingPolicy(string input, string expected)
	{
		string actual = MessagePackNamingPolicy.PascalCase.ConvertName(input);
		Assert.Equal(expected, actual);
	}
}
