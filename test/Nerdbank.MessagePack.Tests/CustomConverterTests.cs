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
	public void RegisterWorksAfterFirstSerialization()
	{
		this.AssertRoundtrip(new Tree(3));

		// Registering a converter after serialization is allowed (but will reset the converter cache).
		TreeConverter treeConverter = new();
		this.Serializer.RegisterConverter(treeConverter);

		// Verify that the converter was used.
		this.AssertRoundtrip(new Tree(3));
		Assert.Equal(2, treeConverter.InvocationCount);
	}

	[Fact]
	public void UseNonGenericSubConverters_ShapeProvider()
	{
		this.Serializer.RegisterConverter(new CustomTypeConverterNonGenericTypeShapeProvider());
		this.AssertRoundtrip(new CustomType { InternalProperty = "Hello, World!" });
	}

	[Fact]
	public void UseNonGenericSubConverters_Shape()
	{
		this.Serializer.RegisterConverter(new CustomTypeConverterNonGenericTypeShape());
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

	[GenerateShape]
	[MessagePackConverter(typeof(StatefulConverter))]
	internal partial record struct TypeWithStatefulConverter(int Value);

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
				string? value = context.GetConverter<string>(ShapeProvider).Read(ref reader, context: context);
				return new CustomType { InternalProperty = value };
			}

			public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
			{
				context.GetConverter<string>(ShapeProvider).Write(ref writer, value?.InternalProperty, context);
			}
		}
	}

	[GenerateShape<string>]
	private partial class CustomTypeConverterNonGenericTypeShapeProvider : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			string? value = (string?)context.GetConverter(typeof(string), ShapeProvider).Read(ref reader, context);
			return new CustomType { InternalProperty = value };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			context.GetConverter(typeof(string), ShapeProvider).Write(ref writer, value?.InternalProperty, context);
		}
	}

	[GenerateShape<string>]
	private partial class CustomTypeConverterNonGenericTypeShape : MessagePackConverter<CustomType>
	{
		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			string? value = (string?)context.GetConverter(ShapeProvider.GetShape(typeof(string))!).Read(ref reader, context);
			return new CustomType { InternalProperty = value };
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			context.GetConverter(ShapeProvider.GetShape(typeof(string)!)!).Write(ref writer, value?.InternalProperty, context);
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

	private class StatefulConverter : MessagePackConverter<TypeWithStatefulConverter>
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
}
