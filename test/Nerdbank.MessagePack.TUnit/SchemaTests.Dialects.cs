// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

public partial class SchemaTests
{
	[Test, MatrixDataSource]
	public void DialectOptions_AllOverloads(JsonSchemaDialect dialect)
	{
		JsonSchemaOptions options = new() { Dialect = dialect };
		ITypeShape<BasicObject> shape = DialectWitness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<BasicObject>();
		JsonObject expected = this.Serializer.GetJsonSchema(shape, options);
		Assert.True(JsonNode.DeepEquals(expected, this.Serializer.GetJsonSchema<BasicObject>(options)));
		Assert.True(JsonNode.DeepEquals(expected, this.Serializer.GetJsonSchema<BasicObject, DialectWitness>(options)));
		Assert.True(JsonNode.DeepEquals(expected, this.Serializer.GetJsonSchema<BasicObject>(shape.Provider, options)));
		this.AssertDialectSchema(new BasicObject(), dialect);
	}

	[Test]
	public void DialectValues_FollowPublicationOrder()
	{
		Assert.Equal(4, (int)JsonSchemaDialect.Draft4);
		Assert.Equal(9, (int)JsonSchemaDialect.Draft2020_12);
		Assert.True(JsonSchemaDialect.Draft2020_12 > JsonSchemaDialect.Draft4);
	}

	[Test]
	public void DialectOptions_DefaultIsFixed()
	{
		Assert.Equal(JsonSchemaDialect.Draft2020_12, new JsonSchemaOptions().Dialect);
		Assert.Same(JsonSchemaOptions.Default, JsonSchemaOptions.Default);
		Assert.Equal(JsonSchemaDialect.Draft2020_12, JsonSchemaOptions.Default.Dialect);
		JsonSchemaOptions draft4 = JsonSchemaOptions.Default with { Dialect = JsonSchemaDialect.Draft4 };
		Assert.NotSame(JsonSchemaOptions.Default, draft4);
		Assert.Equal(JsonSchemaDialect.Draft2020_12, JsonSchemaOptions.Default.Dialect);
		JsonObject schema = this.Serializer.GetJsonSchema<BasicObject>();
		Assert.True(JsonNode.DeepEquals(schema, this.Serializer.GetJsonSchema<BasicObject>(new JsonSchemaOptions())));
		Assert.True(JsonNode.DeepEquals(schema, this.Serializer.GetJsonSchema<BasicObject>(JsonSchemaOptions.Default)));
		Assert.True(JsonNode.DeepEquals(schema, this.Serializer.GetJsonSchema<BasicObject>(DialectWitness.GeneratedTypeShapeProvider)));
		Assert.True(JsonNode.DeepEquals(schema, this.Serializer.GetJsonSchema<BasicObject, DialectWitness>()));
	}

	[Test]
	public void DialectOptions_SwitchingDoesNotReuseOtherDialectSchemas()
	{
		this.AssertDialectSchema(new RecursiveType(), JsonSchemaDialect.Draft4);
		this.AssertDialectSchema(new RecursiveType(), JsonSchemaDialect.Draft2020_12);
		this.AssertDialectSchema(new RecursiveType(), JsonSchemaDialect.Draft4);
	}

	[Test]
	public void DialectOptions_InvalidInputs()
	{
		ITypeShape<BasicObject> shape = DialectWitness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<BasicObject>();
		Assert.Throws<ArgumentNullException>(() => this.Serializer.GetJsonSchema(shape, null!));
		Assert.Throws<ArgumentNullException>(() => this.Serializer.GetJsonSchema(null!, new JsonSchemaOptions()));
		Assert.Throws<ArgumentOutOfRangeException>(() => this.Serializer.GetJsonSchema(shape, new JsonSchemaOptions { Dialect = (JsonSchemaDialect)int.MaxValue }));
		foreach (int reservedValue in new[] { 0, 1, 2, 3, 5, 6, 7, 8 })
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => this.Serializer.GetJsonSchema(shape, new JsonSchemaOptions { Dialect = (JsonSchemaDialect)reservedValue }));
		}
	}

	[Test, MatrixDataSource]
	public void DialectOptions_RecursiveAndGenericGraphs(JsonSchemaDialect dialect)
	{
		JSchema recursive = this.AssertDialectSchema(new RecursiveType { Child = new RecursiveType() }, dialect);
		JToken.Parse("null").Validate(recursive);
		JToken.Parse("{\"Child\":null}").Validate(recursive);
		Assert.False(JToken.Parse("{\"Child\":3}").IsValid(recursive));
		this.AssertDialectSchema(
			new Family
			{
				Father = new Person { Name = "Dad", Sex = Sex.Male },
				Children = [new Person { Name = "Child", Sex = Sex.Female }],
			},
			dialect);
	}

	[Test, MatrixDataSource]
