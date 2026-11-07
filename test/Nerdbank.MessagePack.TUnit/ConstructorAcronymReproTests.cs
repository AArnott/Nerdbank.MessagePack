// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class ConstructorAcronymReproTests : MessagePackSerializerTestBase
{
	[Test]
	[Arguments(false, false)]
	[Arguments(false, true)]
	[Arguments(true, false)]
	[Arguments(true, true)]
	public async Task AcronymParameterAndInitializerRoundtrip(bool array, bool async)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Required, PerfOverSchemaStability = array };
		IObjectTypeShape shape = Assert.IsAssignableFrom<IObjectTypeShape>(TypeShapeResolver.ResolveDynamicOrThrow<Delta>());
		Assert.NotNull(shape.Constructor);
		Assert.Collection(
			shape.Constructor.Parameters,
			parameter =>
			{
				Assert.Equal("ILDelta", parameter.Name);
				Assert.Equal(ParameterKind.MethodParameter, parameter.Kind);
			});

		Delta result = async ? await this.RoundtripAsync(new Delta(42)) : this.Roundtrip(new Delta(42));
		Assert.Equal(42, result.ILDelta);
		Delta zero = async ? await this.RoundtripAsync(new Delta(0)) : this.Roundtrip(new Delta(0));
		Assert.Equal(0, zero.ILDelta);
		Assert.Contains("\"required\"", this.Serializer.GetJsonSchema<Delta>().ToJsonString());
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void OptionalConstructorDefaultIsUsed(bool array)
	{
		this.Serializer = this.Serializer with { SerializeDefaultValues = SerializeDefaultValuesPolicy.Never, PerfOverSchemaStability = array };
		OptionalDelta? result = this.Roundtrip(new OptionalDelta());
		Assert.NotNull(result);
		Assert.Equal(8, result.ILDelta);
		Assert.Equal(array ? "[]" : "{}", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Contains("\"default\":8", this.Serializer.GetJsonSchema<OptionalDelta>().ToJsonString());
	}

	[Test]
	public void CamelCaseNamingPolicyRoundtrips()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		Assert.Equal(42, this.Roundtrip(new Delta(42)).ILDelta);
		Assert.Contains("\"ilDelta\"", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void ConstructorEstablishedValueIsNotOverwritten(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		Sequence<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		if (array)
		{
			writer.WriteArrayHeader(1);
		}
		else
		{
			writer.WriteMapHeader(1);
			writer.Write("ILDelta");
		}

		writer.Write(-42);
		writer.Flush();
		Assert.Equal(42, this.Serializer.Deserialize<NormalizingDelta>(buffer).ILDelta);
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void ExplicitPropertyNameAndKeyRoundtrip(bool ignoreKeys)
	{
		this.Serializer = this.Serializer with { IgnoreKeyAttributes = ignoreKeys, SerializeDefaultValues = SerializeDefaultValuesPolicy.Required };
		Assert.Equal(42, this.Roundtrip(new KeyedDelta(42)).ILDelta);
		Assert.Contains("\"required\"", this.Serializer.GetJsonSchema<KeyedDelta>().ToJsonString());
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void MissingRequiredConstructorArgumentThrows(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		byte[] empty = [array ? (byte)0x90 : (byte)0x80];
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Deserialize<Delta>(empty));
		Assert.Equal(MessagePackSerializationException.ErrorCode.MissingRequiredProperty, exception.Code);
		Assert.Contains("ILDelta", GetFullMessage(exception));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void RequiredInitializerConflictIsDescriptive(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(new RequiredDelta(42) { ILDelta = 42 }));
		AssertConflictMessage<RequiredDelta>(exception, "ILDelta", "MethodParameter", "MemberInitializer", "serialized key 'ILDelta'");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void IncompatibleArgumentTypeIsDescriptive(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(new IncompatibleDelta("42")));
		AssertConflictMessage<IncompatibleDelta>(exception, "ilDelta", "ILDelta", "System.String", "System.Int32", "types differ");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void AcronymAwareMatchingDoesNotMergeUnmatchedParameters(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Roundtrip(new MultipleParametersDelta(1, 2)));
		Assert.Contains("ILDELTA", GetFullMessage(exception));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void AcronymAwareMatchingDoesNotUseLegacyFallback(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Roundtrip(new AmbiguousDelta(42)));
		Assert.Contains("ildelta", GetFullMessage(exception));
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public void ExactMatchesPreserveCaseDistinctProperties(bool array)
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		CaseDistinctDelta? result = this.Roundtrip(new CaseDistinctDelta(1, 2));
		Assert.NotNull(result);
		Assert.Equal(1, result.ILDelta);
		Assert.Equal(2, result.ilDelta);
		this.Serializer.GetJsonSchema<CaseDistinctDelta>();
	}

	[Test]
	public void NamingPolicyCollisionIsDescriptive()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(new CaseDistinctDelta(1, 2)));
		AssertConflictMessage<CaseDistinctDelta>(exception, "ILDelta", "ilDelta", "serialized key", "PropertyShapeAttribute");
	}

	[Test]
	public void UnmatchedParameterKeyCollisionIsDescriptive()
	{
		this.Serializer = this.Serializer with { PropertyNamingPolicy = new ConstantNamingPolicy() };
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(new UnmatchedParameter(1, 2)));
		AssertConflictMessage<UnmatchedParameter>(exception, "wire", "Value", "serialized key", "MethodParameter");
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
#if NET
	[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test skips when dynamic code is unavailable.")]
#endif
	public void ReflectionProviderRoundtrips(bool array)
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("PolyType's reflection-based shape provider requires runtime code generation.");
		}
#endif

		this.Serializer = this.Serializer with { PerfOverSchemaStability = array };
		ITypeShape<Delta> shape = PolyType.ReflectionProvider.ReflectionTypeShapeProvider.Default.GetTypeShape<Delta>();
		Assert.Equal(42, this.Roundtrip(new Delta(42), shape).ILDelta);
	}

	[Test]
	public void LegacyFirstCharacterOnlyMatchIsNoLongerAccepted()
	{
		MessagePackSerializationException exception = Assert.Throws<MessagePackSerializationException>(
			() => this.Roundtrip(new RenamedParameterDelta(42)));
		Assert.Equal(MessagePackSerializationException.ErrorCode.MissingRequiredProperty, exception.Code);
		Assert.Contains("iLDelta", GetFullMessage(exception));
	}

	[Test]
	public void GetterOnlyPropertyRoundtrips()
	{
		GetterOnlyDelta result = this.Roundtrip(new GetterOnlyDelta(42));
		Assert.Equal(42, result.ILDelta);
	}

	private static void AssertConflictMessage<T>(Exception exception, params string[] fragments)
	{
		string message = GetFullMessage(exception);
		Assert.Contains(typeof(T).FullName!, message);
		Assert.Contains("custom converter", message);
		foreach (string fragment in fragments)
		{
			Assert.Contains(fragment, message);
		}

		Assert.DoesNotContain("duplicate key found", message);
	}

	[GenerateShape]
	public readonly partial struct Delta
	{
		public Delta(int ilDelta) => this.ILDelta = ilDelta;

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public readonly partial struct RenamedParameterDelta
	{
		public RenamedParameterDelta(int iLDelta) => this.ILDelta = iLDelta;

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public readonly partial struct GetterOnlyDelta
	{
		public GetterOnlyDelta(int ilDelta) => this.ILDelta = ilDelta;

		public int ILDelta { get; }
	}

	[GenerateShape]
	public readonly partial struct NormalizingDelta
	{
		public NormalizingDelta(int ilDelta) => this.ILDelta = Math.Abs(ilDelta);

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public readonly partial struct KeyedDelta
	{
		public KeyedDelta(int ilDelta) => this.ILDelta = ilDelta;

		[Key(2)]
		[PropertyShape(Name = "ilDelta")]
		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public partial class OptionalDelta
	{
		public OptionalDelta(int ilDelta = 8) => this.ILDelta = ilDelta;

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public partial class RequiredDelta
	{
		public RequiredDelta(int ilDelta) => this.ILDelta = ilDelta;

		public required int ILDelta { get; init; }
	}

	[GenerateShape]
	public partial class IncompatibleDelta
	{
		public IncompatibleDelta(string ilDelta) => this.ILDelta = int.Parse(ilDelta);

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public partial class MultipleParametersDelta
	{
#pragma warning disable SA1313 // Deliberately test parameter names differing in case.
		public MultipleParametersDelta(int ilDelta, int ILDELTA) => this.ILDelta = ilDelta + ILDELTA;
#pragma warning restore SA1313

		public int ILDelta { get; init; }
	}

	[GenerateShape]
	public partial class AmbiguousDelta
	{
		public AmbiguousDelta(int ildelta) => this.ILDelta = this.IlDelta = ildelta;

		public int ILDelta { get; init; }

		public int IlDelta { get; init; }
	}

	[GenerateShape]
	public partial class CaseDistinctDelta
	{
#pragma warning disable SA1313 // Deliberately test exact, case-distinct names.
		public CaseDistinctDelta(int ILDelta, int ilDelta)
		{
			this.ILDelta = ILDelta;
			this.ilDelta = ilDelta;
		}
#pragma warning restore SA1313

		public int ILDelta { get; }

#pragma warning disable SA1300 // Deliberately test properties differing only in case.
		public int ilDelta { get; }
#pragma warning restore SA1300
	}

	[GenerateShape]
	public partial class UnmatchedParameter
	{
		public UnmatchedParameter(int wire, int value) => this.Value = wire + value;

		public int Value { get; }
	}

	private class ConstantNamingPolicy : MessagePackNamingPolicy
	{
		public override string ConvertName(string name) => "wire";
	}
}
