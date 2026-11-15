// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json.Nodes;

#pragma warning disable SA1201 // Nested fixtures stay with the tests that use them.

public partial class UnionDiscriminatorTests : MessagePackSerializerTestBase
{
	[Test]
	public void Member_IsWrittenFirst_OptionalOnRead_AndCheckedWhenPresent()
	{
		KindFirst? value = this.Roundtrip(new KindFirst { Uri = "file:///a.cs" });
		Assert.Equal("file:///a.cs", value!.Uri);
		Assert.Equal("""{"Kind":"create","Uri":"file:///a.cs"}""", this.Json());

		KindFirst? missing = this.Read<KindFirst>(this.Map(("Uri", "file:///a.cs")));
		Assert.Equal("file:///a.cs", missing!.Uri);
		Assert.Equal("create", missing.Kind);

		KindFirst? reordered = this.Read<KindFirst>(this.Map(("Uri", "file:///a.cs"), ("Kind", "create")));
		Assert.Equal("file:///a.cs", reordered!.Uri);

		MessagePackSerializationException mismatch = Assert.Throws<MessagePackSerializationException>(() => this.Read<KindFirst>(this.Map(("Kind", "delete"), ("Uri", "file:///a.cs"))));
		Assert.Equal(MessagePackSerializationException.ErrorCode.DiscriminatorMismatch, mismatch.Code);

		MessagePackSerializationException duplicate = Assert.Throws<MessagePackSerializationException>(() => this.Read<KindFirst>(this.Map(("Kind", "create"), ("Kind", "create"))));
		Assert.Equal(MessagePackSerializationException.ErrorCode.DoublePropertyAssignment, duplicate.Code);

		MessagePackSerializationException nil = Assert.Throws<MessagePackSerializationException>(() => this.Read<KindFirst>(this.MapWithNil("Kind")));
		Assert.Equal(MessagePackSerializationException.ErrorCode.DiscriminatorMismatch, nil.Code);
	}