#if NET
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void DialectOptions_DefinitionNameCollisions(JsonSchemaDialect dialect)
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("This test requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		Type first = CreateNode("SchemaCollisionFirst", typeof(int));
		Type second = CreateNode("SchemaCollisionSecond", typeof(string));
		Assert.Equal(first.FullName, second.FullName);
		Type firstList = typeof(List<>).MakeGenericType(first);
		Type secondList = typeof(List<>).MakeGenericType(second);
		ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SchemaCollisionRoot"), AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
		TypeBuilder root = module.DefineType("SchemaCollision.Root", TypeAttributes.Public);
		root.DefineDefaultConstructor(MethodAttributes.Public);
		root.DefineField("First", firstList, FieldAttributes.Public);
		root.DefineField("Second", secondList, FieldAttributes.Public);
		root.DefineField("Repeat", firstList, FieldAttributes.Public);
		ITypeShapeProvider provider = PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default;
		JsonObject schema = this.Serializer.GetJsonSchema(provider.GetTypeShape(root.CreateTypeInfo()!.AsType())!, new JsonSchemaOptions { Dialect = dialect });
		string definitionsKeyword = dialect == JsonSchemaDialect.Draft4 ? "definitions" : "$defs";
		JsonObject definitions = schema[definitionsKeyword]!.AsObject();
		Assert.Equal(5, definitions.Count);
		Assert.DoesNotContain("Version=", schema.ToJsonString());
		Assert.DoesNotContain("SchemaCollisionFirst", schema.ToJsonString());
		Assert.DoesNotContain("SchemaCollisionSecond", schema.ToJsonString());
		JSchema parsed = JSchema.Parse(schema.ToJsonString());
		JToken.Parse("""
			{"First":[{"Value":1,"Next":{"Value":2}}],"Second":[{"Value":"a","Next":{"Value":"b"}}],"Repeat":[{"Value":3}]}
			""").Validate(parsed);
		Assert.False(JToken.Parse("""{"First":[{"Value":"bad"}]}""").IsValid(parsed));
		Assert.False(JToken.Parse("""{"Second":[{"Value":1}]}""").IsValid(parsed));
		Assert.False(JToken.Parse("""{"Repeat":[{"Value":"bad"}]}""").IsValid(parsed));

		static Type CreateNode(string assemblyName, Type valueType)
		{
			ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run).DefineDynamicModule("Main");
			TypeBuilder node = module.DefineType("SchemaCollision.Node", TypeAttributes.Public);
			node.DefineDefaultConstructor(MethodAttributes.Public);
			node.DefineField("Value", valueType, FieldAttributes.Public);
			node.DefineField("Next", node, FieldAttributes.Public);
			return node.CreateTypeInfo()!.AsType();
		}
	}

	[Test, MatrixDataSource]
	public void DialectOptions_PositionalAndHomogeneousArrays(JsonSchemaDialect dialect)
	{
		JSchema schema = this.AssertDialectSchema(new ArrayOfValuesObject { Property0 = "hello", GappedProperty = true }, dialect);
		JToken.Parse("[\"hello\",{},true,null,42]").Validate(schema);
		Assert.False(JToken.Parse("[\"hello\",{},\"not a boolean\"]").IsValid(schema));
		JSchema required = this.AssertDialectSchema(new ArrayOfValuesWithRequired { Property0 = "required" }, dialect);
		Assert.False(JToken.Parse("[]").IsValid(required));
		JSchema list = this.AssertDialectSchema(new List<int> { 1, 2, 3 }, dialect);
		Assert.False(JToken.Parse("[1,\"bad\"]").IsValid(list));
		this.AssertDialectSchema(new Point(3, 5), dialect);
		this.Serializer = this.Serializer.WithHiFiDateTime();
		this.AssertDialectSchema(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), dialect);
		this.AssertDialectSchema(DateTimeOffset.UtcNow, dialect);
	}

	[Test, MatrixDataSource]
	public void DialectOptions_Unions(JsonSchemaDialect dialect)
	{
		JSchema schema = this.AssertDialectSchema<BaseType>(new SubType { Message = "hello", Value = 3 }, dialect);
		Assert.False(JToken.Parse("[1,{},42]").IsValid(schema));
		Assert.False(JToken.Parse("[\"wrong tag\",{}]").IsValid(schema));
	}

	[Test, MatrixDataSource]
	public void DialectOptions_FlatArrays(JsonSchemaDialect dialect)
	{
		this.Serializer = this.Serializer with { MultiDimensionalArrayFormat = MultiDimensionalArrayFormat.Flat };
		int[,] rank2 = new int[2, 2];
		rank2[1, 1] = 4;
		this.AssertDialectSchema(rank2, dialect);
		int[,,] rank3 = new int[1, 1, 2];
		rank3[0, 0, 1] = 2;
		this.AssertDialectSchema(rank3, dialect);
#if NET
		this.AssertDialectSchema(new int[1, 1, 1, 2], dialect);
#endif
	}

	[Test, MatrixDataSource]
	public void DialectOptions_CustomConverters(JsonSchemaDialect dialect)
	{
		this.Serializer = this.Serializer with { Converters = [new TupleCustomConverter(dialect)] };
		JSchema schema = this.AssertDialectSchema(new CustomType(), dialect);
		Assert.False(JToken.Parse("[\"bad\",\"value\"]").IsValid(schema));
		Assert.False(JToken.Parse("[1]").IsValid(schema));
		Assert.False(JToken.Parse("[1,\"value\",3]").IsValid(schema));
		this.Serializer = this.Serializer with { Converters = [new SingleDialectCustomConverter()] };
		if (dialect == JsonSchemaDialect.Draft4)
		{
			Assert.Throws<NotSupportedException>(() => this.Serializer.GetJsonSchema<CustomType>(new JsonSchemaOptions { Dialect = dialect }));
		}
		else
		{
			this.AssertDialectSchema(new CustomType(), dialect);
		}
	}

	private JSchema AssertDialectSchema<T>(T value, JsonSchemaDialect dialect)
	{
		ITypeShape<T> shape = DialectWitness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<T>();
		JsonObject schema = this.Serializer.GetJsonSchema(shape, new JsonSchemaOptions { Dialect = dialect });
		string definitions = dialect == JsonSchemaDialect.Draft4 ? "definitions" : "$defs";
		string schemaUri = dialect == JsonSchemaDialect.Draft4 ? "http://json-schema.org/draft-04/schema#" : "https://json-schema.org/draft/2020-12/schema";
		Assert.Equal(schemaUri, schema["$schema"]!.GetValue<string>());
		Assert.False(schema.ContainsKey(dialect == JsonSchemaDialect.Draft4 ? "$defs" : "definitions"));
		AssertSchemaKeywords(schema);
		JSchema parsed = JSchema.Parse(schema.ToJsonString());
		byte[] msgpack = this.Serializer.Serialize(value, shape);
		JToken.Parse(this.Serializer.ConvertToJson(msgpack, new() { IgnoreKnownExtensions = true })).Validate(parsed);
		return parsed;

		void AssertSchemaKeywords(JsonNode? node)
		{
			if (node is JsonObject obj)
			{
				if (obj["$ref"] is JsonValue reference)
				{
					Assert.StartsWith($"#/{definitions}/", reference.GetValue<string>());
				}

				if (dialect == JsonSchemaDialect.Draft4)
				{
					Assert.False(obj.ContainsKey("prefixItems"));
				}
				else
				{
					Assert.IsNotType<JsonArray>(obj["items"]);
				}

				foreach (KeyValuePair<string, JsonNode?> property in obj)
				{
					AssertSchemaKeywords(property.Value);
				}
			}
			else if (node is JsonArray array)
			{
				foreach (JsonNode? item in array)
				{
					AssertSchemaKeywords(item);
				}
			}
		}
	}

	[GenerateShapeFor<BasicObject>]
	[GenerateShapeFor<RecursiveType>]
	[GenerateShapeFor<Family>]
	[GenerateShapeFor<ArrayOfValuesObject>]
	[GenerateShapeFor<ArrayOfValuesWithRequired>]
	[GenerateShapeFor<BaseType>]
	[GenerateShapeFor<List<int>>]
	[GenerateShapeFor<Point>]
	[GenerateShapeFor<DateTime>]
	[GenerateShapeFor<DateTimeOffset>]
	[GenerateShapeFor<int[,]>]
	[GenerateShapeFor<int[,,]>]
