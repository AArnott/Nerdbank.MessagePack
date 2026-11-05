// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PolyType.Tests;

/// <summary>
/// Tests for serialization of C# union types (the C# 15 <c>union</c> feature).
/// </summary>
/// <remarks>
/// <para>
/// Some tests use union types declared in the PolyType.TestCases package, which exercise consuming unions from another assembly.
/// </para>
/// <para>
/// The union types declared in this file are source generated, and include both <c>union</c> declarations and hand-authored types that follow the C# union member pattern.
/// On frameworks that predate .NET 11, <see cref="UnionAttribute"/> is supplied by a polyfill in the PolyType.TestCases package.
/// </para>
/// </remarks>
public partial class CSharpUnionTests : MessagePackSerializerTestBase
{
	[Test]
	[Arguments("instance")]
	[Arguments("type")]
	[Arguments("factory")]
	public async Task CustomConverterOverridesUnion(string registration)
	{
		this.Serializer = registration switch
		{
			"instance" => this.Serializer with { Converters = [new IntOrBoolConverter()] },
			"type" => this.Serializer with { ConverterTypes = [typeof(IntOrBoolConverter)] },
			"factory" => this.Serializer with { ConverterFactories = [new IntOrBoolConverterFactory()] },
			_ => throw new ArgumentOutOfRangeException(nameof(registration)),
		};

		IntOrBool value = new(42);
		Assert.Equal(42, this.Roundtrip(value).Value);
		Assert.Equal("42", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.Equal(42, (await this.RoundtripAsync(value)).Value);
		Assert.Equal("42", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task ValueCase(bool async)
	{
		CSharpScalarUnion value = new(42);
		CSharpScalarUnion result = async ? await this.RoundtripAsync(value) : this.Roundtrip(value);
		Assert.Equal(42, result.Value);
		Assert.Equal("42", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task ReferenceCase(bool async)
	{
		CSharpScalarUnion value = new("hi");
		CSharpScalarUnion result = async ? await this.RoundtripAsync(value) : this.Roundtrip(value);
		Assert.Equal("hi", result.Value);
		Assert.Equal("\"hi\"", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void NullPayload()
	{
		// A null payload selects the first nullable case.
		CSharpScalarUnion result = this.Roundtrip(default(CSharpScalarUnion));
		Assert.Null(result.Value);
		Assert.Equal("null", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void ClassUnion_NullReference()
	{
		Assert.Null(this.Roundtrip<CSharpClassUnion>(null));
		Assert.Equal("null", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.True(this.DataMatchesSchema<CSharpClassUnion>(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void ObjectPayload()
	{
		CSharpHierarchyUnion result = this.Roundtrip(new CSharpHierarchyUnion(new CSharpDog("Rover", 10)));
		Assert.Equal(new CSharpDog("Rover", 10), result.Value);
		Assert.Equal("""["CSharpDog",{"BarkVolume":10,"Name":"Rover"}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void Recursive()
	{
		CSharpRecursiveUnion result = this.Roundtrip(new CSharpRecursiveUnion(new CSharpRecursiveUnion(new CSharpRecursiveUnion(true))));
		Assert.Equal("""["CSharpRecursiveUnion",["CSharpRecursiveUnion",true]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		CSharpRecursiveUnion inner = Assert.IsType<CSharpRecursiveUnion>(Assert.IsType<CSharpRecursiveUnion>(result.Value).Value);
		Assert.Equal(true, inner.Value);
	}

	[Test]
	public void NoMatchingCase_Throws()
	{
		// default(CSharpAmbiguousNumericUnion) holds a null payload, but no case admits null.
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(default(CSharpAmbiguousNumericUnion), this.TimeoutToken));
		this.Logger.LogInformation(ex.ToString());
		Assert.IsAssignableFrom<InvalidOperationException>(ex.GetBaseException());
	}

	[Test]
	public void PerfOverSchemaStability_UsesIntegerTags()
	{
		this.Serializer = this.Serializer with { PerfOverSchemaStability = true };
		CSharpHierarchyUnion result = this.Roundtrip(new CSharpHierarchyUnion(new CSharpDog("Rover", 10)));
		Assert.Equal(new CSharpDog("Rover", 10), result.Value);

		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal(MessagePackType.Integer, reader.NextMessagePackType);
	}

	[Test]
	public void DiscriminatorObjects()
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = true };
		CSharpScalarUnion result = this.Roundtrip(new CSharpScalarUnion(42));
		Assert.Equal(42, result.Value);
		Assert.Equal("42", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test]
	public void Schema()
	{
		this.Logger.LogInformation(SchemaToString(this.Serializer.GetJsonSchema<CSharpHierarchyUnion>()));
		Assert.True(this.DataMatchesSchema<CSharpHierarchyUnion>(new(this.Serializer.Serialize(new CSharpHierarchyUnion(new CSharpDog("Rover", 10)), this.TimeoutToken))));
		Assert.True(this.DataMatchesSchema<CSharpHierarchyUnion>(new(this.Serializer.Serialize(new CSharpHierarchyUnion(42), this.TimeoutToken))));
	}

	[Test]
	public void DefaultStructuralEquality()
	{
		IEqualityComparer<CSharpHierarchyUnion> comparer = StructuralEqualityComparer.GetDefault<CSharpHierarchyUnion>();
		Assert.True(comparer.Equals(new(new CSharpDog("Rover", 10)), new(new CSharpDog("Rover", 10))));
		Assert.False(comparer.Equals(new(new CSharpDog("Rover", 10)), new(new CSharpDog("Rover", 11))));
		Assert.False(comparer.Equals(new(new CSharpDog("Rover", 10)), new(42)));
		Assert.Equal(comparer.GetHashCode(new(new CSharpDog("Rover", 10))), comparer.GetHashCode(new(new CSharpDog("Rover", 10))));
	}

	[Test]
	public void HashCollisionResistantEquality()
	{
		IEqualityComparer<CSharpRecursiveUnion> comparer = StructuralEqualityComparer.GetHashCollisionResistant<CSharpRecursiveUnion>();
		Assert.True(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(new CSharpRecursiveUnion(true))));
		Assert.False(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(new CSharpRecursiveUnion(false))));
		Assert.False(comparer.Equals(new(new CSharpRecursiveUnion(true)), new(true)));
		Assert.Equal(comparer.GetHashCode(new(new CSharpRecursiveUnion(true))), comparer.GetHashCode(new(new CSharpRecursiveUnion(true))));
	}

	[Test]
	public void ObjectPayloads()
	{
		Pet result = this.Roundtrip(new Pet(new Dog("Rover", 10)));
		Assert.Equal(new Dog("Rover", 10), result.Value);
		Assert.Equal("""["Dog",{"Name":"Rover","BarkVolume":10}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));

		result = this.Roundtrip(new Pet(new Cat("Whiskers", 13)));
		Assert.Equal(new Cat("Whiskers", 13), result.Value);
	}

	[Test]
	public void UnionAsProperty()
	{
		Household? result = this.Roundtrip(new Household { Pet = new Pet(new Cat("Whiskers", 13)) });
		Assert.Equal(new Cat("Whiskers", 13), result?.Pet?.Value);
		Assert.Equal("""{"Pet":["Cat",{"Name":"Whiskers","MeowPitch":13}]}""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));

		result = this.Roundtrip(new Household { Pet = null });
		Assert.NotNull(result);
		Assert.Null(result.Pet);
	}

	[Test]
	public void RecursiveDeclaredUnion()
	{
		BoolOrNested result = this.Roundtrip(new BoolOrNested(new BoolOrNested(new BoolOrNested(true))));
		Assert.Equal("""["BoolOrNested",["BoolOrNested",true]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		BoolOrNested inner = Assert.IsType<BoolOrNested>(Assert.IsType<BoolOrNested>(result.Value).Value);
		Assert.Equal(true, inner.Value);
	}

	[Test, MatrixDataSource]
	public async Task RecursiveDeclaredUnion_DepthLimit(bool async, bool discriminatorObjects, bool exceedsLimit)
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = discriminatorObjects };
		int depth = this.Serializer.StartingContext.MaxDepth + (exceedsLimit ? 1 : 0);
		Sequence<byte> buffer = new();
		MessagePackWriter writer = new(buffer);
		for (int i = 0; i < depth; i++)
		{
			if (discriminatorObjects)
			{
				writer.WriteMapHeader(1);
			}
			else
			{
				writer.WriteArrayHeader(2);
			}

			writer.Write(i == depth - 1 ? "Boolean" : "BoolOrNested");
		}

		writer.Write(true);
		writer.Flush();

		if (exceedsLimit)
		{
			MessagePackSerializationException ex = async
				? await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.DeserializeAsync<BoolOrNested>(new MemoryStream(buffer.AsReadOnlySequence.ToArray()), this.TimeoutToken).AsTask())
				: Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<BoolOrNested>(buffer.AsReadOnlySequence, this.TimeoutToken));
			Assert.Contains("Exceeded maximum depth", GetFullMessage(ex));
		}
		else
		{
			BoolOrNested result = async
				? await this.Serializer.DeserializeAsync<BoolOrNested>(new MemoryStream(buffer.AsReadOnlySequence.ToArray()), this.TimeoutToken)
				: this.Serializer.Deserialize<BoolOrNested>(buffer.AsReadOnlySequence, this.TimeoutToken);
			for (int i = 1; i < depth; i++)
			{
				result = Assert.IsType<BoolOrNested>(result.Value);
			}

			Assert.Equal(true, result.Value);
		}
	}

	[Test, MatrixDataSource]
	public async Task RecursiveDeclaredUnion_SerializeDepthLimit(bool async, bool exceedsLimit)
	{
		int depth = this.Serializer.StartingContext.MaxDepth + (exceedsLimit ? 1 : 0);
		BoolOrNested value = new(true);
		for (int i = 1; i < depth; i++)
		{
			value = new(value);
		}

		using MemoryStream stream = new();
		if (exceedsLimit)
		{
			if (async)
			{
				await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.SerializeAsync(stream, value, this.TimeoutToken).AsTask());
			}
			else
			{
				Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(value, this.TimeoutToken));
			}
		}
		else if (async)
		{
			await this.Serializer.SerializeAsync(stream, value, this.TimeoutToken);
		}
		else
		{
			this.Serializer.Serialize(value, this.TimeoutToken);
		}
	}

	[Test]
	public void StructWithTryGetValue()
	{
		Assert.Equal(5, this.Roundtrip(new NumberOrText(5)).Value);
		Assert.Equal("five", this.Roundtrip(new NumberOrText("five")).Value);
		Assert.Null(this.Roundtrip(new NumberOrText((string?)null)).Value);
	}

	[Test]
	public void DeclaredUnion_NoMatchingCase_Throws()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize(default(IntOrBool), this.TimeoutToken));
		this.Logger.LogInformation(ex.ToString());
		Assert.IsAssignableFrom<InvalidOperationException>(ex.GetBaseException());
	}

	[Test]
	public void ReferencePreservation()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		Dog dog = new("Rover", 10);
		PetClassUnion pet = new(dog);
		ThreePets? result = this.Roundtrip(new ThreePets { First = pet, Second = new PetClassUnion(dog), Third = pet });
		Assert.NotNull(result?.First);
		Assert.Equal(dog, result.First.Value);
		Assert.Same(result.First, result.Third);
		Assert.NotSame(result.First, result.Second);
		Assert.Same(result.First.Value, result.Second?.Value);
	}

	[Test, MatrixDataSource]
	public async Task IntegerCasesPreserveWidthAndSign(bool async)
	{
		(object Value, byte Code, int Length)[] cases =
		[
			((sbyte)8, MessagePackCode.Int8, 2), (sbyte.MinValue, MessagePackCode.Int8, 2), (sbyte.MaxValue, MessagePackCode.Int8, 2),
			((byte)8, MessagePackCode.UInt8, 2), (byte.MinValue, MessagePackCode.UInt8, 2), (byte.MaxValue, MessagePackCode.UInt8, 2),
			((short)8, MessagePackCode.Int16, 3), (short.MinValue, MessagePackCode.Int16, 3), (short.MaxValue, MessagePackCode.Int16, 3),
			((ushort)8, MessagePackCode.UInt16, 3), (ushort.MinValue, MessagePackCode.UInt16, 3), (ushort.MaxValue, MessagePackCode.UInt16, 3),
			(8, MessagePackCode.Int32, 5), (int.MinValue, MessagePackCode.Int32, 5), (int.MaxValue, MessagePackCode.Int32, 5),
			(8U, MessagePackCode.UInt32, 5), (uint.MinValue, MessagePackCode.UInt32, 5), (uint.MaxValue, MessagePackCode.UInt32, 5),
			(8L, MessagePackCode.Int64, 9), (long.MinValue, MessagePackCode.Int64, 9), (long.MaxValue, MessagePackCode.Int64, 9),
			(8UL, MessagePackCode.UInt64, 9), (ulong.MinValue, MessagePackCode.UInt64, 9), (ulong.MaxValue, MessagePackCode.UInt64, 9),
		];
		foreach ((object value, byte code, int length) in cases)
		{
			NativeIntegers input = value switch
			{
				sbyte v => new(v),
				byte v => new(v),
				short v => new(v),
				ushort v => new(v),
				int v => new(v),
				uint v => new(v),
				long v => new(v),
				ulong v => new(v),
				_ => throw new InvalidOperationException(),
			};
			NativeIntegers result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.Equal(value.GetType(), result.Value?.GetType());
			Assert.Equal(value, result.Value);
			Assert.Equal(code, this.lastRoundtrippedMsgpack.First.Span[0]);
			Assert.Equal(length, this.lastRoundtrippedMsgpack.Length);
		}
	}

	[Test, MatrixDataSource]
	public async Task SingleIntegerCaseUsesShortestEncoding(bool async, bool aliasedOtherInteger)
	{
		if (aliasedOtherInteger)
		{
			this.Serializer = this.Serializer with
			{
				UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => c == typeof(short) ? new("short") : null),
			};
			IntOrShort input = new(8);
			IntOrShort result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.IsType<int>(result.Value);
		}
		else
		{
			IntOrBool input = new(8);
			IntOrBool result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.IsType<int>(result.Value);
		}

		Assert.Equal(new byte[] { 8 }, this.lastRoundtrippedMsgpack.ToArray());
	}

	[Test, MatrixDataSource]
	public async Task IntegerInputUsesExactEncodingThenSmallestFittingCase(bool async)
	{
		(byte[] Bytes, object Expected)[] cases =
		[
			([8], (short)8),
			([MessagePackCode.UInt16, 0, 8], (short)8),
			([MessagePackCode.Int32, 0, 0, 0, 8], 8),
			([MessagePackCode.UInt32, 0, 0, 128, 0], 32768),
		];
		foreach ((byte[] bytes, object expected) in cases)
		{
			IntOrShort result = async
				? await this.Serializer.DeserializeAsync<IntOrShort>(new MemoryStream(bytes), this.TimeoutToken)
				: this.Serializer.Deserialize<IntOrShort>(bytes, this.TimeoutToken);
			Assert.Equal(expected.GetType(), result.Value?.GetType());
			Assert.Equal(expected, result.Value);
		}

		byte[] unsigned16 = [MessagePackCode.UInt16, 0, 8];
		NativeIntegers unsignedResult = async
			? await this.Serializer.DeserializeAsync<NativeIntegers>(new MemoryStream(unsigned16), this.TimeoutToken)
			: this.Serializer.Deserialize<NativeIntegers>(unsigned16, this.TimeoutToken);
		Assert.Equal((ushort)8, Assert.IsType<ushort>(unsignedResult.Value));
	}

	[Test, MatrixDataSource]
	public async Task CharacterCaseUsesNativeIntegerEncoding(bool async)
	{
		CharOrInt input = new('a');
		CharOrInt result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Equal('a', Assert.IsType<char>(result.Value));
		Assert.Equal(new byte[] { MessagePackCode.UInt16, 0, 97 }, this.lastRoundtrippedMsgpack.ToArray());
	}

	[Test]
	public void CharacterAndUInt16RequireAlias()
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Roundtrip(new CharOrUInt16('a')));
		Assert.Contains("same integer encoding", GetFullMessage(ex));
	}

	[Test, MatrixDataSource]
	public async Task FloatingPointCasesUseNativeCodes(bool async)
	{
		foreach (float number in new[] { 8f, -0f, float.NaN, float.PositiveInfinity, float.MinValue, float.MaxValue })
		{
			FloatOrDouble input = new(number);
			FloatOrDouble result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.Equal(number, Assert.IsType<float>(result.Value));
			Assert.Equal(MessagePackCode.Float32, this.lastRoundtrippedMsgpack.First.Span[0]);
		}

		foreach (double number in new[] { 8d, -0d, double.NaN, double.NegativeInfinity, double.MinValue, double.MaxValue })
		{
			FloatOrDouble input = new(number);
			FloatOrDouble result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.Equal(number, Assert.IsType<double>(result.Value));
			Assert.Equal(MessagePackCode.Float64, this.lastRoundtrippedMsgpack.First.Span[0]);
		}
	}

	[Test, MatrixDataSource]
	public async Task NullPayloadUsesCaseEncoding(bool async, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = discriminatorObjects };
		TextOrApple input = new((string)null!);
		TextOrApple result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Null(result.Value);
		Assert.Equal(new byte[] { MessagePackCode.Nil }, this.lastRoundtrippedMsgpack.ToArray());
		Assert.True(this.DataMatchesSchema<TextOrApple>(this.lastRoundtrippedMsgpack));

		// This union's null payload selects Apple, since it is the first nullable case.
		AppleOrText apple = new((Apple)null!);
		AppleOrText appleResult = async ? await this.RoundtripAsync(apple) : this.Roundtrip(apple);
		Assert.Null(appleResult.Value);
		Assert.Equal(discriminatorObjects ? """{"Apple":null}""" : """["Apple",null]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.True(this.DataMatchesSchema<AppleOrText>(this.lastRoundtrippedMsgpack));

		TextOrApple nonNull = new(new Apple(3));
		result = async ? await this.RoundtripAsync(nonNull) : this.Roundtrip(nonNull);
		Assert.Equal(new Apple(3), result.Value);
	}

	[Test, MatrixDataSource]
	public async Task CollectionPayloadHasNoHeader(bool async, bool dictionary, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = discriminatorObjects };
		if (dictionary)
		{
			MapOrBool input = new(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 });
			MapOrBool result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.Equal(2, Assert.IsType<Dictionary<string, int>>(result.Value).Count);
			Assert.Equal("""{"a":1,"b":2}""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<MapOrBool>(this.lastRoundtrippedMsgpack));
		}
		else
		{
			ArrayOrBool input = new(new[] { 1, 2 });
			ArrayOrBool result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.Equal(new[] { 1, 2 }, Assert.IsType<int[]>(result.Value));
			Assert.Equal("[1,2]", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<ArrayOrBool>(this.lastRoundtrippedMsgpack));
		}
	}

	[Test, MatrixDataSource]
	public async Task NullCollectionPayloadUsesNil(bool async)
	{
		NullableArrayOrBool input = new((int[]?)null);
		NullableArrayOrBool result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Null(result.Value);
		Assert.Equal(new byte[] { MessagePackCode.Nil }, this.lastRoundtrippedMsgpack.ToArray());
		Assert.True(this.DataMatchesSchema<NullableArrayOrBool>(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public void AmbiguousCollectionsRejected(bool differentWireTypes)
	{
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() =>
		{
			if (differentWireTypes)
			{
				this.Serializer.Serialize(new ArrayOrMap(new[] { 1 }));
			}
			else
			{
				this.Serializer.Serialize(new TwoArrays(new[] { 1 }));
			}
		});
		Assert.Contains("more than one untagged collection", GetFullMessage(ex));
	}

	[Test, MatrixDataSource]
	public async Task CollectionAndObjectUseDistinctEncodings(bool async, bool discriminatorObjects, bool dictionary, bool explicitAlias)
	{
		this.Serializer = this.Serializer with
		{
			UseDiscriminatorObjects = discriminatorObjects,
			UnionTypeAliasProvider = explicitAlias
				? new DelegateUnionTypeAliasProvider((u, c) => c == typeof(Apple) ? new("fruit") : null)
				: null,
		};
		string alias = explicitAlias ? "fruit" : "Apple";
		if (!dictionary)
		{
			ArrayOrApple apple = new(new Apple(1));
			Assert.Equal(new Apple(1), (async ? await this.RoundtripAsync(apple) : this.Roundtrip(apple)).Value);
			Assert.Equal($"{{\"{alias}\":{{\"Seeds\":1}}}}", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<ArrayOrApple>(this.lastRoundtrippedMsgpack));
			ArrayOrApple array = new(new[] { 1, 2 });
			ArrayOrApple result = async ? await this.RoundtripAsync(array) : this.Roundtrip(array);
			Assert.Equal(new[] { 1, 2 }, Assert.IsType<int[]>(result.Value));
			Assert.Equal("[1,2]", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<ArrayOrApple>(this.lastRoundtrippedMsgpack));
		}
		else
		{
			MapOrApple apple = new(new Apple(1));
			Assert.Equal(new Apple(1), (async ? await this.RoundtripAsync(apple) : this.Roundtrip(apple)).Value);
			Assert.Equal($"[\"{alias}\",{{\"Seeds\":1}}]", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<MapOrApple>(this.lastRoundtrippedMsgpack));
			MapOrApple map = new(new Dictionary<string, int> { ["Apple"] = 2 });
			MapOrApple result = async ? await this.RoundtripAsync(map) : this.Roundtrip(map);
			Assert.Single(Assert.IsType<Dictionary<string, int>>(result.Value));
			Assert.Equal("""{"Apple":2}""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
			Assert.True(this.DataMatchesSchema<MapOrApple>(this.lastRoundtrippedMsgpack));
		}
	}

	[Test, MatrixDataSource]
	public async Task BinaryCollectionRetainsConfiguredWrapper(bool async, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = discriminatorObjects };
		BinaryOrApple apple = new(new Apple(1));
		Assert.Equal(new Apple(1), (async ? await this.RoundtripAsync(apple) : this.Roundtrip(apple)).Value);
		Assert.Equal(discriminatorObjects ? """{"Apple":{"Seeds":1}}""" : """["Apple",{"Seeds":1}]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.True(this.DataMatchesSchema<BinaryOrApple>(this.lastRoundtrippedMsgpack));
		BinaryOrApple bytes = new(new byte[] { 1, 2 });
		BinaryOrApple result = async ? await this.RoundtripAsync(bytes) : this.Roundtrip(bytes);
		Assert.Equal(new byte[] { 1, 2 }, Assert.IsType<byte[]>(result.Value));
		Assert.Equal(MessagePackType.Binary, new MessagePackReader(this.lastRoundtrippedMsgpack).NextMessagePackType);
	}

	[Test, MatrixDataSource]
	public async Task InvalidNativeInputRejected(bool async, bool nil)
	{
		byte[] bytes = nil ? [MessagePackCode.Nil] : [MessagePackCode.UInt64, 255, 255, 255, 255, 255, 255, 255, 255];
		if (async)
		{
			await Assert.ThrowsAsync<MessagePackSerializationException>(() => this.Serializer.DeserializeAsync<IntOrShort>(new MemoryStream(bytes)).AsTask());
		}
		else
		{
			Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize<IntOrShort>(bytes));
		}
	}

	[Test, MatrixDataSource]
	public async Task ExplicitCollectionAliasesRemoveAmbiguity(bool async, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with
		{
			UseDiscriminatorObjects = discriminatorObjects,
			UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => new(c == typeof(int[]) ? "array" : "list")),
		};
		TwoArrays input = new(new[] { 1, 2 });
		TwoArrays result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Equal(new[] { 1, 2 }, Assert.IsType<int[]>(result.Value));
		Assert.Equal(discriminatorObjects ? """{"array":[1,2]}""" : """["array",[1,2]]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		input = new(new List<int> { 1, 2 });
		result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Equal(new[] { 1, 2 }, Assert.IsType<List<int>>(result.Value));
	}

	[Test, MatrixDataSource]
	public async Task NativeReferencePreservation(bool async, bool binary)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		if (binary)
		{
			byte[] bytes = [1, 2, 3];
			NativeReferences input = new() { Binary = [new(bytes), new(bytes)] };
			NativeReferences? result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.NotNull(result);
			Assert.Equal(bytes, Assert.IsType<byte[]>(result.Binary[0].Value));
			Assert.Same(result.Binary[0].Value, result.Binary[1].Value);
		}
		else
		{
			string text = new('a', 100);
			NativeReferences input = new() { Text = [new(text), new(text)] };
			NativeReferences? result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
			Assert.NotNull(result);
			Assert.Equal(text, result.Text[0].Value);
			Assert.Same(result.Text[0].Value, result.Text[1].Value);
		}
	}

	[Test, MatrixDataSource]
	public async Task AmbiguousNullPayloadsUseFirstUntaggedReferenceCase(bool async, bool explicitAlias)
	{
		if (explicitAlias)
		{
			this.Serializer = this.Serializer with
			{
				UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => c == typeof(int[]) ? new("array") : null),
			};
		}

		TextOrArray text = new("hi");
		Assert.Equal("hi", (async ? await this.RoundtripAsync(text) : this.Roundtrip(text)).Value);
		Assert.Equal("\"hi\"", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		TextOrArray array = new(new[] { 1 });
		Assert.Equal(new[] { 1 }, Assert.IsType<int[]>((async ? await this.RoundtripAsync(array) : this.Roundtrip(array)).Value));

		byte[] nil = [MessagePackCode.Nil];
		TextOrArray result = async
			? await this.Serializer.DeserializeAsync<TextOrArray>(new MemoryStream(nil), this.TimeoutToken)
			: this.Serializer.Deserialize<TextOrArray>(nil, this.TimeoutToken);
		Assert.Null(result.Value);
		Assert.Null((async ? await this.RoundtripAsync(result) : this.Roundtrip(result)).Value);
		Assert.Equal(nil, this.lastRoundtrippedMsgpack.ToArray());
	}

	[Test, MatrixDataSource]
	public async Task MultipleNativeReferenceCasesPreserveReferences(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		string text = new('a', 100);
		int[] numbers = [1, 2];
		NativeReferences input = new() { Mixed = [new(text), new(numbers), new(text), new(numbers)] };
		NativeReferences? result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.NotNull(result);
		Assert.Equal(text, Assert.IsType<string>(result.Mixed[0].Value));
		Assert.Equal(numbers, Assert.IsType<int[]>(result.Mixed[1].Value));
		Assert.Same(result.Mixed[0].Value, result.Mixed[2].Value);
		Assert.Same(result.Mixed[1].Value, result.Mixed[3].Value);
	}

	[Test, MatrixDataSource]
	public async Task NilSelectsFirstUnaliasedReferenceConstructor(bool async, bool aliasFirstCase)
	{
		if (aliasFirstCase)
		{
			this.Serializer = this.Serializer with
			{
				UnionTypeAliasProvider = new DelegateUnionTypeAliasProvider((u, c) => c == typeof(string) ? new("text") : null),
			};
		}

		byte[] nil = [MessagePackCode.Nil];
		NullableReferences result = async
			? await this.Serializer.DeserializeAsync<NullableReferences>(new MemoryStream(nil), this.TimeoutToken)
			: this.Serializer.Deserialize<NullableReferences>(nil, this.TimeoutToken);
		Assert.Null(result.Value);
		Assert.Equal(aliasFirstCase ? typeof(int[]) : typeof(string), result.SelectedType);
	}

	[Test]
	public void NullPayloadIdentity()
	{
		TextOrApple text = new((string?)null);
		TextOrApple apple = new((Apple?)null);
		Assert.Equal(text, apple);
		Assert.Null(text.Value);
		Assert.Null(apple.Value);

		NullableReferences handwrittenText = new((string?)null);
		NullableReferences handwrittenArray = new((int[]?)null);
		Assert.Null(handwrittenText.Value);
		Assert.Null(handwrittenArray.Value);
		Assert.NotEqual(handwrittenText.SelectedType, handwrittenArray.SelectedType);
		Assert.NotEqual(handwrittenText, handwrittenArray);
	}

	[Test]
	public void OptionalNullPayloadPresence()
	{
		TextOrApple? absent = default;
		TextOrApple? present = new TextOrApple((string?)null);
		Assert.False(absent.HasValue);
		Assert.True(present.HasValue);
		Assert.Null(present.Value.Value);
		Assert.NotNull((object?)present);
	}

	[Test]
	public void HandwrittenOptionalNullPayloadPresence()
	{
		NullableReferences? absent = default;
		NullableReferences? present = new NullableReferences((int[]?)null);
		Assert.False(absent.HasValue);
		Assert.True(present.HasValue);
		Assert.Null(present.Value.Value);
		Assert.Equal(typeof(int[]), present.Value.SelectedType);
		Assert.NotNull((object?)present);
	}

	[Test, MatrixDataSource]
	public async Task HandwrittenNullPayloadRoundtrip(bool async, bool array)
	{
		NullableReferences input = array ? new((int[]?)null) : new((string?)null);
		NullableReferences result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Null(result.Value);
		Assert.Equal(typeof(string), result.SelectedType);
		Assert.Equal("null", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task CustomPrimitiveConverterRetainsDiscriminator(bool async)
	{
		this.Serializer = this.Serializer with { Converters = [new StringEncodedIntConverter()] };
		CSharpScalarUnion input = new(8);
		CSharpScalarUnion result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.Equal(8, result.Value);
		Assert.Equal("""["Int32","8"]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task ClassUnionNullPayload(bool async, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with { UseDiscriminatorObjects = discriminatorObjects };
		NullableClassUnion input = new((string?)null);
		NullableClassUnion? result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.NotNull(result);
		Assert.Null(result.Value);
		Assert.Equal(discriminatorObjects ? """{"String":null}""" : """["String",null]""", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		Assert.True(this.DataMatchesSchema<NullableClassUnion>(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task ClassUnionNativePayload(bool async)
	{
		NullableClassUnion? result = async ? await this.RoundtripAsync(new NullableClassUnion("text")) : this.Roundtrip(new NullableClassUnion("text"));
		Assert.NotNull(result);
		Assert.Equal("text", result.Value);
		Assert.Equal("\"text\"", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
		result = async ? await this.RoundtripAsync<NullableClassUnion>(null) : this.Roundtrip<NullableClassUnion>(null);
		Assert.Null(result);
		Assert.Equal("null", this.Serializer.ConvertToJson(this.lastRoundtrippedMsgpack));
	}

	[Test, MatrixDataSource]
	public async Task RecursiveArrayReferencePreservation(bool async)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		Tree[] children = [new(1)];
		Tree[] input = [new(children), new(children)];
		Tree[]? result = async ? await this.RoundtripAsync<Tree[], TreeArrayShape>(input) : this.Roundtrip<Tree[], TreeArrayShape>(input);
		Assert.NotNull(result);
		Assert.Same(result[0].Value, result[1].Value);
	}

	[Test]
	public void RecursiveClassUnionNullSchema()
	{
		Assert.True(this.DataMatchesSchema<NullableClassUnion>(new(this.Serializer.Serialize<NullableClassUnion>(null, this.TimeoutToken))));
		Assert.True(this.DataMatchesSchema<NullableClassUnion>(new(this.Serializer.Serialize(new NullableClassUnion(new NullableClassUnion((string?)null)), this.TimeoutToken))));
		Assert.True(this.DataMatchesSchema<NullableUnionHolder>(new(this.Serializer.Serialize(new NullableUnionHolder(), this.TimeoutToken))));
	}

	[Test, MatrixDataSource]
	public async Task ClassUnionsSharingNativePayload(bool async, bool discriminatorObjects)
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles, UseDiscriminatorObjects = discriminatorObjects };
		string text = new('x', 30);
		ClassUnionReferences input = new() { First = new(text), Second = new(text) };
		ClassUnionReferences? result = async ? await this.RoundtripAsync(input) : this.Roundtrip(input);
		Assert.NotNull(result?.First);
		Assert.NotNull(result.Second);
		Assert.NotSame(result.First, result.Second);
		Assert.Same(result.First.Value, result.Second.Value);
	}

	public class IntOrBoolConverter : MessagePackConverter<IntOrBool>
	{
		public override IntOrBool Read(ref MessagePackReader reader, SerializationContext context)
			=> reader.NextMessagePackType == MessagePackType.Boolean ? new(reader.ReadBoolean()) : new(reader.ReadInt32());

		public override void Write(ref MessagePackWriter writer, in IntOrBool value, SerializationContext context)
		{
			switch (value.Value)
			{
				case int number:
					writer.Write(number);
					break;
				case bool boolean:
					writer.Write(boolean);
					break;
				default:
					throw new MessagePackSerializationException("The union has no matching case.");
			}
		}
	}

	public class StringEncodedIntConverter : MessagePackConverter<int>
	{
		public override int Read(ref MessagePackReader reader, SerializationContext context) => int.Parse(reader.ReadString()!, System.Globalization.CultureInfo.InvariantCulture);

		public override void Write(ref MessagePackWriter writer, in int value, SerializationContext context) => writer.Write(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
	}

#pragma warning disable SA1201 // StyleCop does not yet recognize union declarations.
	[GenerateShape]
	public partial union Pet(Cat, Dog);

	/// <summary>A union whose default value matches no case.</summary>
	[GenerateShape]
	public partial union IntOrBool(int, bool);

	[GenerateShape]
	public partial union BoolOrNested(bool, BoolOrNested);

	[GenerateShape]
	public partial union NativeIntegers(sbyte, byte, short, ushort, int, uint, long, ulong);

	[GenerateShape]
	public partial union IntOrShort(int, short);

	[GenerateShape]
	public partial union CharOrInt(char, int);

	[GenerateShape]
	public partial union CharOrUInt16(char, ushort);

	[GenerateShape]
	public partial union FloatOrDouble(float, double);

	[GenerateShape]
	public partial union TextOrApple(string?, Apple?);

	[GenerateShape]
	public partial union AppleOrText(Apple?, string?);

	[GenerateShape]
	public partial union ArrayOrBool(int[], bool);

	[GenerateShape]
	public partial union NullableArrayOrBool(int[]?, bool);

	[GenerateShape]
	public partial union MapOrBool(Dictionary<string, int>, bool);

	[GenerateShape]
	public partial union TwoArrays(int[], List<int>);

	[GenerateShape]
	public partial union ArrayOrMap(int[], Dictionary<string, int>);

	[GenerateShape]
	public partial union ArrayOrApple(int[], Apple);

	[GenerateShape]
	public partial union MapOrApple(Dictionary<string, int>, Apple);

	[GenerateShape]
	public partial union BinaryOrBool(byte[], bool);

	[GenerateShape]
	public partial union BinaryOrApple(byte[], Apple);

	[GenerateShape]
	public partial union TextOrArray(string?, int[]?);

	[GenerateShape]
	public partial union Tree(int, Tree[]);
#pragma warning restore SA1201

	/// <summary>A hand-authored struct union that implements the non-boxing access pattern.</summary>
	[Union, GenerateShape]
	public readonly partial struct NumberOrText
	{
		private readonly int number;
		private readonly string? text;
		private readonly bool isText;

		public NumberOrText(int value) => this.number = value;

		public NumberOrText(string? value)
		{
			this.text = value;
			this.isText = true;
		}

		public object? Value => this.isText ? this.text : this.number;

		public bool HasValue => !this.isText || this.text is not null;

		public bool TryGetValue(out int value)
		{
			value = this.number;
			return !this.isText;
		}

		public bool TryGetValue([NotNullWhen(true)] out string? value)
		{
			value = this.text;
			return this.isText && this.text is not null;
		}
	}

	[Union, GenerateShape]
	public readonly partial struct NullableReferences
	{
		public NullableReferences(string? value)
		{
			this.Value = value;
			this.SelectedType = typeof(string);
		}

		public NullableReferences(int[]? value)
		{
			this.Value = value;
			this.SelectedType = typeof(int[]);
		}

		public object? Value { get; }

		public Type SelectedType { get; }
	}

	/// <summary>A hand-authored union implemented as a class, so that reference preservation applies to the union itself.</summary>
	[Union, GenerateShape]
	public sealed partial class PetClassUnion
	{
		public PetClassUnion(Cat value) => this.Value = value;

		public PetClassUnion(Dog value) => this.Value = value;

		public object? Value { get; }
	}

	public record Cat(string Name, int MeowPitch);

	public record Dog(string Name, int BarkVolume);

	public record Apple(int Seeds);

	[GenerateShape]
	public partial class Household
	{
		public Pet? Pet { get; set; }
	}

	[GenerateShape]
	public partial class ThreePets
	{
		public PetClassUnion? First { get; set; }

		public PetClassUnion? Second { get; set; }

		public PetClassUnion? Third { get; set; }
	}

	[GenerateShape]
	public partial class NativeReferences
	{
		public BinaryOrBool[] Binary { get; set; } = [];

		public CSharpScalarUnion[] Text { get; set; } = [];

		public TextOrArray[] Mixed { get; set; } = [];
	}

	[Union, GenerateShape]
	public sealed partial class NullableClassUnion
	{
		public NullableClassUnion(string? value) => this.Value = value;

		public NullableClassUnion(NullableClassUnion value) => this.Value = value;

		public object? Value { get; }
	}

	[GenerateShapeFor<Tree[]>]
	public partial class TreeArrayShape;

	[GenerateShape]
	public partial class NullableUnionHolder
	{
		public NullableClassUnion? Union { get; set; }
	}

	[GenerateShape]
	public partial class ClassUnionReferences
	{
		public NullableClassUnion? First { get; set; }

		public NullableClassUnion? Second { get; set; }
	}

	private class IntOrBoolConverterFactory : IMessagePackConverterFactory
	{
		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		{
			if (type != typeof(IntOrBool))
			{
				return null;
			}

			Assert.IsAssignableFrom<IUnionTypeShape<IntOrBool>>(shape);
			return new IntOrBoolConverter();
		}
	}
}
