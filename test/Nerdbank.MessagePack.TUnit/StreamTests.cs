// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StreamTests : MessagePackSerializerTestBase
{
	[Test, MatrixDataSource]
	public async Task SerializeWithStreamAsync(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		await this.Serializer.SerializeAsync(stream, person, this.TimeoutToken);

		stream.Position = 0;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, this.TimeoutToken);

		Assert.Equal(person, deserialized);
	}

	[Test, MatrixDataSource]
	public void SerializeWithStream(bool memoryStream)
	{
		Person person = new("Andrew", "Arnott");

		Stream stream = new MemoryStream();
		if (!memoryStream)
		{
			stream = new MonitoringStream(stream);
		}

		this.Serializer.Serialize(stream, person, this.TimeoutToken);

		stream.Position = 0;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, this.TimeoutToken);
	}

	[Test]
	public void Deserialize_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		this.Serializer.Serialize(stream, person1, this.TimeoutToken);
		long person2Position = stream.Position;
		this.Serializer.Serialize(stream, person2, this.TimeoutToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream, this.TimeoutToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[Test]
	public async Task DeserializeAsync_FromMemoryStreamAtNonZeroPosition()
	{
		// Create test data
		Person person1 = new("Andrew", "Arnott");
		Person person2 = new("Jane", "Doe");

		// Serialize both persons to a MemoryStream
		MemoryStream stream = new();
		await this.Serializer.SerializeAsync(stream, person1, this.TimeoutToken);
		long person2Position = stream.Position;
		await this.Serializer.SerializeAsync(stream, person2, this.TimeoutToken);

		// Reset to position of second person and deserialize
		stream.Position = person2Position;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream, this.TimeoutToken);

		// Should get the second person, not the first
		Assert.Equal(person2, deserialized);
		Assert.NotEqual(person1, deserialized);
	}

	[GenerateShape]
	public partial record Person(string FirstName, string LastName);
}
