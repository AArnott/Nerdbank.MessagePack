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
			public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
			{
				string? value = context.GetConverter<string, CustomTypeConverter>().Read(ref reader, context);
				return new CustomType { InternalProperty = value };
			}

			public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
			{
				context.GetConverter<string, CustomTypeConverter>().Write(ref writer, value?.InternalProperty, context);
			}
		}
	}
}
