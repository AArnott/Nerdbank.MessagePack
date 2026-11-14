// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable DuckTyping // Experimental API

using System.Buffers;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

public partial class DerivedTypeDuckTypingTests
{
	public enum PlainColor
	{
		Red,
		Blue,
	}

	[GenerateShape]
	public partial interface IPet;

	[Test]
	public async Task RequiredPropertyDistinction_RoundtripAsync()
	{
		await this.AssertRoundtripAsync<Animal>(new Dog("Buddy", 5));
		await this.AssertRoundtripAsync<Animal>(new Cat("Whiskers", 3));
	}

	[Test]
	public void Constructor_RejectsNullBase()
	{
		Assert.Throws<ArgumentNullException>(() => new DerivedTypeDuckTyping(null!));
	}

	[Test]
	public void Constructor_RejectsNullDerivedShape()
	{
		ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<Dog>(), null!));
		Assert.Equal("derivedTypeShapes", ex.ParamName);
	}

	[Test]
	public void Constructor_RejectsFewerThanTwoConcreteTypes()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<Dog>()));
		Assert.Contains("At least two", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsDuplicateTypes()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<Dog>(), this.Shape<Dog>()));
		Assert.Contains("more than once", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsAbstractDerivedType()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<AbstractPet>(), this.Shape<Dog>()));
		Assert.Contains("must be concrete", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsInterfaceDerivedType()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<IPet>(), this.Shape<Dog>()));
		Assert.Contains("must be concrete", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsTypeNotAssignableToBase()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<Dog>(), this.Shape<Drawing>()));
		Assert.Contains("not assignable", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsNonObjectCase()
	{
#if NET
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(
			TypeShapeResolver.Resolve<object, ClassificationWitness>(),
			TypeShapeResolver.Resolve<PlainColor, ClassificationWitness>(),
			this.Shape<Circle>()));
#else
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(
			TypeShapeResolver.ResolveDynamicOrThrow<object>(),
			TypeShapeResolver.ResolveDynamicOrThrow<PlainColor>(),
			this.Shape<Circle>()));
