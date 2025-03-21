// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

[Trait("ReferencePreservation", "true")]
public partial class ReferencePreservationTests : MessagePackSerializerTestBase
{
	public ReferencePreservationTests(ITestOutputHelper logger)
		: base(logger)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
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

	[Trait("AsyncSerialization", "true")]
	[Fact]
	public async Task AsyncSerialization()
	{
		CustomType o = new();
		CustomType?[] array = [o, o, null];
		CustomType?[]? deserializedArray = await this.RoundtripAsync<CustomType?[], Witness>(array);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
		Assert.Null(deserializedArray[2]);
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
		this.Serializer = this.Serializer with { Converters = [new CustomTypeConverter()] };
		CustomType value = new() { Message = "test" };
		CustomType[] array = [value, value];
		CustomType[]? deserializedArray = this.Roundtrip<CustomType[], Witness>(array);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);

		// Verify that the custom converter actually ran, by verifying that the internal member was serialized.
		Assert.Equal(value, deserializedArray[0]);
	}

	[Fact]
	public void CustomConverterByRegistrationSkippedByReferencePreservation_Reconfigured()
	{
		this.Serializer = this.Serializer with
		{
			PreserveReferences = ReferencePreservationMode.Off,
			Converters = [new CustomTypeConverter()],
		};
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };

		CustomType value = new() { Message = "test" };
		CustomType[] array = [value, value];
		CustomType[]? deserializedArray = this.Roundtrip<CustomType[], Witness>(array);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);

		// Verify that the custom converter actually ran, by verifying that the internal member was serialized.
		Assert.Equal(value, deserializedArray[0]);
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

	[Fact]
	public void DictionaryReferencePreservation()
	{
		Dictionary<string, int> dict = new() { ["a"] = 1, ["b"] = 2 };
		Dictionary<string, int>[]? deserializedArray = this.Roundtrip<Dictionary<string, int>[], Witness>([dict, dict]);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
	}

	[Theory, PairwiseData]
	public void ReferenceConsolidationWhenInterningIsOn(bool interning)
	{
		this.Serializer = this.Serializer with { InternStrings = interning };

		// Create two unique string objects with the same value.
		string city = "New York";
		string city2 = (city + "A")[..^1]; // construct a new instance with the same value.
		Assert.NotSame(city, city2); // sanity check

		string[]? deserialized = this.Roundtrip<string[], Witness>([city, city2]);
		Assert.NotNull(deserialized);

		// We expect equal string references after deserialization iff interning is on.
		Assert.Equal(interning, ReferenceEquals(deserialized[0], deserialized[1]));

		// Only interning should produce an object reference in the serialized form.
		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		reader.ReadString(); // city
		Assert.Equal(interning ? MessagePackType.Extension : MessagePackType.String, reader.NextMessagePackType);
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

	/// <summary>
	/// Verifies that the extension type code used for object references can be customized.
	/// </summary>
	[Fact]
	public void CustomExtensionTypeCode()
	{
		this.Serializer = this.Serializer with
		{
			LibraryExtensionTypeCodes = this.Serializer.LibraryExtensionTypeCodes with
			{
				ObjectReference = 100,
			},
		};

		object value = new();
		RecordWithObjects root = new() { Value1 = value, Value2 = value };
		Sequence<byte> sequence = new();
		this.Serializer.Serialize(sequence, root, TestContext.Current.CancellationToken);
		this.LogMsgPack(sequence);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();
		reader.Skip(this.Serializer.StartingContext); // Value1 name
		reader.Skip(this.Serializer.StartingContext); // Value1 value
		reader.Skip(this.Serializer.StartingContext); // Value2 name
		Assert.Equal(100, reader.ReadExtensionHeader().TypeCode);

		RecordWithObjects? deserializedRoot = this.Serializer.Deserialize<RecordWithObjects>(sequence, TestContext.Current.CancellationToken);
		Assert.NotNull(deserializedRoot);
		Assert.Same(deserializedRoot.Value1, deserializedRoot.Value2);
	}

	[Fact]
	public void KnownSubTypes_StaticRegistration()
	{
		BaseRecord baseInstance = new BaseRecord();
		BaseRecord derivedInstance = new DerivedRecordA();
		BaseRecord[] array = [baseInstance, baseInstance, derivedInstance, derivedInstance];

		BaseRecord[]? deserialized = this.Roundtrip<BaseRecord[], Witness>(array);

		Assert.NotNull(deserialized);
		Assert.IsType<BaseRecord>(deserialized[0]);
		Assert.IsType<DerivedRecordA>(deserialized[2]);
		Assert.Same(deserialized[0], deserialized[1]);
		Assert.Same(deserialized[2], deserialized[3]);
	}

	[Fact]
	public void KnownSubTypes_DynamicRegistration()
	{
		DerivedTypeMapping<BaseRecord> mapping = new();
		mapping.Add<DerivedRecordB>(1, Witness.ShapeProvider);
		this.Serializer = this.Serializer with { DerivedTypeMappings = [mapping] };

		BaseRecord baseInstance = new BaseRecord();
		BaseRecord derivedInstance = new DerivedRecordB();
		BaseRecord[] array = [baseInstance, baseInstance, derivedInstance, derivedInstance];

		BaseRecord[]? deserialized = this.Roundtrip<BaseRecord[], Witness>(array);

		Assert.NotNull(deserialized);
		Assert.IsType<BaseRecord>(deserialized[0]);
		Assert.IsType<DerivedRecordB>(deserialized[2]);
		Assert.Same(deserialized[0], deserialized[1]);
		Assert.Same(deserialized[2], deserialized[3]);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Rejected_DuringSerialization(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		SinglyLinkedListNode first = new();
		first.Next = first;

		if (async)
		{
			await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await this.Serializer.SerializeAsync(Stream.Null, first, TestContext.Current.CancellationToken));
		}
		else
		{
			Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(first, TestContext.Current.CancellationToken));
		}
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Rejected_DuringDeserialization(bool async)
	{
		// First, compose a msgpack buffer that contains cycles.
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		SinglyLinkedListNode first = new();
		first.Next = first;
		byte[] msgpack = this.Serializer.Serialize(first, TestContext.Current.CancellationToken);

		// Now reconfigure the serializer to reject cycles and attempt to deserialize.
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };

		if (async)
		{
			await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await this.Serializer.DeserializeAsync<SinglyLinkedListNode>(PipeReader.Create(new(msgpack)), TestContext.Current.CancellationToken));
		}
		else
		{
			Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<SinglyLinkedListNode>(msgpack, TestContext.Current.CancellationToken));
		}
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_ReferenceSelf(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		SinglyLinkedListNode first = new();
		first.Next = first;
		SinglyLinkedListNode? deserializedRoot = async ? await this.RoundtripAsync(first) : this.Roundtrip(first);
		Assert.NotNull(deserializedRoot);
		Assert.Same(deserializedRoot, deserializedRoot.Next);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Minimal(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		SinglyLinkedListNode first = new();
		SinglyLinkedListNode second = new();
		first.Next = second;
		second.Next = first;
		SinglyLinkedListNode? deserializedRoot = async ? await this.RoundtripAsync(first) : this.Roundtrip(first);
		Assert.NotNull(deserializedRoot);
		Assert.NotSame(deserializedRoot, deserializedRoot.Next);
		Assert.Same(deserializedRoot, deserializedRoot.Next?.Next);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Minimal_WithKeys(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		SinglyLinkedListNodeWithKeyAttribute first = new();
		SinglyLinkedListNodeWithKeyAttribute second = new();
		first.Next = second;
		second.Next = first;
		SinglyLinkedListNodeWithKeyAttribute? deserializedRoot = async ? await this.RoundtripAsync(first) : this.Roundtrip(first);
		Assert.NotNull(deserializedRoot);
		Assert.NotSame(deserializedRoot, deserializedRoot.Next);
		Assert.Same(deserializedRoot, deserializedRoot.Next?.Next);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Multistep(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		SinglyLinkedListNode first = new();
		SinglyLinkedListNode second = new();
		SinglyLinkedListNode third = new();
		first.Next = second;
		second.Next = third;
		third.Next = first;
		SinglyLinkedListNode? deserializedRoot = async ? await this.RoundtripAsync(first) : this.Roundtrip(first);
		Assert.NotNull(deserializedRoot);
		Assert.NotSame(deserializedRoot, deserializedRoot.Next);
		Assert.Same(deserializedRoot, deserializedRoot.Next?.Next?.Next);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_OrderRequirementSatisfied(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		CyclicMustBeFirst first = new();
		CyclicMustBeSecond second = new() { First = first };
		first.Second = second;

		// Arrange to serialize "first"... first.
		CyclicMustBeFirst? deserializedRoot = async ? await this.RoundtripAsync(first) : this.Roundtrip(first);

		Assert.NotNull(deserializedRoot);
		Assert.Same(deserializedRoot, deserializedRoot.Second?.First);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_OrderRequirementNotSatisfied(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		CyclicMustBeFirst first = new();
		CyclicMustBeSecond second = new() { First = first };
		first.Second = second;

		// Arrange to serialize "second"... first.
		// This means that upon deserialization, we must either:
		// 1. Fail, because Second demands a First be given during construction, OR
		// 2. Succeed, by bending the language semantics slightly, by temporarily assigning null to Second.First, a non-nullable and required property.
		// FWIW STJ succeeds, by noticing the ordering requirement *during serialization* and serializes a different root from the one given.
		// At the moment, we're going with the Fail option.
		if (async)
		{
			await Assert.ThrowsAsync<MessagePackSerializationException>(async () => await this.RoundtripAsync(second));
		}
		else
		{
			Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(second));
		}

		////CyclicMustBeSecond? deserializedRoot = this.Roundtrip(second);

		////Assert.NotNull(deserializedRoot);
		////Assert.Same(deserializedRoot, deserializedRoot.First.Second);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_DoublyLinkedList(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		DoublyLinkedListNode? a = new();
		DoublyLinkedListNode? b = new();
		DoublyLinkedListNode? c = new();

		a.Next = b;
		b.Next = c;

		b.Previous = a;
		c.Previous = b;

		a = async ? await this.RoundtripAsync(a) : this.Roundtrip(a);
		b = a?.Next;
		c = b?.Next;

		Assert.NotNull(a);
		Assert.NotNull(b);
		Assert.NotNull(c);

		Assert.Null(a.Previous);
		Assert.Same(b, a.Next);

		Assert.Same(c, b.Next);
		Assert.Same(a, b.Previous);

		Assert.Null(c.Next);
		Assert.Same(b, c.Previous);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_InvolvingStruct(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };
		ObjectWithStructField obj = new();
		obj.Struct = new StructWithObjectField { Object = obj };

		ObjectWithStructField? deserialized = async ? await this.RoundtripAsync(obj) : this.Roundtrip(obj);

		Assert.NotNull(deserialized);
		Assert.Same(deserialized, deserialized.Struct.Object);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_ManyTypes(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };

		MultiLinkTypeObject obj = new();
		obj.List = [obj];
		obj.ImmutableList = obj.List.ToImmutableList();
		obj.Array = [obj];
		obj.Enumerable = [obj];
		obj.Dictionary = new() { ["key"] = obj };
		obj.ImmutableDictionary = obj.Dictionary.ToImmutableDictionary();
		obj.Optional = (obj, true);

		MultiLinkTypeObject? deserialized = async ? await this.RoundtripAsync(obj) : this.Roundtrip(obj);
		Assert.NotNull(deserialized);

		Assert.Same(deserialized, deserialized.List?[0]);
		Assert.Same(deserialized, deserialized.ImmutableList?[0]);
		Assert.Same(deserialized, deserialized.Array?[0]);
		Assert.Same(deserialized, deserialized.Enumerable?.First());
		Assert.Same(deserialized, deserialized.Dictionary?["key"]);
		Assert.Same(deserialized, deserialized.ImmutableDictionary?["key"]);
		Assert.Same(deserialized, deserialized.Optional?.Item1);
	}

	[Theory, PairwiseData]
	public async Task CyclicReference_Union(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.AllowCycles };

		CyclicUnionBase obj = new CyclicUnionDerived();
		obj.Next = obj;

		CyclicUnionBase? deserialized = async ? await this.RoundtripAsync(obj) : this.Roundtrip(obj);
		Assert.NotNull(deserialized);

		Assert.Same(deserialized, deserialized.Next);
	}

	public struct StructWithObjectField
	{
		public ObjectWithStructField? Object { get; set; }
	}

	[GenerateShape]
	public partial class ObjectWithStructField
	{
		public ObjectWithStructField()
		{
		}

		public StructWithObjectField Struct { get; set; }
	}

	[GenerateShape]
	public partial class SinglyLinkedListNode
	{
		public SinglyLinkedListNode? Next { get; set; }
	}

	[GenerateShape]
	public partial class DoublyLinkedListNode
	{
		public DoublyLinkedListNode? Previous { get; set; }

		public DoublyLinkedListNode? Next { get; set; }
	}

	[GenerateShape]
	public partial class CyclicMustBeFirst
	{
		// This property is nullable and not required.
		// The declaring class is therefore a good candidate to be activated first.
		public CyclicMustBeSecond? Second { get; set; }
	}

	[GenerateShape]
	public partial class CyclicMustBeSecond
	{
		// This property is required and non-nullable.
		// The declaring class therefore requires that the other class be constructed first
		// if we are to fully honor language semantics.
		public required CyclicMustBeFirst First { get; init; }
	}

	[GenerateShape]
	public partial class SinglyLinkedListNodeWithKeyAttribute
	{
		[Key(0)]
		public SinglyLinkedListNodeWithKeyAttribute? Next { get; set; }
	}

	[GenerateShape]
	public partial class MultiLinkTypeObject
	{
		public List<MultiLinkTypeObject>? List { get; set; }

		public ImmutableList<MultiLinkTypeObject>? ImmutableList { get; set; }

		public MultiLinkTypeObject[]? Array { get; set; }

		public IEnumerable<MultiLinkTypeObject>? Enumerable { get; set; }

		public Dictionary<string, MultiLinkTypeObject>? Dictionary { get; set; }

		public ImmutableDictionary<string, MultiLinkTypeObject>? ImmutableDictionary { get; set; }

		public (MultiLinkTypeObject, bool)? Optional { get; set; }
	}

	[GenerateShape]
	[DerivedTypeShape(typeof(CyclicUnionDerived))]
	public partial class CyclicUnionBase
	{
		public CyclicUnionBase? Next { get; set; }
	}

	public class CyclicUnionDerived : CyclicUnionBase;

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
	[DerivedTypeShape(typeof(DerivedRecordA), Tag = 1)]
	public partial record BaseRecord;

	[GenerateShape]
	public partial record DerivedRecordA : BaseRecord;

	[GenerateShape]
	public partial record DerivedRecordB : BaseRecord;

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
	[GenerateShape<string[]>]
	[GenerateShape<BaseRecord[]>]
	[GenerateShape<Dictionary<string, int>[]>]
	private partial class Witness;
}
