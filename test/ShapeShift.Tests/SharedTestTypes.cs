// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit.Sdk;
using static SharedTestTypes;

internal static partial class SharedTestTypes
{
	public enum SomeEnum
	{
		A,
		B,
		C,
	}

	[GenerateShape]
	public partial class ClassWithReadOnlyCollectionProperties : IEquatable<ClassWithReadOnlyCollectionProperties>
	{
		public List<string> List { get; } = new();

		public Dictionary<string, string> Dictionary { get; } = new();

		public bool Equals(ClassWithReadOnlyCollectionProperties? other)
			=> StructuralEquality.Equal(this.List, other?.List) && StructuralEquality.Equal(this.Dictionary, other?.Dictionary);
	}

	[GenerateShape]
	public partial class Fruit : IEquatable<Fruit>
	{
		public int Seeds { get; set; }

		public bool Equals(Fruit? other) => other is not null && this.Seeds == other.Seeds;
	}

	[GenerateShape]
	public partial class IntRichPoco : IEquatable<IntRichPoco>
	{
		public byte UInt8 { get; set; }

		public ushort UInt16 { get; set; }

		public uint UInt32 { get; set; }

		public ulong UInt64 { get; set; }

		public sbyte Int8 { get; set; }

		public short Int16 { get; set; }

		public int Int32 { get; set; }

		public long Int64 { get; set; }

		public bool Equals(IntRichPoco? other)
			=> other is not null
			&& this.UInt8 == other.UInt8
			&& this.UInt16 == other.UInt16
			&& this.UInt32 == other.UInt32
			&& this.UInt64 == other.UInt64
			&& this.Int8 == other.Int8
			&& this.Int16 == other.Int16
			&& this.Int32 == other.Int32
			&& this.Int64 == other.Int64;
	}

	[GenerateShape]
	public partial record OtherPrimitiveTypes(string AString, bool ABoolean, float AFloat, double ADouble);

	[GenerateShape]
	public partial class EmptyClass : IEquatable<EmptyClass>
	{
		public bool Equals(EmptyClass? other) => other is not null;
	}

	[GenerateShape]
	public partial record RecordClass(int Seeds)
	{
		public int Weight { get; set; }

		public int ChildNumber { get; init; }
	}

	[GenerateShape]
	public partial class DefaultCtorWithInitProperty : IEquatable<DefaultCtorWithInitProperty>
	{
		public int Age { get; init; }

		public bool Equals(DefaultCtorWithInitProperty? other) => other is not null && this.Age == other.Age;
	}

	[GenerateShape]
	public partial record RecordWithNullableStruct(int? Value);

	[GenerateShape]
	public partial class ClassWithDictionary : IEquatable<ClassWithDictionary>
	{
		public Dictionary<string, int>? StringInt { get; set; }

		public bool Equals(ClassWithDictionary? other) => other is not null && StructuralEquality.Equal(this.StringInt, other.StringInt);
	}

	[GenerateShape]
	public partial class ClassWithImmutableDictionary : IEquatable<ClassWithImmutableDictionary>
	{
		public ImmutableDictionary<string, int>? StringInt { get; set; }

		public bool Equals(ClassWithImmutableDictionary? other) => other is not null && StructuralEquality.Equal(this.StringInt, other.StringInt);
	}

	[GenerateShape]
	public partial class ClassWithArray : IEquatable<ClassWithArray>
	{
		public int[]? IntArray { get; set; }

		public bool Equals(ClassWithArray? other) => other is not null && StructuralEquality.Equal(this.IntArray, other.IntArray);
	}

	[GenerateShape]
	public partial class ClassWithEnumerable : IEquatable<ClassWithEnumerable>
	{
		public IEnumerable<int>? IntEnum { get; set; }

		public bool Equals(ClassWithEnumerable? other) => other is not null && StructuralEquality.Equal(this.IntEnum, other.IntEnum);
	}

	[GenerateShape]
	public partial record HasEnum(SomeEnum Value);

	[GenerateShape]
	public partial class HasMultiDimensionalArray : IEquatable<HasMultiDimensionalArray>
	{
		public int[,]? Array2D { get; set; }

		public int[,,]? Array3D { get; set; }

		public bool Equals(HasMultiDimensionalArray? other) => other is not null && StructuralEquality.Equal<int>(this.Array2D, other.Array2D) && StructuralEquality.Equal<int>(this.Array3D, other.Array3D);
	}

	public record UnannotatedPoco
	{
		public int Value { get; set; }
	}

	[GenerateShape]
	public partial record ReferencesUnannotatedPoco
	{
		public UnannotatedPoco? Poco { get; set; }
	}

	[GenerateShape]
	internal partial record InternalRecordWithPrivateField
	{
		[PropertyShape]
		private int privateField;

		[PropertyShape(Ignore = true)]
		internal int PrivateFieldAccessor
		{
			get => this.privateField;
			set => this.privateField = value;
		}

		[PropertyShape(Ignore = true)]
		internal int PrivatePropertyAccessor
		{
			get => this.PrivateProperty;
			set => this.PrivateProperty = value;
		}

		[PropertyShape]
		private int PrivateProperty { get; set; }
	}

	[GenerateShape]
	internal partial record RecordWithReadOnlyProperties(int A, int B)
	{
		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial class RecordWithReadOnlyProperties_NoConstructor
	{
		internal RecordWithReadOnlyProperties_NoConstructor(int a, int b)
		{
			this.A = a;
			this.B = b;
		}

		public int A { get; set; }

		public int B { get; set; }

		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial record RecordWithReadOnlyPropertiesKeyed([property: Key(0)] int A, [property: Key(1)] int B)
	{
		[PropertyShape(Ignore = true)]
		public int Sum => this.A + this.B;
	}

	[GenerateShape]
	internal partial record TypeWithConstructorParameterMatchingSerializedPropertyName
	{
		public TypeWithConstructorParameterMatchingSerializedPropertyName(int otherName = 8)
			=> this.Marshaled = otherName;

		[PropertyShape(Name = "otherName")]
		public int Marshaled { get; set; }
	}

	[GenerateShape]
	internal partial class ClassWithIndexer
	{
		public int Member { get; set; }

		public int this[int index] => index;

		public override bool Equals(object? obj) => obj is ClassWithIndexer other && this.Member == other.Member;

		public override int GetHashCode() => this.Member;
	}

	[GenerateShape<UnannotatedPoco>]
	[GenerateShape<object>]
	[GenerateShape<string>]
	[GenerateShape<byte[]>]
	[GenerateShape<Memory<byte>>]
	[GenerateShape<ReadOnlyMemory<byte>>]
	internal partial class Witness;

	internal class CustomStringConverter : Converter<string>
	{
		public override string? Read(ref Reader reader, SerializationContext context)
			=> reader.ReadString() + "R";

		public override void Write(ref Writer writer, in string? value, SerializationContext context)
			=> writer.Write(value + "W");
	}
}
