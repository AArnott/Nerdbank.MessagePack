// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

public partial class UnionTypeAliasProviderTests : MessagePackSerializerTestBase
{
#if NET
	public interface IError
	{
		static abstract string Name { get; }
	}
#endif

	[Test, MatrixDataSource]
	public async Task CustomAliases(bool async, bool integerAliases, bool discriminatorObjects, bool perfOverSchemaStability, bool useMapping)
	{
		DerivedTypeIdentifier numberAlias = integerAliases ? new(-73) : new("stable.number");
		DerivedTypeIdentifier textAlias = integerAliases ? new(74) : new("stable.text");
		IUnionTypeAliasProvider provider;
		if (useMapping)
		{
			UnionCaseMapping mapping = new(typeof(Scalar));
			mapping.Add(numberAlias, typeof(int));
			mapping.Add(textAlias, typeof(string));
			provider = new UnionCaseMappingCollection([mapping]);
		}
		else
		{
			provider = new DelegateUnionTypeAliasProvider((unionType, caseType) =>
			{
				Assert.Equal(typeof(Scalar), unionType);
				return caseType == typeof(int) ? numberAlias : textAlias;
			});
		}

		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = provider,
			UseDiscriminatorObjects = discriminatorObjects,
			PerfOverSchemaStability = perfOverSchemaStability,
		};

