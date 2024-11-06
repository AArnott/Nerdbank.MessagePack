// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SerializationContextTests
{
	/// <summary>
	/// Verifies that the <see cref="SerializationContext.GetConverter{T}"/> method throws when not within a serialization operation.
	/// </summary>
	[Fact]
	public void GetConverterThrows()
	{
		SerializationContext context = new();
		Assert.Throws<InvalidOperationException>(() => context.GetConverter<MyType>());
	}

	[GenerateShape]
	public partial class MyType;
}
