// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[assembly: TypeShapeExtension(typeof(CustomConverterTests.GenericData<>), AssociatedTypes = [typeof(CustomConverterTests.GenericDataConverter2<>)])]
[assembly: TypeShapeExtension(typeof(CustomConverterTests.GenericData<>), AssociatedTypes = [typeof(CustomConverterTests.GenericDataConverterNonGeneric)])]

public partial class CustomConverterTests : MessagePackSerializerTestBase
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
	public void RegisterWorksAfterFirstSerialization()
	{
		this.AssertRoundtrip(new Tree(3));

		// Registering a converter after serialization is allowed (but will reset the converter cache).
		TreeConverter treeConverter = new();
		this.Serializer = this.Serializer with { Converters = [treeConverter] };

		// Verify that the converter was used.
		this.AssertRoundtrip(new Tree(3));
		Assert.Equal(2, treeConverter.InvocationCount);
	}

	[Fact]
	public void UseNonGenericSubConverters_ShapeProvider()
	{
		this.Serializer = this.Serializer with { Converters = [new CustomTypeConverterNonGenericTypeShapeProvider()] };
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[Fact]
	public void UseNonGenericSubConverters_Shape()
	{
		this.Serializer = this.Serializer with { Converters = [new CustomTypeConverterNonGenericTypeShape()] };
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[Fact]
	public void StatefulConverters()
	{
		SerializationContext modifiedStarterContext = this.Serializer.StartingContext;
		modifiedStarterContext["ValueMultiplier"] = 3;
		this.Serializer = this.Serializer with
		{
			StartingContext = modifiedStarterContext,
		};
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new TypeWithStatefulConverter(5));

		// Assert that the multiplier state had the intended impact.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(5 * 3, reader.ReadInt32());

		// Assert that state dictionary changes made by the converter do not impact the caller.
		Assert.Null(this.Serializer.StartingContext["SHOULDVANISH"]);
	}

	[Fact]
	public void GenericDataAndGenericConverterByOpenGenericAttribute()
	{
		GenericData<string>? deserialized = this.Roundtrip<GenericData<string>, Witness>(new GenericData<string> { Value = "Hello, World!" });
		Assert.Equal("Hello, World!11", deserialized?.Value);
	}

	[Fact]
	public void GenericDataAndGenericConverterByClosedGenericRuntimeRegistration()
	{
		this.Serializer = this.Serializer with { Converters = [new GenericDataConverter2<string>()] };
		GenericData<string>? deserialized = this.Roundtrip<GenericData<string>, Witness>(new GenericData<string> { Value = "Hello, World!" });
		Assert.Equal("Hello, World!22", deserialized?.Value);
	}

	[Fact]
	public void GenericDataAndGenericConverterByOpenGenericRuntimeRegistration()
	{
		this.Serializer = this.Serializer with { ConverterTypes = [typeof(GenericDataConverter2<>)] };
		GenericData<string>? deserialized = this.Roundtrip<GenericData<string>, Witness>(new GenericData<string> { Value = "Hello, World!" });
		Assert.Equal("Hello, World!22", deserialized?.Value);
	}

	[Fact]
	public void GenericDataAndNonGenericConverterByRuntimeRegistration()
	{
		this.Serializer = this.Serializer with { ConverterTypes = [typeof(GenericDataConverterNonGeneric)] };
		GenericData<string>? deserialized = this.Roundtrip<GenericData<string>, Witness>(new GenericData<string> { Value = "Hello, World!" });
		Assert.Equal("Hello, World!44", deserialized?.Value);

		// Verify that other type arguments than that supported by the runtime registered converter invokes the attribute-registered converter.
		GenericData<int>? deserialized2 = this.Roundtrip<GenericData<int>, Witness>(new GenericData<int> { Value = "Hello, World!" });
		Assert.Equal("Hello, World!11", deserialized2?.Value);
	}

	[Fact]
	public void NonGenericRuntimeRegistrationOfConverterByType()
	{
		this.Serializer = this.Serializer with { ConverterTypes = [typeof(TreeConverterPlus2)] };
		Tree? deserialized = this.Roundtrip(new Tree(3));
		Assert.Equal(5, deserialized?.FruitCount);
	}

	[Fact]
	public void GenericDataAndGenericConverterByOpenGenericRuntimeRegistration_NotAssociated()
	{
		this.Serializer = this.Serializer with { ConverterTypes = [typeof(GenericDataConverter3<>)] };
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<GenericData<string>, Witness>(new GenericData<string> { Value = "Hello, World!" }, TestContext.Current.CancellationToken));
		this.Logger.WriteLine(ex.Message);
	}

	[GenerateShape]
	[MessagePackConverter(typeof(StatefulConverter))]
	internal partial record struct TypeWithStatefulConverter(int Value);

	[GenerateShape]
	[AssociatedTypeShape(typeof(TreeConverterPlus2))]
	public partial record Tree(int FruitCount);

	public class TreeConverterPlus2 : MessagePackConverter<Tree>
	{
		public override Tree? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			return new Tree(reader.ReadInt32() + 1);
		}

		public override void Write(ref MessagePackWriter writer, in Tree? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write(value.FruitCount + 1);
		}
	}

	[GenerateShape, MessagePackConverter(typeof(CustomTypeConverter))]
	public partial record CustomType
	{
		internal string? InternalProperty { get; set; }

		public override string ToString() => this.InternalProperty ?? "(null)";

		[GenerateShapeFor<string>]
		internal partial class CustomTypeConverter : MessagePackConverter<CustomType>
		{
			public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
			{
				string? value = context.GetConverter<string>(GeneratedTypeShapeProvider).Read(ref reader, context);
				return new CustomType { InternalProperty = value };
			}

			public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
			{
				context.GetConverter<string>(GeneratedTypeShapeProvider).Write(ref writer, value?.InternalProperty, context);
			}
		}
	}

	[MessagePackConverter(typeof(GenericDataConverter<>))]
	public partial record GenericData<T>
	{
		internal string? Value { get; set; }
	}

	public partial class GenericDataConverter<T> : MessagePackConverter<GenericData<T>>
	{
		public override GenericData<T>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected array of length 1.");
			}

			return new GenericData<T>
			{
				Value = reader.ReadString() + "1",
			};
		}

		public override void Write(ref MessagePackWriter writer, in GenericData<T>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.Value + "1");
		}
	}

	public partial class GenericDataConverter2<T> : MessagePackConverter<GenericData<T>>
	{
		public override GenericData<T>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected array of length 1.");
			}

			return new GenericData<T>
			{
				Value = reader.ReadString() + "2",
			};
		}

		public override void Write(ref MessagePackWriter writer, in GenericData<T>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.Value + "2");
		}
	}

	public partial class GenericDataConverter3<T> : MessagePackConverter<GenericData<T>>
	{
		public override GenericData<T>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected array of length 1.");
			}

			return new GenericData<T>
			{
				Value = reader.ReadString() + "3",
			};
		}

		public override void Write(ref MessagePackWriter writer, in GenericData<T>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.Value + "3");
		}
	}

	public partial class GenericDataConverterNonGeneric : MessagePackConverter<GenericData<string>>
	{
		public override GenericData<string>? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int count = reader.ReadArrayHeader();
			if (count != 1)
			{
				throw new MessagePackSerializationException("Expected array of length 1.");
			}

			return new GenericData<string>
			{
				Value = reader.ReadString() + "4",
			};
		}

		public override void Write(ref MessagePackWriter writer, in GenericData<string>? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.Value + "4");
		}
	}

	internal class StatefulConverter : MessagePackConverter<TypeWithStatefulConverter>
	{
		public override TypeWithStatefulConverter Read(ref MessagePackReader reader, SerializationContext context)
		{
			int multiplier = (int)context["ValueMultiplier"]!;
			int serializedValue = reader.ReadInt32();
			return new TypeWithStatefulConverter(serializedValue / multiplier);
		}

		public override void Write(ref MessagePackWriter writer, in TypeWithStatefulConverter value, SerializationContext context)
		{
			int multiplier = (int)context["ValueMultiplier"]!;
			writer.Write(value.Value * multiplier);

			// This is used by the test to validate that additions to the state dictionary do not impact callers (though it may impact callees).
			context["SHOULDVANISH"] = new object();
		}
	}

	[GenerateShapeFor<string>]
	private partial class CustomTypeConverterNonGenericTypeShapeProvider : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			string? value = (string?)context.GetConverter(typeof(string), GeneratedTypeShapeProvider).ReadObject(ref reader, context);
			return new CustomType { InternalProperty = value };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			context.GetConverter(typeof(string), GeneratedTypeShapeProvider).WriteObject(ref writer, value?.InternalProperty, context);
		}
	}

	[GenerateShapeFor<string>]
	private partial class CustomTypeConverterNonGenericTypeShape : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			string? value = (string?)context.GetConverter(GeneratedTypeShapeProvider.GetTypeShape(typeof(string))!).ReadObject(ref reader, context);
			return new CustomType { InternalProperty = value };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			context.GetConverter(GeneratedTypeShapeProvider.GetTypeShape(typeof(string)!)!).WriteObject(ref writer, value?.InternalProperty, context);
		}
	}

	private class NoOpConverter : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context) => throw new NotImplementedException();

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context) => throw new NotImplementedException();
	}

	/// <summary>
	/// A <see cref="Tree"/> converter that may be <em>optionally</em> applied at runtime.
	/// It should <em>not</em> be referenced from <see cref="Tree"/> via <see cref="MessagePackConverterAttribute"/>.
	/// </summary>
	private class TreeConverter : MessagePackConverter<Tree>
	{
		public int InvocationCount { get; private set; }

		public override Tree? Read(ref MessagePackReader reader, SerializationContext context)
		{
			this.InvocationCount++;
			if (reader.TryReadNil())
			{
				return null;
			}

			return new Tree(reader.ReadInt32());
		}

		public override void Write(ref MessagePackWriter writer, in Tree? value, SerializationContext context)
		{
			this.InvocationCount++;
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write(value.FruitCount);
		}
	}

	[GenerateShapeFor<GenericData<string>>]
	[GenerateShapeFor<GenericData<int>>]
	private partial class Witness;
}
