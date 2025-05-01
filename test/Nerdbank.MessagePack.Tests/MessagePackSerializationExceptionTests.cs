// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class MessagePackSerializationExceptionTests
{
	[Fact]
	public void Code_Default()
	{
		Assert.Equal(MessagePackSerializationException.ErrorCode.Unspecified, new MessagePackSerializationException().Code);
	}

	[Fact]
	public void Code_Inherited()
	{
		Assert.Equal(
			MessagePackSerializationException.ErrorCode.MissingRequiredProperty,
			new MessagePackSerializationException(null, new MessagePackSerializationException() { Code = MessagePackSerializationException.ErrorCode.MissingRequiredProperty }).Code);
	}
}