	[Test]
	public void TypeLevelName_IsNotRenamed()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		TypeLevel? value = this.Roundtrip(new TypeLevel { Uri = "file:///a.cs" });
		Assert.Equal("file:///a.cs", value!.Uri);
		Assert.Equal("""{"Kind":"create","uri":"file:///a.cs"}""", this.Json());
	}

	[Test]
	public void BoolIntAndEnum_RoundTripByNumberAndName()
	{
		Assert.Equal("a", this.Roundtrip(new BoolCase { Name = "a" })!.Name);
		Assert.Equal("""{"Flag":false,"Name":"a"}""", this.Json());
		Assert.Equal(0, this.Roundtrip(new IntCase())!.Code);
		Assert.Equal("""{"Code":0}""", this.Json());
		Assert.Equal(Color.Red, this.Roundtrip(new EnumNumber())!.Color);
		Assert.Equal("""{"Color":1}""", this.Json());

		MessagePackSerializer named = new() { SerializeEnumValuesByName = true };
		byte[] msgpack = named.Serialize(new EnumNumber(), this.Shape<EnumNumber>());
		Assert.Equal("""{"Color":"Red"}""", named.ConvertToJson(msgpack));
		MessagePackReader reader = new(msgpack);
		Assert.Equal(Color.Red, named.Deserialize(ref reader, this.Shape<EnumNumber>())!.Color);
		byte[] mismatchBytes = this.Map(("Color", "Blue"));
		MessagePackSerializationException mismatch = Assert.Throws<MessagePackSerializationException>(() =>
		{
			MessagePackReader mismatchReader = new(mismatchBytes);
			return named.Deserialize(ref mismatchReader, this.Shape<EnumNumber>());
		});
		Assert.Equal(MessagePackSerializationException.ErrorCode.DiscriminatorMismatch, mismatch.Code);
	}

	[Test]
	public void DefaultOmission_StillWritesTheDiscriminator()
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never };
		this.Roundtrip(new IntCase());
		Assert.Equal("""{"Code":0}""", this.Json());
		this.Roundtrip(new BoolCase { Name = "a" });
		Assert.Equal("""{"Flag":false,"Name":"a"}""", this.Json());
	}

	[Test]
	public void ConstField_IsOmittedBecauseTheShapeExcludesIt()
	{
		this.Roundtrip(new ConstFieldCase { Uri = "file:///a.cs" });
		Assert.Equal("""{"Uri":"file:///a.cs"}""", this.Json());
	}

	[Test]
	public void PropertyShapeName_IsTheSerializedName()
	{
		this.Roundtrip(new Renamed { Uri = "file:///a.cs" });
		Assert.Equal("""{"op":"create","Uri":"file:///a.cs"}""", this.Json());
	}

	[Test]
	public async Task AsyncRoundTrip_PreservesTheDiscriminator()
	{
		KindFirst? value = await this.RoundtripAsync(new KindFirst { Uri = "file:///a.cs" });
		Assert.Equal("file:///a.cs", value!.Uri);
		Assert.Equal("""{"Kind":"create","Uri":"file:///a.cs"}""", this.Json());
	}

	[Test]
	public void Union_WritesTagThenDiscriminator()
	{
		this.Roundtrip<FileChange>(new CreateCase("file:///a.cs"));
		Assert.Equal("""["CreateCase",{"Kind":"create","Uri":"file:///a.cs"}]""", this.Json());
		this.Roundtrip<FileChange>(new EditCase("file:///a.cs"));
		Assert.Equal("""["EditCase",{"Uri":"file:///a.cs"}]""", this.Json());
	}

	[Test]
	public void Schema_ListsDiscriminatorAsOptionalConst()
	{
		JsonObject schema = ObjectSchema(this.Serializer.GetJsonSchema(this.Shape<KindFirst>()));
		Assert.Equal("\"create\"", schema["properties"]!["Kind"]!["const"]!.ToJsonString());
		Assert.Null(schema["required"]);

		JsonObject draft4 = ObjectSchema(this.Serializer.GetJsonSchema(this.Shape<KindFirst>(), new JsonSchemaOptions { Dialect = JsonSchemaDialect.Draft4 }));
		Assert.Equal("[\"create\"]", draft4["properties"]!["Kind"]!["enum"]!.ToJsonString());
		Assert.Null(draft4["const"]);

		JsonObject union = this.Serializer.GetJsonSchema(this.Shape<FileChange>());
		Assert.Contains("\"const\":\"create\"", union.ToJsonString(), StringComparison.Ordinal);

		JsonObject keyed = ObjectSchema(this.Serializer.GetJsonSchema(this.Shape<KeyedCreate>()));
		Assert.Equal("\"create\"", keyed["properties"]!["0"]!["const"]!.ToJsonString());
		Assert.Equal("\"create\"", keyed["prefixItems"]![0]!["const"]!.ToJsonString());
		Assert.Null(keyed["required"]);
		Assert.Null(keyed["minItems"]);
	}

	[Test]
	public void KeyedLayout_WritesAndChecksTheIndex()
	{
		KeyedCreate? value = this.Roundtrip(new KeyedCreate { Uri = "file:///a.cs" });
		Assert.Equal("file:///a.cs", value!.Uri);
		Assert.Equal("""["create","file:///a.cs"]""", this.Json());

		KeyedLater? later = this.Roundtrip(new KeyedLater("file:///a.cs"));
		Assert.Equal("file:///a.cs", later!.Uri);
		Assert.Equal("""["file:///a.cs","create"]""", this.Json());

		KeyedLater? absent = this.Read<KeyedLater>(this.Array("file:///a.cs"));
		Assert.Equal("file:///a.cs", absent!.Uri);
		Assert.Equal("create", absent.Kind);

		MessagePackSerializationException mismatch = Assert.Throws<MessagePackSerializationException>(() => this.Read<KeyedLater>(this.Array("file:///a.cs", "delete")));
		Assert.Equal(MessagePackSerializationException.ErrorCode.DiscriminatorMismatch, mismatch.Code);

		KeyedLater? fromMap = this.Read<KeyedLater>(this.IntMap((0, "file:///a.cs"), (1, "create")));
		Assert.Equal("file:///a.cs", fromMap!.Uri);
	}

	[Test]
	public void InvalidDeclarations_FailWhenTheConverterIsBuilt()
	{
		this.AssertNotSupported<GetterMismatch>("instead of");
		this.AssertNotSupported<SetterCase>("setter");
		this.AssertNotSupported<TwoDiscriminators>("more than one");
		this.AssertNotSupported<MixedNames>("same discriminator name");
		this.AssertNotSupported<DuplicateValues>("more than one case");
		this.AssertNotSupported<NameCollision>("already has a property");
		this.AssertNotSupported<NullConstant>("null");
		this.AssertNotSupported<MissingKey>("must have Key");
		this.AssertNotSupported<TypeLevelKeyed>("must be a member");
		this.AssertNotSupported<DifferentIndexes>("same index");
	}

	[Test]
	public void SameIndex_DistinctValues_AreAccepted()
	{
		this.Roundtrip<IndexedChange>(new IndexedCreate("file:///a.cs"));
		Assert.Equal("""["IndexedCreate",["create","file:///a.cs"]]""", this.Json());
	}

	[Test]
	public void UnsignedValue_SurvivesSchemaAndRoundTrip()
	{
		Assert.Equal(ulong.MaxValue, this.Roundtrip(new UnsignedCase())!.Code);
		JsonObject schema = ObjectSchema(this.Serializer.GetJsonSchema(this.Shape<UnsignedCase>()));
		Assert.Equal("18446744073709551615", schema["properties"]!["Code"]!["const"]!.ToJsonString());
	}

	[Test]
	public void ProbeInstance_DoesNotFinalizeUninitializedState()
	{
		_ = this.Serializer.GetJsonSchema(this.Shape<FinalizerProbe>());
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		Assert.Equal(0, FinalizerProbe.UninitializedFinalizations);
	}

	private static JsonObject ObjectSchema(JsonObject schema)
	{
		if (schema["properties"] is not null || schema["prefixItems"] is not null || schema["items"] is not null)
		{
			return schema;
		}

		JsonObject definitions = (JsonObject)(schema["$defs"] ?? schema["definitions"])!;
		return definitions.Select(pair => pair.Value).OfType<JsonObject>().First(candidate => candidate["properties"] is not null || candidate["prefixItems"] is not null || candidate["items"] is not null);
	}

	private string Json() => this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack);

	private T? Read<T>(byte[] msgpack)