#endif
		Assert.Contains("object types", ex.Message, StringComparison.Ordinal);
		Assert.Contains(nameof(PlainColor), ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsTwoCatchAlls()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<IdenticalTypeBase>(), this.Shape<IdenticalType1>()));
		Assert.Contains("no required properties", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_RejectsIdenticalRequiredNames()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<PairBase>(), this.Shape<LeftTwin>(), this.Shape<RightTwin>()));
		Assert.Contains("same properties", ex.Message, StringComparison.Ordinal);
		Assert.Contains(nameof(LeftTwin), ex.Message, StringComparison.Ordinal);
		Assert.Contains(nameof(RightTwin), ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void Constructor_AllowsConcreteCatchAllBase()
	{
		DerivedTypeDuckTyping union = new(this.Shape<Drawing>(), this.Shape<Circle>());
		Assert.Equal(typeof(Drawing), union.BaseType);
	}

	[Test]
	public void AttributedHierarchyIsDuckTypedThroughItsObjectShape()
	{
		this.Use(this.Shape<TaggedAnimal>(), this.Shape<TaggedDog>());
		Assert.Equal(new TaggedAnimal("Ada"), this.Deserialize<TaggedAnimal>(this.Map(("Name", "Ada"))));
		Assert.Equal(new TaggedDog("Rex", 3), this.Deserialize<TaggedAnimal>(this.Map(("Name", "Rex"), ("BarkVolume", 3))));
	}

	[Test]
	public void AttributedHierarchySchemaUsesTheObjectView()
	{
		this.Use(this.Shape<TaggedAnimal>(), this.Shape<TaggedDog>());
		JsonObject schema = this.Serializer.GetJsonSchema(this.Shape<TaggedAnimal>());
		Assert.Contains("oneOf", schema.ToJsonString(), StringComparison.Ordinal);
	}

	[Test]
	public void SchemaSelectsTheMostSpecificConcreteCase()
	{
		this.Use(this.Shape<Edit>(), this.Shape<TextEdit>(), this.Shape<AnnotatedEdit>());
		JsonObject schema = this.Serializer.GetJsonSchema(this.Shape<Edit>());
		JsonArray branches = this.UnionBranches(schema);
		Assert.Equal(2, branches.Count);
		Assert.Contains("\"not\"", branches.ToJsonString(), StringComparison.Ordinal);

		JSchema parsed = JSchema.Parse(schema.ToJsonString());
		Assert.True(this.PayloadMatches(parsed, """{"Range":"r","NewText":"n"}"""));
		Assert.True(this.PayloadMatches(parsed, """{"Range":"r","NewText":"n","AnnotationId":"a"}"""));
		Assert.False(this.PayloadMatches(parsed, "{}"));
	}

	[Test]
	public void SchemaRejectsAmbiguousPayloadsAndOmitsAbstractBases()
	{
		this.Use(this.Shape<Creature>(), this.Shape<VolumeDog>(), this.Shape<VolumeCat>());
		JsonObject schema = this.Serializer.GetJsonSchema(this.Shape<Creature>());
		Assert.Equal(2, this.UnionBranches(schema).Count);

		JSchema parsed = JSchema.Parse(schema.ToJsonString());
		Assert.True(this.PayloadMatches(parsed, """{"BarkVolume":1}"""));

		// VolumeCat's constructor parameter does not match its property, so the reader requires the parameter name.
		Assert.True(this.PayloadMatches(parsed, """{"pitch":1}"""));
		Assert.False(this.PayloadMatches(parsed, """{"MeowPitch":2}"""));
	}

	[Test]
	public void SchemaRejectsPayloadsThatMatchIncomparableCases()
	{
		this.Use(this.Shape<Animal2>(), this.Shape<Cat2>(), this.Shape<Dog2>());
		JSchema parsed = JSchema.Parse(this.Serializer.GetJsonSchema(this.Shape<Animal2>()).ToJsonString());
		Assert.True(this.PayloadMatches(parsed, """{"Name":"Ada"}"""));
		Assert.True(this.PayloadMatches(parsed, """{"Name":"Ada","Lives":9}"""));
		Assert.True(this.PayloadMatches(parsed, """{"Name":"Ada","Breed":"collie"}"""));
		Assert.False(this.PayloadMatches(parsed, """{"Name":"Ada","Lives":9,"Breed":"collie"}"""));

		// Lives is not an integer, so the cat object schema fails, but both cases still match by name.
		Assert.False(this.PayloadMatches(parsed, """{"Name":"Ada","Lives":"nine","Breed":"collie"}"""));
	}

	[Test]
	public void SchemaUsesTheCatchAllOnlyWhenNothingElseMatches()
	{
		this.Use(this.Shape<Drawing>(), this.Shape<Circle>());
		JSchema parsed = JSchema.Parse(this.Serializer.GetJsonSchema(this.Shape<Drawing>()).ToJsonString());
		Assert.True(this.PayloadMatches(parsed, "{}"));
		Assert.True(this.PayloadMatches(parsed, """{"Radius":1}"""));
		Assert.True(this.PayloadMatches(parsed, """{"Radius":1,"Extra":true}"""));
	}

	[Test]
	public void TaggedAndDuckTypedSchemasStayDistinct()
	{
		// Duck is declared first so its schema is built before the tagged horse. A type-keyed cache would then
		// reuse the plain horse map for the tagged property.
		this.Use(this.Shape<NestedAnimal>(), this.Shape<NestedHorse>(), this.Shape<NestedQuarterHorse>());
		JsonObject schema = this.Serializer.GetJsonSchema(this.Shape<TaggedAndDuck>());
		JsonObject definitions = Assert.IsType<JsonObject>(schema["$defs"]);
		JsonObject model = Assert.IsType<JsonObject>(definitions.First(definition => definition.Key.Contains(nameof(TaggedAndDuck), StringComparison.Ordinal)).Value);
		JsonObject properties = Assert.IsType<JsonObject>(model["properties"]);
		string duck = Assert.IsType<JsonObject>(properties["Duck"]).ToJsonString();
		string tagged = Assert.IsType<JsonObject>(properties["Tagged"]).ToJsonString();
		Assert.Contains("\"Speed\"", duck, StringComparison.Ordinal);
		Assert.DoesNotContain("prefixItems", duck, StringComparison.Ordinal);
		Assert.Contains("prefixItems", tagged, StringComparison.Ordinal);
	}

	[Test]
	public void DerivedCaseWithItsOwnHierarchyRoundtripsAsAMap()
	{
		this.Use(this.Shape<NestedAnimal>(), this.Shape<NestedHorse>(), this.Shape<NestedQuarterHorse>());
		NestedHorse horse = new("Spirit", 20);
		byte[] horseBytes = this.Serializer.Serialize<NestedAnimal>(horse, this.TimeoutToken);
		Assert.StartsWith("{", this.Serializer.ConvertToJson(horseBytes), StringComparison.Ordinal);
		Assert.Equal(horse, this.Deserialize<NestedAnimal>(horseBytes));

		NestedQuarterHorse quarterHorse = new("Spirit", 20, "bay");
		byte[] quarterBytes = this.Serializer.Serialize<NestedAnimal>(quarterHorse, this.TimeoutToken);
		Assert.StartsWith("{", this.Serializer.ConvertToJson(quarterBytes), StringComparison.Ordinal);
		Assert.Equal(quarterHorse, this.Deserialize<NestedAnimal>(quarterBytes));
	}

	[Test]
	public void RequiredNamesContainingNulStayDistinct()
	{
		this.Use(this.Shape<NulBase>(), this.Shape<NulJoined>(), this.Shape<NulSplit>());
		Assert.Equal(new NulJoined(1), this.Deserialize<NulBase>(this.Map(("A\0B", 1))));
		Assert.Equal(new NulSplit(1, 2), this.Deserialize<NulBase>(this.Map(("A", 1), ("B", 2))));
	}

	[Test]
	public void Constructor_RejectsTypesThatDifferOnlyByAnOptionalMember()
	{
		// A parameter with a default is not required, so these cases have the same required name.
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new DerivedTypeDuckTyping(this.Shape<Animal>(), this.Shape<OptionalNameOnly>(), this.Shape<OptionalWithDefault>()));
		Assert.Contains("same properties", ex.Message, StringComparison.Ordinal);
	}

	[Test]
	public void MatchesSerializedPropertyNameRatherThanConstructorParameterName()
	{
		this.Use(this.Shape<Creature>(), this.Shape<VolumeDog>(), this.Shape<VolumeCat>());
		VolumeDog dog = Assert.IsType<VolumeDog>(this.Deserialize<Creature>(this.Map(("BarkVolume", 5))));
		Assert.Equal(5, dog.BarkVolume);
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Creature>(this.Map(("barkVolume", 5))));
		Assert.Contains("does not match", this.FullMessage(ex), StringComparison.Ordinal);
	}

	[Test]
	public void ExplicitPropertyShapeNameIsNotRewrittenByNamingPolicy()
	{
		this.Use(this.Shape<Animal>(), this.Shape<Dog>(), this.Shape<RenamedCat>());
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		RenamedCat cat = Assert.IsType<RenamedCat>(this.Deserialize<Animal>(this.Map(("name", "Whiskers"), ("voice-level", 4))));
		Assert.Equal(4, cat.MeowPitch);
		Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Animal>(this.Map(("name", "Whiskers"), ("meowPitch", 4))));
	}

	[Test]
	public void CamelCasePolicyMatchesSerializedNames()
	{
		this.Use(this.Shape<Animal>(), this.Shape<Dog>(), this.Shape<Cat>());
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		Dog dog = Assert.IsType<Dog>(this.Deserialize<Animal>(this.Map(("name", "Buddy"), ("barkVolume", 5))));
		Assert.Equal(5, dog.BarkVolume);

		// MessagePack property names are ordinal. There is no case-insensitive comparer; case folding is only whatever a naming policy does.
		Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Animal>(this.Map(("name", "Buddy"), ("barkvolume", 5))));
	}

	[Test]
	public void RequiredKeywordAndIsRequiredAttributeAreEvidence()
	{
		this.Use(this.Shape<RequiredAnimalBase>(), this.Shape<RequiredPitchAnimal>(), this.Shape<FlaggedBark>());
		Assert.IsType<RequiredPitchAnimal>(this.Deserialize<RequiredAnimalBase>(this.Map(("MeowPitch", 3))));
		Assert.IsType<FlaggedBark>(this.Deserialize<RequiredAnimalBase>(this.Map(("BarkVolume", 8))));
		Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<RequiredAnimalBase>(this.Map(("Note", "hi"))));
	}

	[Test]
	public void MostSpecificCaseWinsAndUnknownPropertiesDoNotRejectIt()
	{
		this.Use(this.Shape<Edit>(), this.Shape<TextEdit>(), this.Shape<AnnotatedEdit>());
		Assert.Equal(new TextEdit("r", "n"), this.Deserialize<Edit>(this.Map(("Range", "r"), ("NewText", "n"))));
		Assert.Equal(new AnnotatedEdit("r", "n", "a"), this.Deserialize<Edit>(this.Map(("Range", "r"), ("NewText", "n"), ("AnnotationId", "a"))));
		Assert.Equal(new AnnotatedEdit("r", "n", "a"), this.Deserialize<Edit>(this.Map(("Range", "r"), ("NewText", "n"), ("AnnotationId", "a"), ("Extra", "x"))));
		Assert.Equal(new TextEdit("r", "n"), this.Deserialize<Edit>(this.Map(("Range", "r"), ("NewText", "n"), ("Extra", "x"))));
	}

	[Test]
	public void IncomparableCasesAreAmbiguous()
	{
		this.Use(this.Shape<Animal>(), this.Shape<Dog>(), this.Shape<Cat>());
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Animal>(this.Map(("Name", "x"), ("BarkVolume", 1), ("MeowPitch", 2))));
		string message = this.FullMessage(ex);
		Assert.Contains("more specific", message, StringComparison.Ordinal);
		Assert.Contains(nameof(Dog), message, StringComparison.Ordinal);
		Assert.Contains(nameof(Cat), message, StringComparison.Ordinal);
	}

	[Test]
	public void CatchAllIsUsedOnlyWhenNothingElseMatches()
	{
		this.Use(this.Shape<Drawing>(), this.Shape<Circle>());
		Assert.Equal(new Drawing(), this.Deserialize<Drawing>(this.Map()));
		Assert.Equal(new Drawing(), this.Deserialize<Drawing>(this.Map(("Unknown", 1))));
		Assert.Equal(new Circle(4), this.Deserialize<Drawing>(this.Map(("Radius", 4))));
		Assert.Equal(new Circle(4), this.Deserialize<Drawing>(this.Map(("Radius", 4), ("Unknown", 1))));
	}

	[Test]
	public void ConcreteBaseIsACaseAndUnregisteredRuntimeTypesThrow()
	{
		this.Use(this.Shape<Animal2>(), this.Shape<Cat2>());
		Assert.Equal(new Animal2("Ada"), this.Deserialize<Animal2>(this.Map(("Name", "Ada"))));
		Assert.Equal(new Cat2("Ada", 9), this.Deserialize<Animal2>(this.Map(("Name", "Ada"), ("Lives", 9))));
		Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Animal2>(this.Map(("Other", 1))));

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<Animal2>(new Dog2("Ada", "collie"), this.TimeoutToken));
		Assert.Contains("No duck-typing union case is registered", this.FullMessage(ex), StringComparison.Ordinal);
	}

	[Test]
	public void NonMapValuesDoNotMatchTheCatchAll()
	{
		this.Use(this.Shape<Drawing>(), this.Shape<Circle>());
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Deserialize<Drawing>(this.EmptyArray()));
		Assert.Contains("expects a map", this.FullMessage(ex), StringComparison.Ordinal);
		Assert.Null(this.Deserialize<Drawing>(this.Serializer.Serialize<Drawing>(null, this.TimeoutToken)));
	}

	[Test]
	public void GetterOnlyMemberIsNotEvidence()
	{
		this.Use(this.Shape<EvidenceBase>(), this.Shape<ComputedCatchAll>(), this.Shape<RealValue>());
		Assert.IsType<ComputedCatchAll>(this.Deserialize<EvidenceBase>(this.Map()));
		Assert.IsType<ComputedCatchAll>(this.Deserialize<EvidenceBase>(this.Map(("ComputedValue", 1))));
		Assert.Equal(new RealValue(2), this.Deserialize<EvidenceBase>(this.Map(("Value", 2))));
	}

	[Test]
	public void NamingPolicyCollisionFailsWhenTheConverterIsBuilt()
	{
		this.Use(this.Shape<ShapeBase>(), this.Shape<CircleShape>(), this.Shape<SquareShape>());
		this.Serializer = this.Serializer with { PropertyNamingPolicy = new CollapseNamingPolicy() };
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<ShapeBase>(new CircleShape(3), this.TimeoutToken));
		Assert.Contains("same properties", this.FullMessage(ex), StringComparison.Ordinal);
	}

	[Test]
	public void KeyedLayoutsAreRejected()
	{
		this.Use(this.Shape<KeyedBase>(), this.Shape<KeyedDog>(), this.Shape<KeyedCat>());
		MessagePackSerializationException keyed = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<KeyedBase>(new KeyedDog { BarkVolume = 1 }, this.TimeoutToken));
		Assert.Contains(nameof(KeyAttribute), this.FullMessage(keyed), StringComparison.Ordinal);
	}

	[Test]
	public void PerfOverSchemaStabilityIsRejected()
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = true };
		MessagePackSerializationException arrays = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize<Animal>(new Dog("Buddy", 5), this.TimeoutToken));
		Assert.Contains("PerfOverSchemaStability", this.FullMessage(arrays), StringComparison.Ordinal);
	}

