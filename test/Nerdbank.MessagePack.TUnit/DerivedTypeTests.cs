// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class DerivedTypeTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task BaseType(bool async)
	{
		BaseClass value = new() { BaseClassProperty = 5 };
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);

		// Assert that it's serialized in its special syntax that allows for derived types.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		reader.ReadNil();
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
	}

	[Test]
	public void BaseTypeExplicitIdentifier()
	{
		BaseTypeExplicitBase? result = this.Roundtrip(new BaseTypeExplicitBase());

		// Assert that an array wrapper was created.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(2, reader.ReadArrayHeader());

		// We don't care which of the identifiers from the attribute were picked,
		// but we want to make sure it isn't the null default used w/o attributes.
		Assert.False(reader.TryReadNil());
		reader.Skip(default);

		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Test]
	public void BaseTypeExplicitIdentifier_RuntimeMapping()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Required };

		DerivedShapeMapping<BaseClass> mapping = new();
		mapping.Add<BaseClass>(3, Witness.GeneratedTypeShapeProvider);
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };

		BaseClass? result = this.Roundtrip(new BaseClass());

		// Assert that an array wrapper was created.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(3, reader.ReadInt32());
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Test]
	public void DerivedAType()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 });

		// Assert that this has no special header because it has no Union attribute of its own.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadMapHeader());
		Assert.Equal(nameof(DerivedA.DerivedAProperty), reader.ReadString());
		Assert.Equal(6, reader.ReadInt32());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
		Assert.Equal(5, reader.ReadInt32());
		Assert.True(reader.End);
	}

	[Test, MatrixDataSource]
	public async Task DerivedA_AsBaseType(bool async)
	{
		var value = new DerivedA { BaseClassProperty = 5, DerivedAProperty = 6 };
		if (async)
		{
			await this.AssertRoundtripAsync<BaseClass>(value);
		}
		else
		{
			this.AssertRoundtrip<BaseClass>(value);
		}
	}

	[Test]
	public void DerivedAA_AsBaseType() => this.AssertRoundtrip<BaseClass>(new DerivedAA { BaseClassProperty = 5, DerivedAProperty = 6 });

	[Test]
	public void DerivedB_AsBaseType() => this.AssertRoundtrip<BaseClass>(new DerivedB(10) { BaseClassProperty = 5 });

	[Test]
	public void EnumerableDerived_BaseType()
	{
		// This is a lossy operation. Only the collection elements are serialized,
		// and the class cannot be deserialized because the constructor doesn't take a collection.
		EnumerableDerived value = new(3) { BaseClassProperty = 5 };
		byte[] msgpack = this.Serializer.Serialize<BaseClass>(value, this.TimeoutToken);
		Console.WriteLine(this.Serializer.ConvertToJson(msgpack));
	}

	[Test]
	public void ClosedGenericDerived_BaseType() => this.AssertRoundtrip<BaseClass>(new DerivedGeneric<int>(5) { BaseClassProperty = 10 });

	[Test, MatrixDataSource]
	public async Task Null(bool async)
	{
		if (async)
		{
			await this.AssertRoundtripAsync<BaseClass>(null);
		}
		else
		{
			this.AssertRoundtrip<BaseClass>(null);
		}
	}

	[Test]
	public void UnrecognizedAlias()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(2);
		writer.Write(100);
		writer.WriteMapHeader(0);
		writer.Flush();

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence, this.TimeoutToken));
		Console.WriteLine(ex.Message);
	}

	[Test]
	public void UnrecognizedArraySize()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(3);
		writer.Write(100);
		writer.WriteNil();
		writer.WriteNil();
		writer.Flush();

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<BaseClass>(sequence, this.TimeoutToken));
		Console.WriteLine(ex.Message);
	}

	[Test]
	public void UnknownDerivedType()
	{
		BaseClass? result = this.Roundtrip<BaseClass>(new UnknownDerived());
		Assert.IsType<BaseClass>(result);
	}

	[Test, MatrixDataSource]
	public void UnknownDerivedType_PrefersClosestMatch(bool runtimeMapping)
	{
		if (runtimeMapping)
		{
			DerivedShapeMapping<BaseClass> mapping = new();
			mapping.Add<DerivedA>(1, Witness.GeneratedTypeShapeProvider);
			mapping.Add<DerivedAA>(2, Witness.GeneratedTypeShapeProvider);
			mapping.Add<DerivedB>(3, Witness.GeneratedTypeShapeProvider);
			this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };
		}

		Assert.IsType<DerivedA>(this.Roundtrip<BaseClass>(new DerivedAUnknown()));
		Assert.IsType<DerivedAA>(this.Roundtrip<BaseClass>(new DerivedAAUnknown()));
	}

	[Test, MatrixDataSource]
	public async Task MixedAliasTypes(bool async)
	{
		MixedAliasBase value = new MixedAliasDerivedA();
		ReadOnlySequence<byte> msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal("A", reader.ReadString());

		value = new MixedAliasDerived1();
		msgpack = async ? await this.AssertRoundtripAsync(value) : this.AssertRoundtrip(value);
		reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(10, reader.ReadInt32());
	}

	[Test]
	public void ImpliedAlias()
	{
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<ImpliedAliasBase>(new ImpliedAliasDerived());
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(typeof(ImpliedAliasDerived).Name, reader.ReadString());
	}

	[Test]
	public void RecursiveSubTypes()
	{
		// If it were to work, this is how we expect it to work:
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<RecursiveBase>(new RecursiveDerivedDerived());
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(1, reader.ReadInt32());
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(13, reader.ReadInt32());
	}

	[Test]
	public void RuntimeRegistration_Integers()
	{
		DerivedShapeMapping<DynamicallyRegisteredBase> mapping = new();
#if NET
		mapping.Add<DynamicallyRegisteredDerivedA>(1);
		mapping.Add<DynamicallyRegisteredDerivedB>(2);
#else
		mapping.Add<DynamicallyRegisteredDerivedA>(1, Witness.GeneratedTypeShapeProvider);
		mapping.Add<DynamicallyRegisteredDerivedB>(2, Witness.GeneratedTypeShapeProvider);
#endif
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };

		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredBase());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedA());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedB());
	}

	[Test]
	public void RuntimeRegistration_Strings()
	{
		DerivedShapeMapping<DynamicallyRegisteredBase> mapping = new();
#if NET
		mapping.Add<DynamicallyRegisteredDerivedA>("A");
		mapping.Add<DynamicallyRegisteredDerivedB>("B");
#else
		mapping.Add<DynamicallyRegisteredDerivedA>("A", Witness.GeneratedTypeShapeProvider);
		mapping.Add<DynamicallyRegisteredDerivedB>("B", Witness.GeneratedTypeShapeProvider);
#endif
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };

		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredBase());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedA());
		this.AssertRoundtrip<DynamicallyRegisteredBase>(new DynamicallyRegisteredDerivedB());
	}

	[Test]
	public void RuntimeRegistration_OverridesStatic()
	{
		DerivedShapeMapping<BaseClass> mapping = new();
		mapping.Add<DerivedB>(1, Witness.GeneratedTypeShapeProvider);
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };

		// Verify that the base type has just one header.
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<BaseClass>(new BaseClass { BaseClassProperty = 5 });
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		reader.ReadNil();
		Assert.Equal(1, reader.ReadMapHeader());

		// Verify that the header type value is the runtime-specified 1 instead of the static 3.
		msgpack = this.AssertRoundtrip<BaseClass>(new DerivedB(13));
		reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(1, reader.ReadInt32());

		// Verify that statically set subtypes are not recognized if no runtime equivalents are registered.
		Assert.IsType<BaseClass>(this.Roundtrip<BaseClass>(new DerivedA()));
	}

	/// <summary>
	/// Verify that an empty mapping is allowed and produces the schema that allows for sub-types to be added in the future.
	/// </summary>
	[Test]
	public void RuntimeRegistration_EmptyMapping()
	{
		DerivedShapeMapping<DynamicallyRegisteredBase> mapping = new();
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip(new DynamicallyRegisteredBase());
		MessagePackReader reader = new(msgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		reader.ReadNil();
		Assert.Equal(0, reader.ReadMapHeader());
	}

	[Test]
	public void CustomConverter_InvokedAsUnionCase_WhenSetAsRuntimeConverter()
	{
		this.Serializer = this.Serializer with { Converters = [new BaseClassCustomConverter()] };

		this.AssertRoundtrip<BaseClass>(new() { BaseClassProperty = 5 });

		// We expect the derived type data to be preserved because a runtime-specified custom converter
		// on the base type is chosen only after exploring the visitor to discover that there is a union wrapper.
		this.AssertRoundtrip<BaseTypeWithCustomConverterAttribute>(new BaseTypeWithCustomConverterAttributeDerived { BaseClassProperty = 8, DerivedAProperty = 10 });
	}

	[Test]
	public void CustomConverter_InvokedAsUnionCase_WhenSetViaConverterAttribute()
	{
		this.AssertRoundtrip<BaseTypeWithCustomConverterAttribute>(new() { BaseClassProperty = 5 });

		// We expect the derived type data to be preserved because an attribute-specified custom converter
		// on the base type is chosen only after exploring the visitor to discover that there is a union wrapper.
		this.AssertRoundtrip<BaseTypeWithCustomConverterAttribute>(new BaseTypeWithCustomConverterAttributeDerived { BaseClassProperty = 8, DerivedAProperty = 10 });
	}

	[Test]
	public void CustomConverter_InvokedAsUnionCase_WhenSetViaConverterAttributeOnMember()
	{
		this.AssertRoundtrip<HasUnionMemberWithMemberAttribute>(new() { Value = new BaseClass { BaseClassProperty = 5 } });

		// In the case of a MessagePackConverter attribute on a property,
		// where DerivedTypeShapeAttribute cannot be seen, we expect that the most likely expectation
		// for the user is that the converter apply all the time for all union cases,
		// in which case, the custom converter we specify is in full control without the union wrapper.
		HasUnionMemberWithMemberAttribute? deserialized = this.Roundtrip<HasUnionMemberWithMemberAttribute>(new() { Value = new DerivedA { BaseClassProperty = 8, DerivedAProperty = 10 } });
		Assert.Equal(new HasUnionMemberWithMemberAttribute { Value = new BaseClass { BaseClassProperty = 8 } }, deserialized);
	}

	[Test]
	public void DisableAttributeUnionAtRuntime()
	{
		this.Serializer = this.Serializer with
		{
			DerivedTypeUnions = [DerivedTypeUnion.CreateDisabled(typeof(BaseClass))],
		};

		this.AssertRoundtrip(new BaseClass { BaseClassProperty = 5 });

		// Assert that no union wrapper was added.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(BaseClass.BaseClassProperty), reader.ReadString());
	}

	[GenerateShapeFor<DerivedGeneric<int>>]
	internal partial class Witness;

	[GenerateShape]
	[DerivedTypeShape(typeof(BaseTypeExplicitBase), Name = "Me", Tag = 3)]
	internal partial class BaseTypeExplicitBase;

	[GenerateShape]
	[DerivedTypeShape(typeof(DerivedA), Tag = 1)]
	[DerivedTypeShape(typeof(DerivedAA), Tag = 2)]
	[DerivedTypeShape(typeof(DerivedB), Tag = 3)]
	[DerivedTypeShape(typeof(EnumerableDerived), Tag = 4)]
	[DerivedTypeShape(typeof(DerivedGeneric<int>), Tag = 5)]
	public partial record BaseClass
	{
		public int BaseClassProperty { get; set; }
	}

	[GenerateShape]
	public partial record DerivedA() : BaseClass
	{
		public int DerivedAProperty { get; set; }
	}

	public record DerivedAA : DerivedA
	{
	}

	public record DerivedAUnknown : DerivedA;

	public record DerivedAAUnknown : DerivedAA;

	public record DerivedB(int DerivedBProperty) : BaseClass
	{
	}

	public record EnumerableDerived(int Count) : BaseClass, IEnumerable<int>
	{
		public IEnumerator<int> GetEnumerator() => Enumerable.Range(0, this.Count).GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	public record DerivedGeneric<T>(T Value) : BaseClass
	{
	}

	public record UnknownDerived : BaseClass;

	[GenerateShape]
	[DerivedTypeShape(typeof(MixedAliasDerivedA), Name = "A")]
	[DerivedTypeShape(typeof(MixedAliasDerived1), Tag = 10)]
	public partial record MixedAliasBase;

	public record MixedAliasDerivedA : MixedAliasBase;

	public record MixedAliasDerived1 : MixedAliasBase;

	[GenerateShape]
	[DerivedTypeShape(typeof(ImpliedAliasDerived))]
	public partial record ImpliedAliasBase;

	public record ImpliedAliasDerived : ImpliedAliasBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredDerivedA : DynamicallyRegisteredBase;

	[GenerateShape]
	public partial record DynamicallyRegisteredDerivedB : DynamicallyRegisteredBase;

	[GenerateShape]
	[DerivedTypeShape(typeof(RecursiveDerived), Tag = 1)]
	public partial record RecursiveBase;

	[DerivedTypeShape(typeof(RecursiveDerivedDerived), Tag = 13)]
	public partial record RecursiveDerived : RecursiveBase;

	public record RecursiveDerivedDerived : RecursiveDerived;

	[GenerateShape]
	[DerivedTypeShape(typeof(BaseTypeWithCustomConverterAttributeDerived), Tag = 1)]
	[MessagePackConverter(typeof(BaseClassWithAttributeCustomConverter))]
	public partial record BaseTypeWithCustomConverterAttribute
	{
		public int BaseClassProperty { get; init; }
	}

	[GenerateShape]
	public partial record BaseTypeWithCustomConverterAttributeDerived : BaseTypeWithCustomConverterAttribute
	{
		public int DerivedAProperty { get; init; }
	}

	[GenerateShape]
	public partial record HasUnionMemberWithMemberAttribute
	{
		[MessagePackConverter(typeof(BaseClassCustomConverter))]
		public BaseClass? Value { get; set; }
	}

	internal class BaseClassWithAttributeCustomConverter : MessagePackConverter<BaseTypeWithCustomConverterAttribute>
	{
		public override BaseTypeWithCustomConverterAttribute? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int arrayLength = reader.ReadArrayHeader();
			if (arrayLength != 1)
			{
				throw new MessagePackSerializationException($"Expected array of length 1, but got {arrayLength}.");
			}

			int propertyValue = reader.ReadInt32();
			return new BaseTypeWithCustomConverterAttribute { BaseClassProperty = propertyValue };
		}

		public override void Write(ref MessagePackWriter writer, in BaseTypeWithCustomConverterAttribute? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.BaseClassProperty);
		}
	}

	internal class BaseClassCustomConverter : MessagePackConverter<BaseClass>
	{
		public override BaseClass? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int arrayLength = reader.ReadArrayHeader();
			if (arrayLength != 1)
			{
				throw new MessagePackSerializationException($"Expected array of length 1, but got {arrayLength}.");
			}

			int propertyValue = reader.ReadInt32();
			return new BaseClass { BaseClassProperty = propertyValue };
		}

		public override void Write(ref MessagePackWriter writer, in BaseClass? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteArrayHeader(1);
			writer.Write(value.BaseClassProperty);
		}
	}
}
