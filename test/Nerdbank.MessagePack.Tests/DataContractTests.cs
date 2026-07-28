// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Serialization;
using PolyType.ReflectionProvider;

public class DataContractTests : MessagePackSerializerTestBase
{
	[Fact]
	public void InheritedKnownTypesRoundtripWithOneDiscriminator()
	{
		C value = new() { AValue = 1, BValue = 2, CValue = 3 };

		AssertKnownTypeRoundtrip<A>(value);
		AssertKnownTypeRoundtrip<B>(value);

		void AssertKnownTypeRoundtrip<T>(C knownTypeValue)
			where T : A
		{
			ITypeShape<T> shape = ReflectionTypeShapeProvider.Default.GetTypeShape<T>();
			T serializedValue = (T)(A)knownTypeValue;
			byte[] bytes = this.Serializer.Serialize(in serializedValue, shape, TestContext.Current.CancellationToken);

			MessagePackReader reader = new(bytes);
			Assert.Equal(2, reader.ReadArrayHeader());
			Assert.Equal(typeof(C).FullName, reader.ReadString());
			Assert.Equal(MessagePackType.Map, reader.NextMessagePackType);

			C result = Assert.IsType<C>(this.Serializer.Deserialize(bytes, shape, TestContext.Current.CancellationToken));
			Assert.Equal(knownTypeValue.AValue, result.AValue);
			Assert.Equal(knownTypeValue.BValue, result.BValue);
			Assert.Equal(knownTypeValue.CValue, result.CValue);
		}
	}

	[Fact]
	public void RuntimeDerivedTypeUnionTakesPrecedenceOverKnownTypes()
	{
		DerivedTypeMapping<B> mapping = new(ReflectionTypeShapeProvider.Default)
		{
			[17] = typeof(C),
		};
		this.Serializer = this.Serializer with { DerivedTypeUnions = [mapping] };
		ITypeShape<B> shape = ReflectionTypeShapeProvider.Default.GetTypeShape<B>();

		byte[] bytes = this.Serializer.Serialize<B>(new C { AValue = 1, BValue = 2, CValue = 3 }, shape, TestContext.Current.CancellationToken);
		MessagePackReader reader = new(bytes);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(17, reader.ReadInt32());
		Assert.Equal(MessagePackType.Map, reader.NextMessagePackType);

		C result = Assert.IsType<C>(this.Serializer.Deserialize(bytes, shape, TestContext.Current.CancellationToken));
		Assert.Equal(3, result.CValue);
	}

	[Fact]
	public void TypesWithoutKnownTypesRetainDefaultPolymorphism()
	{
		ITypeShape<DefaultBase> shape = ReflectionTypeShapeProvider.Default.GetTypeShape<DefaultBase>();
		byte[] bytes = this.Serializer.Serialize<DefaultBase>(new DefaultDerived { BaseValue = 1, DerivedValue = 2 }, shape, TestContext.Current.CancellationToken);

		MessagePackReader reader = new(bytes);
		Assert.Equal(MessagePackType.Map, reader.NextMessagePackType);

		DefaultBase result = Assert.IsType<DefaultBase>(this.Serializer.Deserialize(bytes, shape, TestContext.Current.CancellationToken));
		Assert.Equal(1, result.BaseValue);
	}

	[DataContract]
	[KnownType(typeof(B))]
	[KnownType(typeof(C))]
	private class A
	{
		[DataMember]
		public int AValue { get; set; }
	}

	[DataContract]
	private class B : A
	{
		[DataMember]
		public int BValue { get; set; }
	}

	[DataContract]
	private class C : B
	{
		[DataMember]
		public int CValue { get; set; }
	}

	private class DefaultBase
	{
		public int BaseValue { get; set; }
	}

	private class DefaultDerived : DefaultBase
	{
		public int DerivedValue { get; set; }
	}
}
