// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public class OptionalConvertersTests : MessagePackSerializerTestBase
{
	[Fact]
	public void NullCheck()
	{
		Assert.Throws<ArgumentNullException>("serializer", () => OptionalConverters.WithSystemTextJsonConverters(null!));
		Assert.Throws<ArgumentNullException>("serializer", () => OptionalConverters.WithGuidConverter(null!, OptionalConverters.GuidFormat.BinaryLittleEndian));
	}

	[Fact]
	public void DoubleAddThrows()
	{
		this.Serializer = this.Serializer.WithGuidConverter(OptionalConverters.GuidFormat.BinaryLittleEndian);
		Assert.Throws<ArgumentException>(() => this.Serializer.WithGuidConverter(OptionalConverters.GuidFormat.StringN));
	}
}
