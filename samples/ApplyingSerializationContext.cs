// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class ApplyingSerializationContext
{
	void ApplyingContext()
	{
		#region ApplyingStartingContext
		MessagePackSerializer serializer = new()
		{
			StartingContext = new SerializationContext
			{
				MaxDepth = 128,
			},
		};
		serializer.Serialize(new SomeType());
		#endregion
	}

	[GenerateShape]
	internal partial record SomeType;
}