#if NET
		where T : IShapeable<T>
#endif
	{
		MessagePackReader reader = new(msgpack);
		return this.Serializer.Deserialize(ref reader, this.Shape<T>(), this.TimeoutToken);
	}

	private ITypeShape<T> Shape<T>()
#if NET
		where T : IShapeable<T>
		=> T.GetTypeShape();
#else
		=> TypeShapeResolver.ResolveDynamicOrThrow<T>();
#endif

	private void AssertNotSupported<T>(string fragment)
#if NET
		where T : IShapeable<T>
#endif
	{
		Exception error = Assert.ThrowsAny<Exception>(() => this.Serializer.GetJsonSchema(this.Shape<T>()));
		NotSupportedException? notSupported = error as NotSupportedException ?? error.InnerException as NotSupportedException;
		Assert.IsType<NotSupportedException>(notSupported);
		Assert.Contains(fragment, notSupported.Message, StringComparison.OrdinalIgnoreCase);
	}

	private byte[] Map(params (string Name, string Value)[] properties)
	{
		ArrayBufferWriter<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		writer.WriteMapHeader(properties.Length);
		foreach ((string name, string value) in properties)
		{
			writer.Write(name);
			writer.Write(value);
		}

		writer.Flush();
		return buffer.WrittenSpan.ToArray();
	}

	private byte[] MapWithNil(string name)
	{
		ArrayBufferWriter<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		writer.WriteMapHeader(1);
		writer.Write(name);
		writer.WriteNil();
		writer.Flush();
		return buffer.WrittenSpan.ToArray();
	}

	private byte[] Array(params string[] values)
	{
		ArrayBufferWriter<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		writer.WriteArrayHeader(values.Length);
		foreach (string value in values)
		{
			writer.Write(value);
		}

		writer.Flush();
		return buffer.WrittenSpan.ToArray();
	}

	private byte[] IntMap(params (int Key, string Value)[] entries)
	{
		ArrayBufferWriter<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		writer.WriteMapHeader(entries.Length);
		foreach ((int key, string value) in entries)
		{
			writer.Write(key);
			writer.Write(value);
		}

		writer.Flush();
		return buffer.WrittenSpan.ToArray();
	}

	[GenerateShape]
	public partial record KindFirst
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";

		public string? Uri { get; set; }
	}

	[GenerateShape]
	[UnionDiscriminator("Kind", "create")]
	public partial record TypeLevel
	{
		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record BoolCase
	{
		[UnionDiscriminator(false)]
		public bool Flag => false;

		public string? Name { get; set; }
	}

	[GenerateShape]
	public partial record IntCase
	{
		[UnionDiscriminator(0)]
		public int Code => 0;
	}

	[GenerateShape]
	public partial record EnumNumber
	{
		[UnionDiscriminator(Color.Red)]
		public Color Color => Color.Red;
	}

	[GenerateShape]
	public partial record UnsignedCase
	{
		[UnionDiscriminator(ulong.MaxValue)]
		public ulong Code => ulong.MaxValue;
	}

	[GenerateShape]
	public partial record ConstFieldCase
	{
#pragma warning disable NBMsgPack131 // A constant field is intentionally outside the object shape.
		[UnionDiscriminator("create")]
		public const string Kind = "create";
#pragma warning restore NBMsgPack131

		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record Renamed
	{
		[PropertyShape(Name = "op"), UnionDiscriminator("create")]
		public string Kind => "create";

		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record KeyedCreate
	{
		[Key(0), UnionDiscriminator("create")]
		public string Kind => "create";

		[Key(1)]
		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record KeyedLater([property: Key(0)] string Uri)
	{
#pragma warning disable NBMsgPack134 // Key(0) is recommended, not required.
		[Key(1), UnionDiscriminator("create")]
		public string Kind => "create";
#pragma warning restore NBMsgPack134
	}

	[GenerateShape, DerivedTypeShape(typeof(CreateCase)), DerivedTypeShape(typeof(EditCase))]
	public abstract partial record FileChange;

	[GenerateShape]
	public partial record CreateCase(string Uri) : FileChange
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";
	}

	[GenerateShape]
	public partial record EditCase(string Uri) : FileChange;

	[GenerateShape, DerivedTypeShape(typeof(IndexedCreate)), DerivedTypeShape(typeof(IndexedDelete))]
	public abstract partial record IndexedChange;

	[GenerateShape]
	public partial record IndexedCreate([property: Key(1)] string Uri) : IndexedChange
	{
		[Key(0), UnionDiscriminator("create")]
		public string Kind => "create";
	}

	[GenerateShape]
	public partial record IndexedDelete([property: Key(1)] string Uri) : IndexedChange
	{
		[Key(0), UnionDiscriminator("delete")]
		public string Kind => "delete";
	}

#pragma warning disable NBMsgPack130
	[GenerateShape]
	public partial record GetterMismatch
	{
		[UnionDiscriminator("delete")]
		public string Kind => "create";
	}
#pragma warning restore NBMsgPack130

#pragma warning disable NBMsgPack131, NBMsgPack110
	[GenerateShape]
	public partial record SetterCase
	{
		[UnionDiscriminator("create")]
		public string Kind { get; set; } = "create";
	}

	[GenerateShape]
	public partial record TwoDiscriminators
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";

		[UnionDiscriminator("other")]
		public string Other => "other";
	}
#pragma warning restore NBMsgPack131

#pragma warning disable NBMsgPack133
	[GenerateShape, DerivedTypeShape(typeof(NamedCreate)), DerivedTypeShape(typeof(NamedDelete))]
	public abstract partial record MixedNames;

	[GenerateShape]
	public partial record NamedCreate : MixedNames
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";
	}

	[GenerateShape]
	public partial record NamedDelete : MixedNames
	{
		[UnionDiscriminator("remove")]
		public string Action => "remove";
	}

	[GenerateShape, DerivedTypeShape(typeof(DuplicateCreate)), DerivedTypeShape(typeof(DuplicateDelete))]
	public abstract partial record DuplicateValues;

	[GenerateShape]
	public partial record DuplicateCreate : DuplicateValues
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";
	}

	[GenerateShape]
	public partial record DuplicateDelete : DuplicateValues
	{
		[UnionDiscriminator("create")]
		public string Kind => "create";
	}
