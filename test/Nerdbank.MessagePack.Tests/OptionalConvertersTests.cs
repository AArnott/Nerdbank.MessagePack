// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Specialized;
using System.Text;

public partial class OptionalConvertersTests : MessagePackSerializerTestBase
{
	[Fact]
	public void NullCheck()
	{
		Assert.Throws<ArgumentNullException>("serializer", () => OptionalConverters.WithSystemTextJsonConverters(null!));
		Assert.Throws<ArgumentNullException>("serializer", () => OptionalConverters.WithGuidConverter(null!, OptionalConverters.GuidStringFormat.StringN));
	}

	[Fact]
	public void DoubleAddThrows()
	{
		this.Serializer = this.Serializer.WithGuidConverter(OptionalConverters.GuidStringFormat.StringD);
		Assert.Throws<ArgumentException>(() => this.Serializer.WithGuidConverter(OptionalConverters.GuidStringFormat.StringN));
	}

	[Fact]
	public void WithAssumedDateTimeKind_InvalidInputs()
	{
		// The valid inputs are tested in the BuiltInConverterTests class.
		Assert.Throws<ArgumentNullException>("serializer", () => OptionalConverters.WithAssumedDateTimeKind(null!, DateTimeKind.Local));
		Assert.Throws<ArgumentException>("kind", () => OptionalConverters.WithAssumedDateTimeKind(this.Serializer, (DateTimeKind)999));
		Assert.Throws<ArgumentException>("kind", () => OptionalConverters.WithAssumedDateTimeKind(this.Serializer, DateTimeKind.Unspecified));
	}

	[Fact]
	public void WithAssumedDateTimeKind_Twice()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(
			   () => this.Serializer
			   .WithAssumedDateTimeKind(DateTimeKind.Local)
			   .WithAssumedDateTimeKind(DateTimeKind.Utc));
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void WithHiFiDateTime_Twice()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(
			   () => this.Serializer
			   .WithHiFiDateTime()
			   .WithHiFiDateTime());
		this.Logger.WriteLine(ex.Message);
	}

	[Fact]
	public void NameValueCollection()
	{
		this.Serializer = this.Serializer.WithNameValueCollectionConverter();
		NameValueCollection values = new()
		{
			{ "Name1", "Value1" },
			{ "Name1", "Value2" },
			{ "Name2", null },
			{ "Name3", "Value3" },
		};
		HasNameValueCollection? roundtripped = this.Roundtrip(new HasNameValueCollection { Values = values });

		Assert.NotNull(roundtripped);
		Assert.NotNull(roundtripped.Values);
		Assert.Equal(values.AllKeys, roundtripped.Values.AllKeys);
		for (int i = 0; i < values.Count; i++)
		{
			Assert.Equal(values.GetValues(i), roundtripped.Values.GetValues(i));
		}

		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(1, reader.ReadMapHeader());
		Assert.Equal(nameof(HasNameValueCollection.Values), reader.ReadString());
		Assert.Equal(3, reader.ReadMapHeader());

		Assert.Equal("Name1", reader.ReadString());
		Assert.Equal(2, reader.ReadArrayHeader());
		Assert.Equal("Value1", reader.ReadString());
		Assert.Equal("Value2", reader.ReadString());

		Assert.Equal("Name2", reader.ReadString());
		Assert.True(reader.TryReadNil());

		Assert.Equal("Name3", reader.ReadString());
		Assert.Equal("Value3", reader.ReadString());
		Assert.True(reader.End);
		Assert.True(this.DataMatchesSchema(this.lastRoundtrippedMsgpack, Witness.GeneratedTypeShapeProvider.GetTypeShapeOrThrow<HasNameValueCollection>()));
	}

	[Fact]
	public void NameValueCollection_Null()
	{
		this.Serializer = this.Serializer.WithNameValueCollectionConverter();
		HasNameValueCollection? roundtripped = this.Roundtrip(new HasNameValueCollection { Values = null });

		Assert.NotNull(roundtripped);
		Assert.Null(roundtripped.Values);
	}

	[Fact]
	public void NameValueCollection_EmptyArrayPreservesKey()
	{
		this.Serializer = this.Serializer.WithNameValueCollectionConverter();
		Sequence<byte> sequence = new();
		MessagePackWriter writer = new(sequence);
		writer.WriteMapHeader(1);
		writer.Write(nameof(HasNameValueCollection.Values));
		writer.WriteMapHeader(1);
		writer.Write("Name");
		writer.WriteArrayHeader(0);
		writer.Flush();

		HasNameValueCollection? result = this.Serializer.Deserialize<HasNameValueCollection>(sequence, TestContext.Current.CancellationToken);

		Assert.NotNull(result);
		Assert.NotNull(result.Values);
		Assert.Equal("Name", Assert.Single(result.Values.AllKeys));
		Assert.Null(result.Values["Name"]);
	}

	[Fact]
	public void NameValueCollection_StringInterning()
	{
		this.Serializer = this.Serializer.WithNameValueCollectionConverter() with { InternStrings = true };
		string firstValue = "RepeatedValue";
		string secondValue = new StringBuilder().Append(firstValue).ToString();
		Assert.NotSame(firstValue, secondValue);

		NameValueCollection values = new()
		{
			{ "Name1", firstValue },
			{ "Name2", secondValue },
		};
		HasNameValueCollection? roundtripped = this.Roundtrip(new HasNameValueCollection { Values = values });

		Assert.NotNull(roundtripped);
		Assert.NotNull(roundtripped.Values);
		Assert.Same(roundtripped.Values["Name1"], roundtripped.Values["Name2"]);
	}

	[GenerateShape]
	public partial class HasNameValueCollection
	{
		public NameValueCollection? Values { get; set; } = new();
	}

	[GenerateShapeFor<HasNameValueCollection>]
	private partial class Witness;
}
