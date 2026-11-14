// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Uncomment the following #define when adding or modifying sample output.
////#define RECORD

#if RECORD
#warning "Recording mode is enabled. This should never be checked in."
#endif

#pragma warning disable DuckTyping // Experimental API

using System.Globalization;
using System.Reflection;
using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

public partial class SampleOutputTests
{
	private const bool RecordMode =
#if RECORD
		true
#else
		false
#endif
		;

	private static readonly string OutputPath = typeof(SampleOutputTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(metadata => metadata.Key == "SampleOutputPath").Value!;

	public static IEnumerable<string> OutputNames()
	{
		yield return "Unions.LossyFarm.json";
		yield return "Unions.RoundtrippingFarmAnimal.json";
		yield return "Unions.HorsePen.json";
		yield return "Unions.HorseBreeds.json";
		yield return "Unions.MultiLevel.json";
		yield return "Unions.FlattenedAnimal.json";
		yield return "Unions.DuckTyping.json";
		yield return "Unions.WireFormat.json";
	}

	[Test]
	[MethodDataSource(nameof(OutputNames))]
	public void MatchesSample(string name)
	{
		string actual = Render(name);
		string path = Path.Combine(OutputPath, name);
#pragma warning disable CS0162 // Unreachable code detected
		if (RecordMode)
		{
			Directory.CreateDirectory(OutputPath);
			File.WriteAllText(path, actual);
			return;
		}
#pragma warning restore CS0162 // Unreachable code detected

		string expected = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
		if (expected != actual)
		{
			Console.WriteLine(Diff(expected, actual));
			Assert.Fail($"Sample output {name} does not match. The diff above shows the expected file as the baseline.");
		}
	}

	[Test]
	public void OutputDirectoryHasNoExtraFiles()
	{
		HashSet<string> names = new(OutputNames(), StringComparer.Ordinal);
		if (!Directory.Exists(OutputPath))
		{
			Assert.Fail($"Sample output directory '{OutputPath}' does not exist.");
			return;
		}

		foreach (string path in Directory.EnumerateFiles(OutputPath))
		{
			string name = Path.GetFileName(path);
			if (names.Contains(name))
			{
				continue;
			}

#pragma warning disable CS0162 // Unreachable code detected
			if (RecordMode)
			{
				File.Delete(path);
			}
			else
			{
				Assert.Fail($"'{name}' is not produced by a sample output test.");
			}
#pragma warning restore CS0162 // Unreachable code detected
		}
	}

	private static string Render(string name)
	{
		MessagePackSerializer serializer = new();
		return name switch
		{
			"Unions.LossyFarm.json" => Line<Sample1.LossyFarm.Farm, Witness>(serializer, UnionSampleOutput.Graphs.LossyFarm()),
			"Unions.RoundtrippingFarmAnimal.json" => Line<Sample1.RoundtrippingFarmAnimal.Farm, Witness>(serializer, UnionSampleOutput.Graphs.RoundtrippingFarm()),
			"Unions.HorsePen.json" => Line<Sample1.HorsePenWrapper.HorsePen, Witness>(serializer, UnionSampleOutput.Graphs.HorsePen()),
			"Unions.HorseBreeds.json" => Line<Sample1.HorseBreeds.BreedPen, Witness>(serializer, UnionSampleOutput.Graphs.HorseBreeds()),
			"Unions.MultiLevel.json" => Line<MultiLevelAnimals.Farm, Witness>(serializer, UnionSampleOutput.Graphs.MultiLevel()),
			"Unions.FlattenedAnimal.json" => Line<Sample1.FlattenedAnimal.Farm, Witness>(serializer, UnionSampleOutput.Graphs.FlattenedAnimal()),
			"Unions.DuckTyping.json" => DuckTypingLines(),
			"Unions.WireFormat.json" => WireFormatLines(serializer),
			_ => throw new ArgumentException($"No sample produces '{name}'.", nameof(name)),
		};
	}

	private static string DuckTypingLines()
	{
#if NET
		MessagePackSerializer serializer = new()
		{
			DerivedTypeUnions =
			[
				new DerivedTypeDuckTyping(
					TypeShapeResolver.Resolve<DuckTyping.Animal>(),
					TypeShapeResolver.Resolve<DuckTyping.Dog>(),
					TypeShapeResolver.Resolve<DuckTyping.Cat>()),
			],
		};
#else
		MessagePackSerializer serializer = new()
		{
			DerivedTypeUnions =
			[
				new DerivedTypeDuckTyping(
					TypeShapeResolver.ResolveDynamicOrThrow<DuckTyping.Animal>(),
					TypeShapeResolver.ResolveDynamicOrThrow<DuckTyping.Dog>(),
					TypeShapeResolver.ResolveDynamicOrThrow<DuckTyping.Cat>()),
			],
		};
#endif
		return Line<DuckTyping.Animal, DuckTyping.Animal>(serializer, UnionSampleOutput.Graphs.Dog())
			+ Line<DuckTyping.Animal, DuckTyping.Animal>(serializer, UnionSampleOutput.Graphs.Cat());
	}

	private static string WireFormatLines(MessagePackSerializer serializer)
	{
		RuntimeSubTypes.UseDiscriminatorObjectsSample.A value = UnionSampleOutput.Graphs.DiscriminatorObject();
		string arrayFormat = Line<RuntimeSubTypes.UseDiscriminatorObjectsSample.Base, RuntimeSubTypes.UseDiscriminatorObjectsSample.Base>(serializer, value);
		MessagePackSerializer objects = serializer with { UseDiscriminatorObjects = true };
		string objectFormat = Line<RuntimeSubTypes.UseDiscriminatorObjectsSample.Base, RuntimeSubTypes.UseDiscriminatorObjectsSample.Base>(objects, value);
		return arrayFormat + objectFormat;
	}

	private static string Line<T, TWitness>(MessagePackSerializer serializer, T value)
#if NET
		where TWitness : IShapeable<T>
#endif
	{
		byte[] msgpack = serializer.Serialize<T, TWitness>(value, CancellationToken.None);
		return serializer.ConvertToJson(msgpack) + "\n";
	}

	private static string Diff(string expected, string actual)
	{
		InlineDiffBuilder inlineBuilder = new(new Differ());
		DiffPaneModel result = inlineBuilder.BuildDiffModel(expected, actual);
		StringBuilder builder = new();
		int lineNumber = 0;
		foreach (DiffPiece line in result.Lines)
		{
			string prefix = line.Type switch
			{
				ChangeType.Inserted => "+ ",
				ChangeType.Deleted => "- ",
				_ => "  ",
			};
			lineNumber++;
			builder.Append(lineNumber.ToString(CultureInfo.InvariantCulture));
			builder.Append(": ");
			builder.Append(prefix);
			builder.AppendLine(line.Text);
		}

		return builder.ToString();
	}

	[GenerateShapeFor<Sample1.LossyFarm.Farm>]
	[GenerateShapeFor<Sample1.RoundtrippingFarmAnimal.Farm>]
	[GenerateShapeFor<Sample1.HorsePenWrapper.HorsePen>]
	[GenerateShapeFor<Sample1.HorseBreeds.BreedPen>]
	[GenerateShapeFor<MultiLevelAnimals.Farm>]
	[GenerateShapeFor<Sample1.FlattenedAnimal.Farm>]
	private partial class Witness;
}
