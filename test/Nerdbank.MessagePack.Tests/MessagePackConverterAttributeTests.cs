// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class MessagePackConverterAttributeTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void CustomTypeWithConverter()
	{
		this.AssertRoundtrip(new CustomType() { InternalProperty = "some value" });
	}

	[Fact]
	public void TypeWithCustomConvertedMembers()
	{
		var instance = new TypeWithCustomMembers()
		{
			EncryptedField = 12,
			OrdinaryField = 24,
			EncryptedProperty = 48,
			OrdinaryProperty = 96,
		};
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(instance);

		// Assert that the serialized data is encrypted.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(4, reader.ReadArrayHeader());
		Assert.NotEqual(12, reader.ReadInt32());
		Assert.Equal(24, reader.ReadInt32());
		Assert.NotEqual(48, reader.ReadInt32());
		Assert.Equal(96, reader.ReadInt32());
	}

	[Fact]
	public void GenericConverterOnMemberOfGenericType()
	{
		this.AssertRoundtrip(new Container() { Value = new(42) });
	}

	[AssociatedTypeShape(typeof(GenericConverter<>), Requirements = TypeShapeRequirements.Constructor)] // workaround https://github.com/eiriktsarpalis/PolyType/issues/181
	public record struct GenericData<T>(int Value);

	[GenerateShape]
	public partial record TypeWithCustomMembers
	{
		[Key(0), MessagePackConverter(typeof(EncryptingIntegerConverter))]
		public int EncryptedField;

		[Key(1)]
		public int OrdinaryField;

		[Key(2), MessagePackConverter(typeof(EncryptingIntegerConverter))]
		public int EncryptedProperty { get; set; }

		[Key(3)]
		public int OrdinaryProperty { get; set; }
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

	[GenerateShape]
	public partial record Container
	{
		[MessagePackConverter(typeof(GenericConverter<>))]
		public GenericData<int> Value { get; set; }
	}

	public class CustomTypeConverter : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			return new() { InternalProperty = reader.ReadString() };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			writer.Write(value?.InternalProperty);
		}
	}

	/// <summary>
	/// Simple XOR encryption for demonstration.
	/// </summary>
	public class EncryptingIntegerConverter : MessagePackConverter<int>
	{
		public override int Read(ref MessagePackReader reader, SerializationContext context) => reader.ReadInt32() ^ 0x12345678;

		public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => writer.Write(value ^ 0x12345678); // Simple XOR encryption for demonstration
	}

	public class GenericConverter<T> : MessagePackConverter<GenericData<T>>
	{
		public override GenericData<T> Read(ref MessagePackReader reader, SerializationContext context) => new(reader.ReadInt32());

		public override void Write(ref MessagePackWriter writer, in GenericData<T> value, SerializationContext context) => writer.Write(value.Value);
	}
}
