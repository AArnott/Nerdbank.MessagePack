// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class CustomConverterTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void ConverterThatDelegates()
	{
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[Fact]
	public void AttributedTypeWorksAfterFirstSerialization()
	{
		this.AssertRoundtrip(new Tree(3));
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[Fact]
	public void RegisterThrowsAfterFirstSerialization()
	{
		this.AssertRoundtrip(new Tree(3));
		this.Serializer.RegisterConverter(new NoOpConverter());
	}

	[GenerateShape]
	public partial record Tree(int FruitCount);

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

	private class NoOpConverter : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context) => throw new NotImplementedException();

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context) => throw new NotImplementedException();
	}
}