		Scalar number = new(42);
		Scalar result = async ? await this.RoundtripAsync(number) : this.Roundtrip(number);
		Assert.Equal(42, result.Value);
		string aliasJson = integerAliases ? "-73" : "\"stable.number\"";
		Assert.Equal(
			discriminatorObjects ? $"{{{aliasJson}:42}}" : $"[{aliasJson},42]",
			this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		if (!integerAliases || !discriminatorObjects)
		{
			Assert.True(this.DataMatchesSchema(this.lastRoundtrippedMsgpack, GetShape<Scalar>()));
		}

		string schema = this.Serializer.GetJsonSchema(GetShape<Scalar>()).ToJsonString();
		Assert.Contains(integerAliases ? discriminatorObjects ? "\"-73\"" : "-73" : "stable.number", schema);

		Scalar text = new("hello");
		result = async ? await this.RoundtripAsync(text) : this.Roundtrip(text);
		Assert.Equal("hello", result.Value);
		aliasJson = integerAliases ? "74" : "\"stable.text\"";
		Assert.Equal(
			discriminatorObjects ? $"{{{aliasJson}:\"hello\"}}" : $"[{aliasJson},\"hello\"]",
			this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		if (!integerAliases || !discriminatorObjects)
		{
			Assert.True(this.DataMatchesSchema(this.lastRoundtrippedMsgpack, GetShape<Scalar>()));
		}
	}

	[Test]
	public void MappingCollectionAndInitializers()
	{
		UnionCaseMapping mapping = new(typeof(Scalar))
		{
			{ "number", typeof(int) },
			{ 74, typeof(string) },
		};
		UnionCaseMappingCollection mappings = [mapping];
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = mappings };
		Assert.Equal(typeof(Scalar), mapping.UnionType);
		Assert.Collection(mapping.OrderBy(e => e.Key.FullName), e => Assert.Equal(typeof(int), e.Key), e => Assert.Equal(typeof(string), e.Key));
		Assert.Single(mappings);
		Assert.Same(mapping, Assert.Single((IEnumerable<UnionCaseMapping>)mappings));
		Assert.Same(mapping, Assert.Single(((IEnumerable)mappings).Cast<UnionCaseMapping>()));
		Assert.Equal(mapping.ToArray(), ((IEnumerable)mapping).Cast<KeyValuePair<Type, DerivedTypeIdentifier>>().ToArray());
		Assert.Equal(2, mapping.Count);
		Assert.Equal(1, mappings.Count);
		Assert.Equal(42, this.Roundtrip(new Scalar(42)).Value);
		Assert.Equal("""["number",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void MappingsFrozenBeforeSerializerAssignment()
	{
		UnionCaseMapping mapping = new(typeof(Scalar));
		mapping.Add("number", typeof(int));
		UnionCaseMappingCollection mappings = [mapping];
		Assert.Throws<InvalidOperationException>(() => mapping.Add("text", typeof(string)));

		this.Serializer = this.Serializer with { UnionTypeAliasProvider = mappings };
		MessagePackSerializer other = new() { UnionTypeAliasProvider = mappings };
		Assert.Equal(42, this.Roundtrip(new Scalar(42)).Value);
		Assert.Equal(42, other.Deserialize(other.Serialize(new Scalar(42), GetShape<Scalar>()), GetShape<Scalar>()).Value);
		Assert.Throws<InvalidOperationException>(() => mapping.Add("boolean", typeof(bool)));
	}

	[Test]
	public void FrozenMappingsSupportConcurrentSerialization()
	{
		UnionCaseMapping mapping = new(typeof(Scalar));
		mapping.Add("number", typeof(int));
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = new UnionCaseMappingCollection([mapping]) };
		Parallel.For(0, 32, i =>
		{
			byte[] msgpack = this.Serializer.Serialize(new Scalar(i), GetShape<Scalar>());
			Assert.Equal(i, this.Serializer.Deserialize(msgpack, GetShape<Scalar>()).Value);
			Assert.Equal($"[\"number\",{i}]", this.Serializer.ConvertToJson(msgpack));
		});
		Assert.Throws<InvalidOperationException>(() => mapping.Add("text", typeof(string)));
	}

	[Test]
	public void InvalidRegistrationsDoNotMutateMapping()
	{
		UnionCaseMapping mapping = new(typeof(Scalar));
		Assert.Throws<ArgumentNullException>(() => mapping.Add("null", null!));
		Assert.Throws<ArgumentException>(() => mapping.Add(default, typeof(int)));
		mapping.Add("number", typeof(int));
		Assert.Throws<ArgumentException>(() => mapping.Add("number", typeof(string)));
		Assert.Throws<ArgumentException>(() => mapping.Add("text", typeof(int)));
		mapping.Add("text", typeof(string));
		Assert.Equal(2, mapping.Count);

		UnionCaseMapping integers = new(typeof(ReorderedScalar));
		integers.Add(1, typeof(int));
		Assert.Throws<ArgumentException>(() => integers.Add(1, typeof(string)));
		integers.Add(2, typeof(string));
		Assert.Equal(2, integers.Count);
	}

	[Test]
	public void DuplicateUnionsRejected()
	{
		UnionCaseMapping first = new(typeof(Scalar));
		UnionCaseMapping second = new(typeof(Scalar));
		Assert.Throws<ArgumentException>(() => new UnionCaseMappingCollection([first, second]));
	}

	[Test]
	public void UnknownCaseRejected()
	{
		UnionCaseMapping mapping = new(typeof(Scalar));
		mapping.Add("boolean", typeof(bool));
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = new UnionCaseMappingCollection([mapping]) };
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(new Scalar(42)));
		Assert.Contains(typeof(Scalar).ToString(), GetFullMessage(ex));
		Assert.Contains(typeof(bool).ToString(), GetFullMessage(ex));
	}

	[Test, MatrixDataSource]
	public void UnmappedCasesAndUnionsRetainDefaults(bool useMapping)
	{
		UnionCaseMapping mapping = new(typeof(Scalar));
		mapping.Add("number", typeof(int));
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = useMapping
				? new UnionCaseMappingCollection([mapping])
				: new DelegateUnionTypeAliasProvider((unionType, caseType) =>
					unionType == typeof(Scalar) && caseType == typeof(int) ? new("number") : (DerivedTypeIdentifier?)null),
		};
		Assert.Equal(42, this.Roundtrip(new Scalar(42)).Value);
		Assert.Equal("""["number",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Equal("hello", this.Roundtrip(new Scalar("hello")).Value);
		Assert.Equal("""["String","hello"]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Equal(42, this.Roundtrip(new ReorderedScalar(42)).Value);
		Assert.Equal("""["Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void SameCaseCanHaveDifferentAliasesInDifferentUnions()
	{
		UnionCaseMapping first = new(typeof(Scalar));
		first.Add("first.number", typeof(int));
		UnionCaseMapping second = new(typeof(ReorderedScalar));
		second.Add("second.number", typeof(int));
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = new UnionCaseMappingCollection([first, second]) };
		Assert.Equal(42, this.Roundtrip(new Scalar(42)).Value);
		Assert.Equal("""["first.number",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Equal(42, this.Roundtrip(new ReorderedScalar(42)).Value);
		Assert.Equal("""["second.number",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public void ReorderingAndAddingCasesPreservesWireContract(bool integerAliases)
	{
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((unionType, caseType) =>
				caseType == typeof(int) ? integerAliases ? new(73) : new("number") : (DerivedTypeIdentifier?)null),
		};
		this.Roundtrip(new Scalar(42));
		ReadOnlySequence<byte> original = this.lastRoundtrippedMsgpack;
		Assert.Equal(42, this.Serializer.Deserialize(original, GetShape<ReorderedScalar>()).Value);
		Assert.Equal(42, this.Serializer.Deserialize(original, GetShape<ExpandedScalar>()).Value);
		this.Roundtrip(new ReorderedScalar(42));
		Assert.Equal(original.ToArray(), this.lastRoundtrippedMsgpack.ToArray());
		this.Roundtrip(new ExpandedScalar(42));
		Assert.Equal(original.ToArray(), this.lastRoundtrippedMsgpack.ToArray());
	}

	[Test]
	public void FullShapeProviderAndConverterCaching()
	{
		ShapeAliasProvider provider = new();
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = provider };
		Assert.Equal(42, this.Roundtrip(new Scalar(42)).Value);
		Assert.Equal("""["Scalar.Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		int calls = provider.Calls;
		Assert.Equal(2, calls);
		this.Roundtrip(new Scalar("hello"));
		this.Serializer.GetJsonSchema(GetShape<Scalar>());
		Assert.Equal(calls, provider.Calls);
	}

	[Test]
	public void ChangingProviderInvalidatesClonedSerializerCache()
	{
		MessagePackSerializer original = this.Serializer;
		this.Roundtrip(new Scalar(42));
		this.Serializer = original with { UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => new("custom." + c.Name)) };
		this.Roundtrip(new Scalar(42));
		Assert.Equal("""["custom.Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		this.Serializer = this.Serializer with { UnionTypeAliasProvider = null };
		this.Roundtrip(new Scalar(42));
		Assert.Equal("""["Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		this.Serializer = original;
		this.Roundtrip(new Scalar(42));
		Assert.Equal("""["Int32",42]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public void InvalidProviderAliasesRejected(bool duplicate, bool integerAliases)
	{
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) =>
				duplicate ? integerAliases ? new(73) : new("duplicate") : default(DerivedTypeIdentifier)),
		};
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(new Scalar(42)));
		Assert.Contains(typeof(Scalar).ToString(), GetFullMessage(ex));
		Assert.Contains(duplicate ? "Duplicate alias" : "must be initialized", GetFullMessage(ex));
	}

	[Test]
	public void CustomAliasCollidingWithDefaultAliasRejected()
	{
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) =>
				c == typeof(int) ? new("String") : (DerivedTypeIdentifier?)null),
		};
		Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(new Scalar(42)));
	}

	[Test]
	public void CustomIntegerCollidingWithInferredReadTagRejected()
	{
		IUnionTypeShape shape = Assert.IsAssignableFrom<IUnionTypeShape>(GetShape<Scalar>());
		int textTag = shape.UnionCases.Single(c => c.UnionCaseType.Type == typeof(string)).Tag;
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) =>
				c == typeof(int) ? new(textTag) : (DerivedTypeIdentifier?)null),
		};
		Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(new Scalar(42)));
	}

	[Test, MatrixDataSource]
	public void OverriddenCasesDoNotAcceptInferredAliases(bool integerAlias)
	{
		IUnionTypeShape shape = Assert.IsAssignableFrom<IUnionTypeShape>(GetShape<Scalar>());
		IUnionCaseShape numberCase = shape.UnionCases.Single(c => c.UnionCaseType.Type == typeof(int));
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteArrayHeader(2);
		writer.WriteRaw((integerAlias ? new DerivedTypeIdentifier(numberCase.Tag) : new(numberCase.Name)).MsgPackAlias.Span);
		writer.Write(42);
		writer.Flush();
		Assert.Equal(42, this.Serializer.Deserialize(sequence, GetShape<Scalar>()).Value);
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => new("custom." + c.Name)),
		};
		Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize(sequence, GetShape<Scalar>()));
	}

	[Test]
	public void RecursiveUnionAndNullPayload()
	{
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => new("custom." + c.Name)),
		};
		CSharpUnionTests.BoolOrNested result = this.Roundtrip(new CSharpUnionTests.BoolOrNested(new CSharpUnionTests.BoolOrNested(true)));
		Assert.Equal(true, Assert.IsType<CSharpUnionTests.BoolOrNested>(result.Value).Value);
		Assert.Equal("""["custom.BoolOrNested",["custom.Boolean",true]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Null(this.Roundtrip(default(Scalar)).Value);
		Assert.Equal("""["custom.String",null]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void HierarchyUnionsAreUnaffected()
	{
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => throw new InvalidOperationException("Not a C# union.")),
		};
		Animal? result = this.Roundtrip<Animal>(new Dog("Rover"));
		Assert.Equal(new Dog("Rover"), result);
		Assert.Equal("""["Dog",{"Name":"Rover"}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

#if NET
	[Test, MatrixDataSource]
	public async Task StaticAbstractErrorRegistry(bool async, bool useMapping)
	{
		Dictionary<Type, string> registry = [];
		UnionCaseMapping mapping = new(typeof(SignInError));
		Register<InvalidOtpError>();
		Register<UserNotFoundError>();
		this.Serializer = this.Serializer with
		{
			UnionTypeAliasProvider = useMapping
				? new UnionCaseMappingCollection([mapping])
				: new DelegateUnionTypeAliasProvider((u, c) => registry.TryGetValue(c, out string? name) ? new(name) : (DerivedTypeIdentifier?)null),
		};
		SignInError error = new(new InvalidOtpError(2));
		SignInError result = async ? await this.RoundtripAsync(error) : this.Roundtrip(error);
		Assert.Equal(new InvalidOtpError(2), result.Value);
		Assert.Equal("""["Shenas.InvalidOtpError",{"RemainingAttempts":2}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));

		void Register<TError>()
			where TError : IError
		{
			registry.Add(typeof(TError), TError.Name);
			mapping.Add(TError.Name, typeof(TError));
		}
	}
