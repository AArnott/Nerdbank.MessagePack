// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class NamingConventionTests : MessagePackSerializerTestBase
{
	[Fact]
	public void PropertyNameConvention_CamelCase()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		TestObject obj = new() { SomeProperty = "test", AnotherProperty = 42 };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, obj);

		// Verify that property names are camelCased
		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		List<string> properties = new();
		for (int i = 0; i < 2; i++)
		{
			properties.Add(reader.ReadString()!);
			reader.Skip(new SerializationContext());
		}

		Assert.Contains("someProperty", properties);
		Assert.Contains("anotherProperty", properties);
	}

	[Fact]
	public void PropertyNameConvention_RespectExplicitNames()
	{
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		TestObjectWithExplicitName obj = new() { SomeProperty = "test", ExplicitlyNamedProperty = 42 };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, obj);

		// Verify that explicit names are preserved, but others are camelCased
		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		List<string> properties = new();
		for (int i = 0; i < 2; i++)
		{
			properties.Add(reader.ReadString()!);
			reader.Skip(new SerializationContext());
		}

		Assert.Contains("someProperty", properties);
		Assert.Contains("CustomName", properties);
	}

	[Fact]
	public void PropertyNameConvention_TakesPrecedenceOverNamingPolicy()
	{
		MessagePackSerializer serializer = this.Serializer with
		{
			PropertyNamingPolicy = MessagePackNamingPolicy.PascalCase,
			PropertyNameConvention = NamingConventions.CamelCase,
		};
		TestObject obj = new() { SomeProperty = "test", AnotherProperty = 42 };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, obj);

		// Verify that PropertyNameConvention takes precedence
		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		string firstPropertyName = reader.ReadString()!;
		reader.Skip(new SerializationContext());

		// Should be camelCase (from convention), not PascalCase (from policy)
		Assert.True(char.IsLower(firstPropertyName[0]));
	}

	[Fact]
	public void PropertyNameConvention_Roundtrip()
	{
		// Set up serializer with naming convention
		MessagePackSerializer originalSerializer = this.Serializer;
		this.Serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };
		
		try
		{
			TestObject original = new() { SomeProperty = "test", AnotherProperty = 42 };

			// Just use the existing AssertRoundtrip method which handles the complexities
			this.AssertRoundtrip(original);
		}
		finally
		{
			// Restore original serializer
			this.Serializer = originalSerializer;
		}
	}

	[Fact]
	public void PropertyNameConvention_CustomConvention()
	{
		// Test with a custom naming convention that adds "_custom" suffix
		static string CustomNamingConvention(IPropertyShape property)
		{
			if (NamingConventions.HasExplicitPropertyName(property))
			{
				return property.Name;
			}

			return property.Name + "_custom";
		}

		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = CustomNamingConvention };
		TestObject obj = new() { SomeProperty = "test", AnotherProperty = 42 };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, obj);

		// Verify that custom convention is applied
		MessagePackReader reader = new(sequence);
		reader.ReadMapHeader();

		List<string> properties = new();
		for (int i = 0; i < 2; i++)
		{
			properties.Add(reader.ReadString()!);
			reader.Skip(new SerializationContext());
		}

		Assert.Contains("SomeProperty_custom", properties);
		Assert.Contains("AnotherProperty_custom", properties);
	}

	[Fact]
	public void HasExplicitPropertyName_PropertyShapeAttribute()
	{
		TestObjectWithExplicitName obj = new() { SomeProperty = "test", ExplicitlyNamedProperty = 42 };

		// We need to get the actual IPropertyShape for testing
		// This is a bit involved as we need to get it from the type shape system
		// For this test, we'll verify through serialization behavior instead
		MessagePackSerializer serializer = this.Serializer with { PropertyNameConvention = NamingConventions.CamelCase };

		Sequence<byte> sequence = new();
		serializer.Serialize(sequence, obj);

		MessagePackReader reader = new(sequence);
		int propertyCount = reader.ReadMapHeader();

		List<string> properties = new();
		for (int i = 0; i < propertyCount; i++)
		{
			properties.Add(reader.ReadString()!);
			reader.Skip(new SerializationContext());
		}

		// Explicit name should be preserved
		Assert.Contains("CustomName", properties);

		// Regular property should be camelCased
		Assert.Contains("someProperty", properties);
	}

	[Fact]
	public void HasExplicitPropertyName_NullProperty_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => NamingConventions.HasExplicitPropertyName(null!));
	}

	[GenerateShape]
	public partial record TestObject
	{
		public string? SomeProperty { get; set; }

		public int AnotherProperty { get; set; }
	}

	[GenerateShape]
	public partial record TestObjectWithExplicitName
	{
		public string? SomeProperty { get; set; }

		[PropertyShape(Name = "CustomName")]
		public int ExplicitlyNamedProperty { get; set; }
	}
}
