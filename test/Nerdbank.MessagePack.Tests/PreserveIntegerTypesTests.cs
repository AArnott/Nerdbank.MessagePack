// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[GenerateShapeFor<object[]>]
[GenerateShapeFor<object>]
public partial class PreserveIntegerTypesTests : MessagePackSerializerTestBase
{
	[Theory]
	[InlineData((byte)40)]
	[InlineData((sbyte)40)]
	[InlineData((sbyte)-40)]
	[InlineData((short)40)]
	[InlineData((short)-40)]
	[InlineData((ushort)40)]
	[InlineData((int)40)]
	[InlineData((int)-40)]
	[InlineData((uint)40u)]
	[InlineData((long)40L)]
	[InlineData((long)-40L)]
	[InlineData((ulong)40UL)]
	public void IntegerTypesPreserved(object value)
	{
		this.Serializer = this.Serializer.WithObjectConverter(new ObjectConverterOptions { PreserveIntegerTypes = true });

		object[] input = [value];
		object[] output = this.Roundtrip<object[], PreserveIntegerTypesTests>(input)!;

		Assert.Equal(value, output[0]);
		Assert.Equal(value.GetType(), output[0]!.GetType());
	}

	[Theory]
	[InlineData((int)40)]
	[InlineData((int)-40)]
	[InlineData((uint)40u)]
	[InlineData((long)40L)]
	[InlineData((long)-40L)]
	[InlineData((ulong)40UL)]
	public void WithoutPreserveIntegerTypes_NonNegativeBecomesUlong(object value)
	{
		this.Serializer = this.Serializer.WithObjectConverter();

		object[] input = [value];
		object[] output = this.Roundtrip<object[], PreserveIntegerTypesTests>(input)!;

		bool isNegative = value switch
		{
			int v => v < 0,
			long v => v < 0,
			_ => false,
		};

		// Without PreserveIntegerTypes, non-negative values are deserialized as ulong, negative as long
		Assert.IsType(isNegative ? typeof(long) : typeof(ulong), output[0]);
	}
}
