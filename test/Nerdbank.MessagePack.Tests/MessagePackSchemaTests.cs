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
using Newtonsoft.Json.Schema;

public partial class MessagePackSchemaTests(ITestOutputHelper logger)
{
	private const bool RecordMode =
#if RECORD
		true
#else
		false
#endif
		;

	private static readonly string KnownGoodSchemasPath = typeof(MessagePackSchemaTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "ResourcesPath").Value!;

	internal enum Sex
	{
		Male,
		Female,
	}

	[Fact]
	public void BasicObject_Map() => this.AssertSchema<BasicObject>();

	[Fact]
	public void Recursive() => this.AssertSchema<RecursiveType>();

	[Fact]
	public void Complex() => this.AssertSchema<Family>();

	private static string SchemaToString(JsonObject schema) => schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

	private static void Record(JsonObject schema, string testName)
	{
		string schemaString = SchemaToString(schema);
		File.WriteAllText(Path.Combine(KnownGoodSchemasPath, testName + ".schema.json"), schemaString, Encoding.UTF8);
	}

	private bool CheckMatchWithLKG(JsonObject schema, string testName)
	{
		string actual = SchemaToString(schema);
		string expected = File.ReadAllText(Path.Combine(KnownGoodSchemasPath, testName + ".schema.json"));
		if (expected != actual)
		{
			logger.WriteLine("Schema does not match the known good schema. The diff is shown below with expected as baseline.");

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
					logger.WriteLine(lineNumberPrefix + diffPrefix + line.Text);
				}
			}

			return false;
		}

		return true;
	}

	private JSchema AssertSchema<T>([CallerMemberName] string? testName = null)
		where T : IShapeable<T>
	{
		Requires.NotNull(testName!);

		JsonObject schema = MessagePackSchema.ToJsonSchema<T>();
		string schemaString = SchemaToString(schema);

#pragma warning disable CS0162 // Unreachable code detected
		if (RecordMode)
		{
			// Log the schema in the test output and record it.
			logger.WriteLine(schemaString);
			Record(schema, testName);
		}
		else
		{
			// Verify that the schema matches the LKG copy.
			if (this.CheckMatchWithLKG(schema, testName))
			{
				logger.WriteLine(schemaString);
			}
			else
			{
				Assert.Fail("Schema does not match the known good schema.");
			}
		}
#pragma warning restore CS0162 // Unreachable code detected

		// Verify that the schema is valid by parsing it as one.
		JSchema parsedSchema = JSchema.Parse(schemaString);
		return parsedSchema;
	}

	[GenerateShape]
	internal partial class BasicObject
	{
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

		public List<Person> Children { get; set; } = [];
	}

	[Description("A human person.")]
	internal class Person
	{
		[Description("The name of the person.")]
		public required string Name { get; set; }

		public required Sex Sex { get; set; }

		public int? Age { get; set; }

		public Dictionary<string, int> PetsAndAges { get; } = [];
	}
}