#pragma warning restore NBMsgPack133

#pragma warning disable NBMsgPack131, NBMsgPack132, NBMsgPack001, NBMsgPack110
	[GenerateShape]
	[UnionDiscriminator("Uri", "create")]
	public partial record NameCollision
	{
		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record NullConstant
	{
		[UnionDiscriminator(null)]
		public string? Kind => null;
	}

	[GenerateShape]
	public partial record MissingKey
	{
		[Key(0)]
		public string? Uri { get; set; }

		[UnionDiscriminator("create")]
		public string Kind => "create";
	}

	[GenerateShape, UnionDiscriminator("Kind", "create")]
	public partial record TypeLevelKeyed
	{
		[Key(0)]
		public string? Uri { get; set; }
	}
#pragma warning restore NBMsgPack131, NBMsgPack132, NBMsgPack001, NBMsgPack110

#pragma warning disable NBMsgPack133, NBMsgPack134
	[GenerateShape, DerivedTypeShape(typeof(IndexZero)), DerivedTypeShape(typeof(IndexOne))]
	public abstract partial record DifferentIndexes;

	[GenerateShape]
	public partial record IndexZero : DifferentIndexes
	{
		[Key(0), UnionDiscriminator("create")]
		public string Kind => "create";

		[Key(1)]
		public string? Uri { get; set; }
	}

	[GenerateShape]
	public partial record IndexOne : DifferentIndexes
	{
		[Key(0)]
		public string? Uri { get; set; }

		[Key(1), UnionDiscriminator("delete")]
		public string Kind => "delete";
	}
#pragma warning restore NBMsgPack133, NBMsgPack134

	[GenerateShape]
	public partial class FinalizerProbe
	{
		internal static int UninitializedFinalizations;

		private readonly object? resource = new();

		[UnionDiscriminator("create")]
		public string Kind => "create";

		~FinalizerProbe()
		{
			if (this.resource is null)
			{
				Interlocked.Increment(ref UninitializedFinalizations);
			}
		}
	}

	public enum Color
	{
		Red = 1,
		Blue = 2,
	}
}
