// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[GenerateShapeFor<object[]>]
[GenerateShapeFor<object>]
public partial class PreserveIntegerTypesTests : MessagePackSerializerTestBase
{
	[Test]
	[Arguments((byte)40)]
	[Arguments((sbyte)40)]
	[Arguments((sbyte)-40)]
	[Arguments((short)40)]
	[Arguments((short)-40)]
	[Arguments((ushort)40)]
	[Arguments((int)40)]
	[Arguments((int)-40)]
	[Arguments((uint)40u)]
	[Arguments((long)40L)]
	[Arguments((long)-40L)]
	[Arguments((ulong)40UL)]
	public void IntegerTypesPreserved(object value)
	{
		this.Serializer = this.Serializer.WithObjectConverter(new ObjectConverterOptions { PreserveIntegerTypes = true });

		object[] input = [value];
		object[] output = this.Roundtrip<object[], PreserveIntegerTypesTests>(input)!;

		Assert.Equal(value, output[0]);
		Assert.Equal(value.GetType(), output[0]!.GetType());
	}

	[Test]
	[Arguments((int)40)]
	[Arguments((int)-40)]
	[Arguments((uint)40u)]
	[Arguments((long)40L)]
	[Arguments((long)-40L)]
	[Arguments((ulong)40UL)]
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