#if NET
	private ITypeShape Shape<T>()
		where T : IShapeable<T>
		=> TypeShapeResolver.Resolve<T>();
#else
	private ITypeShape Shape<T>() => TypeShapeResolver.ResolveDynamicOrThrow<T>();
#endif

	private void Use(ITypeShape baseShape, params ITypeShape[] derived)
	{
		this.Serializer = this.Serializer with
		{
			DerivedTypeUnions = [new DerivedTypeDuckTyping(baseShape, derived)],
		};
	}

	private T? Deserialize<T>(byte[] msgpack)
#if NET
		where T : IShapeable<T>
#endif
		=> this.Serializer.Deserialize<T>(msgpack, this.TimeoutToken);

	private string FullMessage(Exception exception) => GetFullMessage(exception);

	private JsonArray UnionBranches(JsonObject schema)
		=> this.FindOneOf(schema) ?? throw new InvalidOperationException("The duck-typing schema has no oneOf branches: " + schema.ToJsonString());

	private JsonArray? FindOneOf(JsonNode? node)
	{
		if (node is JsonObject obj)
		{
			if (obj["oneOf"] is JsonArray oneOf)
			{
				return oneOf;
			}

			foreach (KeyValuePair<string, JsonNode?> property in obj)
			{
				JsonArray? found = this.FindOneOf(property.Value);
				if (found is not null)
				{
					return found;
				}
			}
		}
		else if (node is JsonArray array)
		{
			foreach (JsonNode? item in array)
			{
				JsonArray? found = this.FindOneOf(item);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	private bool PayloadMatches(JSchema schema, string json)
		=> JObject.Parse(json).IsValid(schema);

	private byte[] Map(params (string Key, object? Value)[] properties)
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(properties.Length);
		foreach ((string key, object? value) in properties)
		{
			writer.Write(key);
			switch (value)
			{
				case null:
					writer.WriteNil();
					break;
				case int number:
					writer.Write(number);
					break;
				case string text:
					writer.Write(text);
					break;
				default:
					throw new ArgumentException($"Unsupported test value type {value.GetType()}.", nameof(properties));
			}
		}

		writer.Flush();
		return sequence.AsReadOnlySequence.ToArray();
	}

	private byte[] EmptyArray()
	{
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(0);
		writer.Flush();
		return sequence.AsReadOnlySequence.ToArray();
	}

	[GenerateShape]
	public abstract partial class Creature;

	[GenerateShape]
	public partial class VolumeDog : Creature
	{
		public VolumeDog(int barkVolume) => this.BarkVolume = barkVolume;

		public int BarkVolume { get; }
	}

	[GenerateShape]
	public partial class VolumeCat : Creature
	{
		public VolumeCat(int pitch) => this.MeowPitch = pitch;

		public int MeowPitch { get; }
	}

	[GenerateShape]
	public partial record RenamedCat(string Name, [property: PropertyShape(Name = "voice-level")] int MeowPitch) : Animal(Name);

	[GenerateShape]
	public partial record OptionalNameOnly(string Name) : Animal(Name);

	[GenerateShape]
	public partial record OptionalWithDefault(string Name, [property: PropertyShape(IsRequired = false)] int BarkVolume = 0) : Animal(Name);

	[GenerateShape]
	public abstract partial class RequiredAnimalBase;

	[GenerateShape]
	public partial class RequiredPitchAnimal : RequiredAnimalBase
	{
		public required int MeowPitch { get; set; }
	}

	[GenerateShape]
	public partial class FlaggedBark : RequiredAnimalBase
	{
		[PropertyShape(IsRequired = true)]
		public int BarkVolume { get; set; }

		public string? Note { get; set; }
	}

	[GenerateShape]
	public abstract partial record Edit;

	[GenerateShape]
	public partial record TextEdit(string Range, string NewText) : Edit;

	[GenerateShape]
	public partial record AnnotatedEdit(string Range, string NewText, string AnnotationId) : TextEdit(Range, NewText);

	[GenerateShape]
	public partial record ComputedCatchAll : EvidenceBase
	{
		[PropertyShape(IsRequired = true)]
		public int ComputedValue => 1;
	}

	[GenerateShape]
	public partial record RealValue(int Value) : EvidenceBase;

	[GenerateShape]
	public abstract partial record EvidenceBase;

	[GenerateShape]
	public partial record Drawing;

	[GenerateShape]
	public partial record Circle(int Radius) : Drawing;

	[GenerateShape]
	public partial record Animal2(string Name);

	[GenerateShape]
	public partial record Cat2(string Name, int Lives) : Animal2(Name);

	[GenerateShape]
	public partial record Dog2(string Name, string Breed) : Animal2(Name);

	[GenerateShape]
	public abstract partial record PairBase;

	[GenerateShape]
	public partial record LeftTwin(string Name, int Value) : PairBase;

	[GenerateShape]
	public partial record RightTwin(string Name, int Value) : PairBase;

	[GenerateShape]
	public abstract partial record AbstractPet;

	[GenerateShape]
	public abstract partial record ShapeBase;

	[GenerateShape]
	public partial record CircleShape(int Radius) : ShapeBase;

	[GenerateShape]
	public partial record SquareShape(int Side) : ShapeBase;

	[GenerateShape]
	[DerivedTypeShape(typeof(TaggedDog))]
	public partial record TaggedAnimal(string Name);

	[GenerateShape]
	public partial record TaggedDog(string Name, int BarkVolume) : TaggedAnimal(Name);

	[GenerateShape]
	public abstract partial record NulBase;

	[GenerateShape]
	public partial record NulJoined([property: PropertyShape(Name = "A\0B")] int Value) : NulBase;

	[GenerateShape]
	public partial record NulSplit([property: PropertyShape(Name = "A")] int Left, [property: PropertyShape(Name = "B")] int Right) : NulBase;

	[GenerateShape]
	[DerivedTypeShape(typeof(NestedHorse))]
	public partial record NestedAnimal(string Name);

	[GenerateShape]
	[DerivedTypeShape(typeof(NestedQuarterHorse))]
	public partial record NestedHorse(string Name, int Speed) : NestedAnimal(Name);

	[GenerateShape]
	public partial record NestedQuarterHorse(string Name, int Speed, string Color) : NestedHorse(Name, Speed);

	[GenerateShape]
	public partial record TaggedAndDuck(NestedAnimal? Duck, NestedHorse? Tagged);

	[GenerateShape]
	public abstract partial class KeyedBase;

	[GenerateShape]
	public partial class KeyedDog : KeyedBase
	{
		[Key(0)]
		[PropertyShape(IsRequired = true)]
		public int BarkVolume { get; set; }
	}

	[GenerateShape]
	public partial class KeyedCat : KeyedBase
	{
		[Key(0)]
		[PropertyShape(IsRequired = true)]
		public int MeowPitch { get; set; }
	}

	[GenerateShapeFor<Creature>]
	[GenerateShapeFor<VolumeDog>]
	[GenerateShapeFor<VolumeCat>]
	[GenerateShapeFor<RenamedCat>]
	[GenerateShapeFor<OptionalNameOnly>]
	[GenerateShapeFor<OptionalWithDefault>]
	[GenerateShapeFor<PlainColor>]
	[GenerateShapeFor<RequiredAnimalBase>]
	[GenerateShapeFor<RequiredPitchAnimal>]
	[GenerateShapeFor<FlaggedBark>]
	[GenerateShapeFor<Edit>]
	[GenerateShapeFor<TextEdit>]
	[GenerateShapeFor<AnnotatedEdit>]
	[GenerateShapeFor<EvidenceBase>]
	[GenerateShapeFor<ComputedCatchAll>]
	[GenerateShapeFor<RealValue>]
	[GenerateShapeFor<Drawing>]
	[GenerateShapeFor<Circle>]
	[GenerateShapeFor<Animal2>]
	[GenerateShapeFor<Cat2>]
	[GenerateShapeFor<Dog2>]
	[GenerateShapeFor<PairBase>]
	[GenerateShapeFor<LeftTwin>]
	[GenerateShapeFor<RightTwin>]
	[GenerateShapeFor<AbstractPet>]
	[GenerateShapeFor<IPet>]
	[GenerateShapeFor<ShapeBase>]
	[GenerateShapeFor<CircleShape>]
	[GenerateShapeFor<SquareShape>]
	[GenerateShapeFor<KeyedBase>]
	[GenerateShapeFor<KeyedDog>]
	[GenerateShapeFor<KeyedCat>]
	[GenerateShapeFor<object>]
	[GenerateShapeFor<int>]
	[GenerateShapeFor<string>]
	private partial class ClassificationWitness;

	private sealed class CollapseNamingPolicy : MessagePackNamingPolicy
	{
		public override string ConvertName(string name) => "value";
	}
}
