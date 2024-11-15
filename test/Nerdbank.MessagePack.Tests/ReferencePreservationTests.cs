﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ReferencePreservationTests : MessagePackSerializerTestBase
{
	public ReferencePreservationTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.Serializer = this.Serializer with { PreserveReferences = true };
	}

	[Fact]
	public void ObjectReferencePreservation()
	{
		object value = new();
		RecordWithObjects root = new()
		{
			Value1 = value,
			Value2 = value,
			Value3 = new object(),
		};
		RecordWithObjects? deserializedRoot = this.Roundtrip(root);
		Assert.NotNull(deserializedRoot);

		// Verify that reference equality is also satisfied within the deserialized tree.
		Assert.Same(deserializedRoot.Value1, deserializedRoot.Value2);
		Assert.NotSame(deserializedRoot.Value3, deserializedRoot.Value1);
	}

	[Fact]
	public void CustomConverterByAttributeSkippedByReferencePreservation()
	{
		CustomType2 value = new() { Message = "test" };
		CustomType2[] array = [value, value];
		CustomType2[]? deserializedArray = this.Roundtrip<CustomType2[], CustomType2Converter>(array);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
	}

	[Fact]
	public void CustomConverterByRegistrationSkippedByReferencePreservation()
	{
		this.Serializer.RegisterConverter(new CustomTypeConverter());
		CustomType value = new() { Message = "test" };
		CustomType[] array = [value, value];
		CustomType[]? deserializedArray = this.Roundtrip<CustomType[], Witness>(array);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
	}

	[Fact]
	public void CustomConverterGetsReferencePreservingPrimitiveConverter()
	{
		string stringValue = "test";
		CustomType2[] array = [new() { Message = stringValue }, new() { Message = stringValue }];
		CustomType2[]? deserializedArray = this.Roundtrip<CustomType2[], CustomType2Converter>(array);
		Assert.NotNull(deserializedArray);
		Assert.NotSame(deserializedArray[0], deserializedArray[1]);
		Assert.Same(deserializedArray[0].Message, deserializedArray[1].Message);
	}

	[Fact]
	public void CustomConverterGetsReferencePreservingNonPrimitiveConverter()
	{
		CustomType inner = new() { Message = "Hi" };
		CustomTypeWrapper wrapper1 = new(inner);
		CustomTypeWrapper wrapper2 = new(inner);
		CustomTypeWrapper[]? deserialized = this.Roundtrip<CustomTypeWrapper[], Witness>([wrapper1, wrapper2]);
		Assert.NotNull(deserialized);
		Assert.NotSame(deserialized[0], deserialized[1]);
		Assert.Same(deserialized[0].Value, deserialized[1].Value);
	}

	[Fact]
	public void StringReferencePreservation()
	{
		string city = "New York";
		string state = city; // same reference.
		RecordWithStrings root = new()
		{
			City = city,
			State = state,
		};
		RecordWithStrings? deserializedRoot = this.Roundtrip(root);
		Assert.NotNull(deserializedRoot);

		// Verify that value equality is satisfied.
		Assert.Equal(root, deserializedRoot);

		// Verify that reference equality is also satisfied within the deserialized tree.
		Assert.Same(deserializedRoot.City, deserializedRoot.State);
	}

	/// <summary>
	/// Verifies that two distinct object whose by-value equality is considered equal are <em>combined</em> into just one reference.
	/// </summary>
	/// <remarks>
	/// This is important because the two objects with equal value in the object graph before serialization could me mutated independently.
	/// A round-trip through serialization should not combine these into a single reference or mutation of one would affect its appearance elsewhere in the graph.
	/// </remarks>
	[Fact]
	public void ReferenceDistinctionBetweenEquivalentValuesIsPreserved()
	{
		CustomType2[] array = [new() { Message = "test" }, new() { Message = "test" }];
		CustomType2[]? deserializedArray = this.Roundtrip<CustomType2[], CustomType2Converter>(array);
		Assert.NotNull(deserializedArray);
		Assert.NotSame(deserializedArray[0], deserializedArray[1]);
	}

	[GenerateShape]
	public partial record RecordWithStrings
	{
		public string? City { get; init; }

		public string? State { get; init; }
	}

	[GenerateShape]
	public partial record RecordWithObjects
	{
		public object? Value1 { get; init; }

		public object? Value2 { get; init; }

		public object? Value3 { get; init; }
	}

	[GenerateShape]
	public partial record CustomType
	{
		internal string? Message { get; set; }
	}

	[GenerateShape]
	public partial record CustomTypeWrapper(CustomType Value);

	[GenerateShape<string>]
	internal partial class CustomTypeConverter : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected an array of length 1.");
			}

			string? message = context.GetConverter<string, CustomTypeConverter>().Read(ref reader, context);
			return new CustomType { Message = message };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			context.GetConverter<string, CustomTypeConverter>().Write(ref writer, value.Message, context);
		}
	}

	[GenerateShape, MessagePackConverter(typeof(CustomType2Converter))]
	public partial record CustomType2
	{
		internal string? Message { get; set; }
	}

	[GenerateShape<string>]
	[GenerateShape<CustomType2[]>]
	internal partial class CustomType2Converter : MessagePackConverter<CustomType2>
	{
		public override CustomType2? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected an array of length 1.");
			}

			string? message = context.GetConverter<string, CustomType2Converter>().Read(ref reader, context);
			return new CustomType2 { Message = message };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType2? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			context.GetConverter<string, CustomType2Converter>().Write(ref writer, value.Message, context);
		}
	}

	[GenerateShape<CustomTypeWrapper[]>]
	[GenerateShape<CustomType[]>]
	private partial class Witness;
}