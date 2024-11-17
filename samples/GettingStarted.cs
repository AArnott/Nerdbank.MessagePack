// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class SimpleUsage
{
	#region SimpleRecord
	[GenerateShape]
	public partial record ARecord(string AString, bool ABoolean);
	#endregion

	void Roundtrip()
	{
		#region SimpleRecordRoundtrip
		// Construct a value.
		var value = new ARecord("hello", true);

		// Create a serializer instance.
		MessagePackSerializer serializer = new();

		// Serialize the value to the buffer.
		byte[] msgpack = serializer.Serialize(value);

		// Deserialize it back.
		ARecord? deserialized = serializer.Deserialize<ARecord>(msgpack);
		#endregion
	}
}
