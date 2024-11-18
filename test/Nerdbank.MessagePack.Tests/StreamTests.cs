// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class StreamTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
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

		await this.Serializer.SerializeAsync(stream, person);

		stream.Position = 0;
		Person? deserialized = await this.Serializer.DeserializeAsync<Person>(stream);

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

		this.Serializer.Serialize(stream, person);

		stream.Position = 0;
		Person? deserialized = this.Serializer.Deserialize<Person>(stream);
	}

	[GenerateShape]
	public partial record Person(string FirstName, string LastName);
}
