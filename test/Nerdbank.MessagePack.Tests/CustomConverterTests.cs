// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class CustomConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void ConverterThatDelegates()
	{
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[GenerateShape, MessagePackConverter(typeof(CustomTypeConverter))]
	public partial record CustomType
	{
		internal string? InternalProperty { get; set; }

		public override string ToString() => this.InternalProperty ?? "(null)";

		[GenerateShape<string>]
		private partial class CustomTypeConverter : MessagePackConverter<CustomType>
		{
			public override CustomType? Deserialize(ref MessagePackReader reader, SerializationContext context)
			{
				string? value = context.GetConverter<string, CustomTypeConverter>().Deserialize(ref reader, context);
				return new CustomType { InternalProperty = value };
			}

			public override void Serialize(ref MessagePackWriter writer, ref CustomType? value, SerializationContext context)
			{
				string? internalProperty = value?.InternalProperty;
				context.GetConverter<string, CustomTypeConverter>().Serialize(ref writer, ref internalProperty, context);
			}
		}
	}
}
