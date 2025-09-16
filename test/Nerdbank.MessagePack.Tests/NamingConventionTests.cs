// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class NamingConventionTests : MessagePackSerializerTestBase
{
	[Fact]
	public void CamelCase_NamingConvention_Applied()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		TestClass value = new() { SomeProperty = "test", AnotherProperty = "value" };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, value);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		// Assert that the naming convention is applied.
		Assert.Equal("someProperty", reader.ReadString());
		reader.Skip(new SerializationContext());

		Assert.Equal("anotherProperty", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	[Fact]
	public void PascalCase_NamingConvention_Applied()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.PascalCase };
		LowerCaseTestClass value = new() { SomeProperty = "test", AnotherProperty = "value" };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, value);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		// Assert that the naming convention is applied.
		Assert.Equal("SomeProperty", reader.ReadString());
		reader.Skip(new SerializationContext());

		Assert.Equal("AnotherProperty", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	[Fact]
	public void NamingConvention_RespectedOverNamingPolicy()
	{
		MessagePackSerializer serializer = this.Serializer with
		{
			PropertyNameConvention = NamingConventions.PascalCase,
			PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase,
		};
		LowerCaseTestClass value = new() { SomeProperty = "test", AnotherProperty = "value" };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, value);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		// Assert that the naming convention takes precedence over naming policy.
		Assert.Equal("SomeProperty", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	[Fact]
	public void ExplicitPropertyName_NotTransformed()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		ExplicitNameTestClass value = new() { SomeProperty = "test", AnotherProperty = "value" };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, value);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		// Assert that explicit property names are not changed.
		Assert.Equal("someProperty", reader.ReadString());
		reader.Skip(new SerializationContext());

		Assert.Equal("ExpressName", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	[Fact]
	public void Roundtrip_WithNamingConvention()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		TestClass original = new() { SomeProperty = "test", AnotherProperty = "value" };

		TestClass result = this.RoundtripWithCustomSerializer(original, serializer);

		Assert.Equal(original.SomeProperty, result.SomeProperty);
		Assert.Equal(original.AnotherProperty, result.AnotherProperty);
	}

	[Fact]
	public void CustomNamingConvention_Applied()
	{
		NamingConvention snakeCase = property =>
		{
			if (NamingConventions.HasExplicitPropertyName(property))
			{
				return property.Name;
			}

			// Simple snake_case conversion
			return string.Concat(property.Name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
		};

		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = snakeCase };
		TestClass value = new() { SomeProperty = "test", AnotherProperty = "value" };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, value);

		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		// Assert that the custom naming convention is applied.
		Assert.Equal("some_property", reader.ReadString());
		reader.Skip(new SerializationContext());

		Assert.Equal("another_property", reader.ReadString());
		reader.Skip(new SerializationContext());
	}

	private T RoundtripWithCustomSerializer<T>(T value, MessagePackSerializer customSerializer)
#if NET
		where T : IShapeable<T>
	{
		ITypeShape<T> shape = T.GetTypeShape();
#else
	{
		ITypeShape<T> shape = GetTypeShape<T, Witness>();
#endif
		Sequence<byte> sequence = new();
		customSerializer.Serialize(sequence, value, shape, TestContext.Current.CancellationToken);
		this.LogMsgPack(sequence);
		this.lastRoundtrippedMsgpack = sequence;
		return customSerializer.Deserialize(sequence, shape, TestContext.Current.CancellationToken);
	}

	[GenerateShape]
	public partial record TestClass
	{
		public string? SomeProperty { get; set; }

		public string? AnotherProperty { get; set; }
	}

	[GenerateShape]
	public partial record LowerCaseTestClass
	{
		public string? SomeProperty { get; set; }

		public string? AnotherProperty { get; set; }
	}

	[GenerateShape]
	public partial record ExplicitNameTestClass
	{
		public string? SomeProperty { get; set; }

		[PropertyShape(Name = "ExpressName")]
		public string? AnotherProperty { get; set; }
	}
}