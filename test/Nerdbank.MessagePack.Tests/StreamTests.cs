// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StreamTests : MessagePackSerializerTestBase
{
	[Theory, PairwiseData]
	public async Task SerializeWithStreamAsync(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		await this.Serializer.SerializeAsync(stream, person, TestContext.Current.CancellationToken);

		stream.Position = 0;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, TestContext.Current.CancellationToken);

		Assert.Equal(person, deserialized);
	}

	[Theory, PairwiseData]
	public void SerializeWithStream(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		this.Serializer.Serialize(stream, person, TestContext.Current.CancellationToken);

		stream.Position = 0;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, TestContext.Current.CancellationToken);
	}

	[Fact]
	public void Deserialize_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		this.Serializer.Serialize(stream, person1, TestContext.Current.CancellationToken);
		long person2Position = stream.Position;
		this.Serializer.Serialize(stream, person2, TestContext.Current.CancellationToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, TestContext.Current.CancellationToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[Fact]
	public async Task DeserializeAsync_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		await this.Serializer.SerializeAsync(stream, person1, TestContext.Current.CancellationToken);
		long person2Position = stream.Position;
		await this.Serializer.SerializeAsync(stream, person2, TestContext.Current.CancellationToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, TestContext.Current.CancellationToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[GenerateShape]
	public partial record Person(string FirstName, string LastName);
}
