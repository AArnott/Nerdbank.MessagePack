// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Uncomment the following #define when adding or modifying tests, or intentionally changing the output of the schema generator.
////#define RECORD

#if RECORD
#warning "Recording mode is enabled. This should never be checked in."
#endif

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

public partial class SchemaTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	private const bool RecordMode =
#if RECORD
		true
#else
		false
#endif
		;

	private static readonly string KnownGoodSchemasPath = typeof(SchemaTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "ResourcesPath").Value!;

	internal enum Sex
	{
		Male,
		Female,
	}

	[Fact]
	public void BasicObject_Map() => this.AssertSchema([new BasicObject { IntProperty = 3, StringProperty = "hi" }]);

	[Fact]
	public void BasicObject_Map_NamingPolicy()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		this.AssertSchema([new BasicObject { IntProperty = 3, StringProperty = "hi" }]);
	}

	[Fact]
	public void BasicObject_Key_AcceptsNull() => this.AssertSchema<ArrayOfValuesObject>([null], testName: "BasicObject_Key");

	[Fact]
	public void BasicObject_Key_AcceptsArrays()
	{
		JSchema schema = this.AssertSchema<ArrayOfValuesObject>(testName: "BasicObject_Key");

		// Force additional array elements to be denied to verify that the indexes are explicitly allowed by the schema.
		DisallowAdditionalProperties(schema);

		this.Logger.WriteLine("Modified schema:\n{0}", schema.ToString());

		JToken.Parse("""
			["str1", null, true]
			""").Validate(schema);

		JToken.Parse("""
			["str1", { "unknown": "object" }, true]
			""").Validate(schema);
	}

	[Fact]
	public void BasicObject_Key_AcceptsMaps()
	{
		JSchema schema = this.AssertSchema<ArrayOfValuesObject>(testName: "BasicObject_Key");

		// Force additional properties to be denied to verify that the indexes are explicitly allowed by the schema.
		DisallowAdditionalProperties(schema);

		JToken.Parse("""
			{
				"0": "str1",
				"2": true
			}
			""").Validate(schema);
	}

	[Fact]
	public void Recursive() => this.AssertSchema([new RecursiveType { Child = new RecursiveType() }]);

	[Fact]
	public void Complex() => this.AssertSchema([
		new Family
		{
			Father = new Person { Name = "Dad", Sex = Sex.Male },
			Mother = new Person { Name = "Mom", Sex = Sex.Female },
			Children = [
				new Person { Name = "Bob", Sex = Sex.Male },
			],
		},
		]);

	[Fact]
	public void DateTimeExtension() => this.AssertSchema([new HasDateTime { Timestamp = DateTime.Now }]);

	[Fact]
	public void CustomConverterHasUndocumentedSchema() => this.AssertSchema([new TypeWithNonDocumentingCustomConverter()]);

	[Fact]
	public void CustomConverterWithDocumentedSchema()
	{
		this.Serializer.RegisterConverter(new DocumentingCustomConverter());
		this.AssertSchema([new CustomType(), null]);
	}

	[Fact]
	public void SubTypeSchema() => this.AssertSchema([new BaseType { Message = "hi" }, new SubType { Message = "hi", Value = 5 }]);

	/// <summary>
	/// Verify that registering converters while <see cref="MessagePackSerializer.PreserveReferences"/>
	/// is <see langword="true"/> does not mess up the schema generation after it is turned off.
	/// </summary>
	[Fact]
	public void ReferencePreservationGraphReset()
	{
		this.Serializer = this.Serializer with { PreserveReferences = true };
		this.Serializer.RegisterConverter(new DocumentingCustomConverter());
		this.Serializer.RegisterConverter(new NonDocumentingCustomConverter());
		this.Serializer = this.Serializer with { PreserveReferences = false };
		JsonObject schema = this.Serializer.GetJsonSchema<CustomType>();
		string schemaString = schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
		this.Logger.WriteLine(schemaString);
		Assert.DoesNotContain("ReferencePreservingConverter", schemaString);
	}

	private static string SchemaToString(JsonObject schema) => schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

	private static void Record(JsonObject schema, string testName)
	{
		string schemaString = SchemaToString(schema);
		File.WriteAllText(Path.Combine(KnownGoodSchemasPath, testName + ".schema.json"), schemaString, Encoding.UTF8);
	}

	private static void DisallowAdditionalProperties(JSchema schema)
	{
		// We always allow additional properties on types that are explicitly allowed to be anything.
		const JSchemaType All = JSchemaType.Object | JSchemaType.Array | JSchemaType.String | JSchemaType.Integer | JSchemaType.Boolean | JSchemaType.Null | JSchemaType.Number;
		if (schema.Type is JSchemaType type && type != All)
		{
			if (type.HasFlag(JSchemaType.Object) is true)
			{
				schema.AllowAdditionalProperties = false;
			}

			if (type.HasFlag(JSchemaType.Array) is true)
			{
				schema.AllowAdditionalItems = false;
			}
		}

		foreach (JSchema sub in schema.OneOf.Concat(schema.AllOf).Concat(schema.AnyOf).Concat(schema.Items).Concat(schema.Properties.Values))
		{
			DisallowAdditionalProperties(sub);
		}
	}

	private bool CheckMatchWithLKG(JsonObject schema, string testName)
	{
		string actual = SchemaToString(schema);
		string expected = File.ReadAllText(Path.Combine(KnownGoodSchemasPath, testName + ".schema.json"));
		if (expected != actual)
		{
			this.Logger.WriteLine("Schema does not match the known good schema. The diff is shown below with expected as baseline.");

			InlineDiffBuilder inlineBuilder = new(new Differ());
			DiffPaneModel result = inlineBuilder.BuildDiffModel(expected, actual);
			if (result.Lines.Any(l => l.Type != ChangeType.Unchanged))
			{
				int maxLineNumberLength = result.Lines.Count.ToString(CultureInfo.CurrentCulture).Length;
				int lineNumber = 0;
				foreach (DiffPiece? line in result.Lines)
				{
					string lineNumberPrefix = string.Format(CultureInfo.CurrentCulture, "{0," + maxLineNumberLength + "}: ", ++lineNumber);
					string diffPrefix = line.Type switch
					{
						ChangeType.Inserted => "+ ",
						ChangeType.Deleted => "- ",
						_ => "  ",
					};
					this.Logger.WriteLine(lineNumberPrefix + diffPrefix + line.Text);
				}
			}

			return false;
		}

		return true;
	}

	private JSchema AssertSchema<T>(T?[]? sampleData = null, [CallerMemberName] string? testName = null)
		where T : IShapeable<T>
	{
		Requires.NotNull(testName!);

		JsonObject schema = this.Serializer.GetJsonSchema<T>();
		string schemaString = SchemaToString(schema);

#pragma warning disable CS0162 // Unreachable code detected
		if (RecordMode)
		{
			// Log the schema in the test output and record it.
			this.Logger.WriteLine(schemaString);
			Record(schema, testName);
		}
		else
		{
			// Verify that the schema matches the LKG copy.
			if (this.CheckMatchWithLKG(schema, testName))
			{
				this.Logger.WriteLine(schemaString);
			}
			else
			{
				Assert.Fail("Schema does not match the known good schema.");
			}
		}
#pragma warning restore CS0162 // Unreachable code detected

		// Verify that the schema is valid by parsing it as one.
		JSchema parsedSchema = JSchema.Parse(schemaString);

		// Verify that the sample data actually serializes to the schema.
		if (sampleData is not null)
		{
			int sampleCounter = 0;
			bool anyFailed = false;
			foreach (T? item in sampleData)
			{
				byte[] msgpack = this.Serializer.Serialize(item);
				string json = MessagePackSerializer.ConvertToJson(msgpack);
				this.Logger.WriteLine($"Sample data {++sampleCounter}:");
				var parsed = JsonNode.Parse(json);
				this.Logger.WriteLine(parsed is null ? "null" : parsed.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
				try
				{
					JToken.Parse(json).Validate(parsedSchema);
				}
				catch (Exception ex)
				{
					this.Logger.WriteLine("Failed: {0}", ex);
					anyFailed = true;
				}
			}

			Assert.False(anyFailed);
		}

		return parsedSchema;
	}

	[GenerateShape]
	internal partial class BasicObject
	{
		public BasicObject(int intProperty = 5) => this.IntProperty = intProperty;

		public string? StringProperty { get; set; }

		public int IntProperty { get; set; }
	}

	[GenerateShape]
	internal partial class RecursiveType
	{
		public RecursiveType? Child { get; set; }
	}

	[GenerateShape]
	internal partial class Family
	{
		public Person? Mother { get; set; }

		[Description("The father.")]
		public Person? Father { get; set; }

		[PropertyShape(Name = "progeny")]
		public List<Person> Children { get; set; } = [];
	}

	[Description("A human person.")]
	internal class Person
	{
		[Description("The name of the person.")]
		public required string Name { get; set; }

		public Sex Sex { get; set; }

		[DefaultValue(18)]
		public int? Age { get; set; }

		public Dictionary<string, int> PetsAndAges { get; } = [];
	}

	[GenerateShape]
	internal partial class ArrayOfValuesObject
	{
		[Key(0)]
		public string? Property0 { get; set; }

		[Key(2)]
		public bool GappedProperty { get; set; }

		[Key(3)]
		public Person? Person { get; set; }
	}

	[GenerateShape]
	internal partial class HasDateTime
	{
		public DateTime Timestamp { get; set; }
	}

	[GenerateShape]
	[KnownSubType<SubType>(1)]
	internal partial class BaseType
	{
		public string? Message { get; set; }
	}

	[GenerateShape]
	internal partial class SubType : BaseType
	{
		public int Value { get; set; }
	}

	[GenerateShape, MessagePackConverter(typeof(NonDocumentingCustomConverter))]
	internal partial class TypeWithNonDocumentingCustomConverter
	{
	}

	internal class NonDocumentingCustomConverter : MessagePackConverter<TypeWithNonDocumentingCustomConverter>
	{
		public override TypeWithNonDocumentingCustomConverter? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			reader.Skip(context);
			return new TypeWithNonDocumentingCustomConverter();
		}

		public override void Write(ref MessagePackWriter writer, in TypeWithNonDocumentingCustomConverter? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteMapHeader(0);
		}
	}

	[GenerateShape]
	internal partial class CustomType
	{
	}

	internal class DocumentingCustomConverter : MessagePackConverter<CustomType>
	{
		public override JsonObject GetJsonSchema(JsonSchemaContext context, ITypeShape typeShape)
		{
			return new JsonObject
			{
				["type"] = "object",
				["properties"] = new JsonObject
				{
				},
			};
		}

		public override CustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			reader.Skip(context);
			return new CustomType();
		}

		public override void Write(ref MessagePackWriter writer, in CustomType? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteMapHeader(0);
		}
	}
}
