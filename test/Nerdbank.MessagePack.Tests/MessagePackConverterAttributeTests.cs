// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class MessagePackConverterAttributeTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void CustomTypeWithConverter()
	{
		this.AssertRoundtrip(new CustomType() { InternalProperty = "some value" });
	}

	[GenerateShape] // to allow use as a direct serialization argument
	[MessagePackConverter(typeof(CustomTypeConverter))]
	public partial record CustomType
	{
		// This property is internal so that if the auto-generated serializer were used instead of the custom one,
		// the value would be dropped and the test would fail.
		internal string? InternalProperty { get; set; }

		public override string ToString() => this.InternalProperty ?? "(null)";
	}

	public class CustomTypeConverter : MessagePackConverter<CustomType>
	{
		public override void Read(ref MessagePackReader reader, ref CustomType? value, SerializationContext context)
		{
			value = new() { InternalProperty = reader.ReadString() };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			writer.Write(value?.InternalProperty);
		}
	}
}