#endif

	[Test]
	public void NullArgumentsRejected()
	{
		Assert.Throws<ArgumentNullException>(() => new UnionCaseMapping(null!));
		Assert.Throws<ArgumentNullException>(() => new DelegateUnionTypeAliasProvider(null!));
		Assert.Throws<ArgumentNullException>(() => new UnionCaseMappingCollection([null!]));
		DelegateUnionTypeAliasProvider provider = new((u, c) => null);
		IUnionTypeShape shape = Assert.IsAssignableFrom<IUnionTypeShape>(GetShape<Scalar>());
		Assert.Throws<ArgumentNullException>(() => provider.GetAlias(null!, shape.UnionCases[0]));
		Assert.Throws<ArgumentNullException>(() => provider.GetAlias(shape, null!));
		UnionCaseMappingCollection mappings = [];
		Assert.Throws<ArgumentNullException>(() => mappings.GetAlias(null!, shape.UnionCases[0]));
		Assert.Throws<ArgumentNullException>(() => mappings.GetAlias(shape, null!));
		Assert.Null(mappings.GetAlias(shape, shape.UnionCases[0]));
	}

	private static ITypeShape<T> GetShape<T>()
#if NET
		where T : IShapeable<T> => T.GetTypeShape();
#else
		=> TypeShapeResolver.ResolveDynamicOrThrow<T>();