#if NET
	[GenerateShapeFor<int[,,,]>]
#endif
	[GenerateShapeFor<CustomType>]
	private partial class DialectWitness;

	private class TupleCustomConverter(JsonSchemaDialect expectedDialect) : DocumentingCustomConverter
	{
		public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		{
			Assert.Equal(expectedDialect, context.Dialect);
			Assert.Throws<ArgumentNullException>(() => context.CreateTupleSchema(null!));
			Assert.Throws<ArgumentException>(() => context.CreateTupleSchema(new JsonArray()));
			Assert.Throws<ArgumentNullException>(() => context.ApplyTupleSchema(null!, new JsonArray(new JsonObject())));
			JsonObject schema = context.CreateTupleSchema(new JsonArray(
				new JsonObject { ["type"] = "integer" },
				new JsonObject { ["type"] = "string" }));
			schema["minItems"] = 2;
			schema["maxItems"] = 2;
			return schema;
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			writer.WriteArrayHeader(2);
			writer.Write(1);
			writer.Write("value");
		}
	}

	private class SingleDialectCustomConverter : DocumentingCustomConverter
	{
		public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		{
			if (context.Dialect != JsonSchemaDialect.Draft2020_12)
			{
				throw new NotSupportedException("This converter only supports Draft 2020-12.");
			}

			return base.GetJsonSchema(context, typeShape);
		}
	}
}
