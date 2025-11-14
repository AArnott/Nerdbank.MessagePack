// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ExceptionSerializationTests : MessagePackSerializerTestBase
{
	[Fact]
	public void Serialize_ExceptionWithData_NoPolymorphism()
	{
		InvalidOperationException ex = new("Test exception with data");
		ex.Data["Key1"] = "Value1";
		ex.Data["Key2"] = 42;
		Exception? deserializedEx = this.Roundtrip<Exception, Witness>(ex);

		Assert.IsType<Exception>(deserializedEx);
		Assert.Equal("Test exception with data", deserializedEx.Message);
		Assert.Equal("Value1", deserializedEx.Data["Key1"]);
		Assert.Equal(42, deserializedEx.Data["Key2"]);
	}

	[GenerateShapeFor<Exception>]
	private partial class Witness;
}
