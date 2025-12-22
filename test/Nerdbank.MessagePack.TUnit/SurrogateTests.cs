// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Property("Surrogates", "true")]
public partial class SurrogateTests : MessagePackSerializerTestBase
{
	[Test]
	public void NonNullReference()
	{
		OriginalType? deserialized = this.Roundtrip(new OriginalType(1, 2));
		Assert.NotNull(deserialized);
		Assert.Equal(3, deserialized.Sum);
	}

	[Test]
	public void NullReference()
	{
		OriginalType? deserialized = this.Roundtrip<OriginalType>(null);
		Assert.Null(deserialized);
	}

	[Test]
	public async Task NonNullReference_Async()
	{
		OriginalType? deserialized = await this.RoundtripAsync(new OriginalType(1, 2));
		Assert.NotNull(deserialized);
		Assert.Equal(3, deserialized.Sum);
	}

	[Test]
	public async Task NullReference_Async()
	{
		OriginalType? deserialized = await this.RoundtripAsync<OriginalType>(null);
		Assert.Null(deserialized);
	}

	[Test]
	[Property("ReferencePreservation", "true")]
	public void ReferencePreservation()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		OriginalType original = new(1, 2);
		OriginalType[]? deserializedArray = this.Roundtrip<OriginalType[], Witness>([original, original]);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
	}

	[Test]
	public void GenericMarshaler()
	{
		OpenGenericDataType<int>? deserialized = this.Roundtrip<OpenGenericDataType<int>, Witness>(new OpenGenericDataType<int> { Value = 42 });
		Assert.NotNull(deserialized);
		Assert.Equal(42, deserialized.Value);
	}

	[Test]
	public void SurrogateIgnoredWithCustomConverterByInstanceRegistration()
	{
		this.Serializer = this.Serializer with { Converters = [new OriginalTypeConverter()] };

		OriginalType obj = new(3, 5);
		OriginalType? deserialized = this.Roundtrip(obj);

		// Verify that the custom converter was used by the way it changes the data.
		Assert.Equal(0, deserialized?.GetA());
		Assert.Equal(8, deserialized?.GetB());
	}

	[Test]
	public void SurrogateIgnoredWithCustomConverterByTypeRegistration()
	{
		this.Serializer = this.Serializer with { ConverterTypes = [typeof(OriginalTypeConverter)] };

		OriginalType obj = new(3, 5);
		OriginalType? deserialized = this.Roundtrip(obj);

		// Verify that the custom converter was used by the way it changes the data.
		Assert.Equal(0, deserialized?.GetA());
		Assert.Equal(8, deserialized?.GetB());
	}

	[Test]
	public void SurrogateIgnoredWithCustomConverterByFactoryRegistration()
	{
		this.Serializer = this.Serializer with { ConverterFactories = [new OriginalTypeConverterFactory()] };

		OriginalType obj = new(3, 5);
		OriginalType? deserialized = this.Roundtrip(obj);

		// Verify that the custom converter was used by the way it changes the data.
		Assert.Equal(0, deserialized?.GetA());
		Assert.Equal(8, deserialized?.GetB());
	}

	[Test]
	public void SurrogateIgnoredWithCustomConverterByAttribute()
	{
		OriginalTypeWithSurrogateAndConverter obj = new(3, 5);
		OriginalTypeWithSurrogateAndConverter? deserialized = this.Roundtrip(obj);

		// Verify that the custom converter was used by the way it changes the data.
		Assert.Equal(3, deserialized?.GetA());
		Assert.Equal(5, deserialized?.GetB());
	}

	[GenerateShape(Marshaler = typeof(Marshaler))]
	internal partial class OriginalType
	{
		private int a;
		private int b;

		internal OriginalType(int a, int b)
		{
			this.a = a;
			this.b = b;
		}

		public int Sum => this.a + this.b;

		internal int GetA() => this.a;

		internal int GetB() => this.b;

		internal record struct MarshaledType(int A, int B);

		internal class Marshaler : IMarshaler<OriginalType, MarshaledType?>
		{
			public OriginalType? Unmarshal(MarshaledType? surrogate)
				=> surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;

			public MarshaledType? Marshal(OriginalType? value)
				=> value is null ? null : new(value.a, value.b);
		}
	}

	[GenerateShape(Marshaler = typeof(Marshaler))]
	[MessagePackConverter(typeof(Converter))]
	internal partial class OriginalTypeWithSurrogateAndConverter
	{
		private int a;
		private int b;

		internal OriginalTypeWithSurrogateAndConverter(int a, int b)
		{
			this.a = a;
			this.b = b;
		}

		public int Sum => this.a + this.b;

		internal int GetA() => this.a;

		internal int GetB() => this.b;

		internal record struct MarshaledType(int A, int B);

		internal class Marshaler : IMarshaler<OriginalTypeWithSurrogateAndConverter, MarshaledType?>
		{
			public OriginalTypeWithSurrogateAndConverter? Unmarshal(MarshaledType? surrogate)
				=> throw new Exception("Marshaler should not be used.");

			public MarshaledType? Marshal(OriginalTypeWithSurrogateAndConverter? value)
				=> throw new Exception("Marshaler should not be used.");
		}

		internal class Converter : MessagePackConverter<OriginalTypeWithSurrogateAndConverter>
		{
			public override OriginalTypeWithSurrogateAndConverter? Read(ref MessagePackReader reader, SerializationContext context)
			{
				if (reader.TryReadNil())
				{
					return null;
				}

				reader.ReadArrayHeader();
				int a = reader.ReadInt32();
				int b = reader.ReadInt32();
				return new OriginalTypeWithSurrogateAndConverter(a, b);
			}

			public override void Write(ref MessagePackWriter writer, in OriginalTypeWithSurrogateAndConverter? value, SerializationContext context)
			{
				if (value is null)
				{
					writer.WriteNil();
					return;
				}

				writer.WriteArrayHeader(2);
				writer.Write(value.a);
				writer.Write(value.b);
			}
		}
	}

	[TypeShape(Marshaler = typeof(OpenGenericDataType<>.Marshaler))]
	internal class OpenGenericDataType<T>
	{
		public T? Value { get; set; }

		internal record struct MarshaledType(T? Value);

		internal class Marshaler : IMarshaler<OpenGenericDataType<T>, MarshaledType?>
		{
			public OpenGenericDataType<T>? Unmarshal(MarshaledType? surrogate)
				=> surrogate.HasValue ? new() { Value = surrogate.Value.Value } : null;

			public MarshaledType? Marshal(OpenGenericDataType<T>? value)
				=> value is null ? null : new(value.Value);
		}
	}

	internal class OriginalTypeConverter : MessagePackConverter<OriginalType>
	{
		public override OriginalType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int sum = reader.ReadInt32();
			return new OriginalType(0, sum);
		}

		public override void Write(ref MessagePackWriter writer, in OriginalType? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write(value.Sum);
		}
	}

	private class OriginalTypeConverterFactory : IMessagePackConverterFactory
	{
		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		{
			return type == typeof(OriginalType) ? new OriginalTypeConverter() : null;
		}
	}

	[GenerateShapeFor<OriginalType[]>]
	[GenerateShapeFor<OpenGenericDataType<int>>]
	private partial class Witness;
}