#endif

	private sealed class ShapeAliasProvider : IUnionTypeAliasProvider
	{
		public int Calls { get; private set; }

		public DerivedTypeIdentifier? GetAlias(IUnionTypeShape unionShape, IUnionCaseShape caseShape)
		{
			Assert.Equal(UnionTypeShapeKind.CSharpUnion, unionShape.UnionKind);
			Assert.Contains(caseShape, unionShape.UnionCases);
			this.Calls++;
			return new(unionShape.Type.Name + "." + caseShape.Name);
		}
	}

#pragma warning disable SA1201 // StyleCop does not yet recognize union declarations.
	[GenerateShape]
	public partial union Scalar(int, string?);

	[GenerateShape]
	public partial union ReorderedScalar(string?, int);

	[GenerateShape]
	public partial union ExpandedScalar(bool, string?, int);

	[GenerateShape]
	public partial union SignInError(InvalidOtpError, UserNotFoundError);
#pragma warning restore SA1201

	[GenerateShape, DerivedTypeShape(typeof(Dog))]
	public partial record Animal(string Name);

	public record Dog(string Name) : Animal(Name);

	public sealed record InvalidOtpError(int RemainingAttempts)
#if NET
		: IError
#endif
	{
		public static string Name => "Shenas.InvalidOtpError";
	}

	public sealed record UserNotFoundError
#if NET
		: IError
#endif
	{
		public static string Name => "Shenas.UserNotFoundError";
	}
}
