// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class OddShapeTests : MessagePackSerializerTestBase
{
	/// <summary>
	/// Verifies that serializing an object with mismatched constructor parameters and properties throws a
	/// <see cref="MessagePackSerializationException"/> with a <see cref="NotSupportedException"/> as the inner exception.
	/// </summary>
	/// <remarks>
	/// This test ensures that the serializer correctly detects and reports cases where the object's
	/// constructor parameters do not align with its settable properties, which is not supported by the serialization
	/// framework.
	/// </remarks>
	[Fact]
	public void OddShapeWithParameterAndPropertyMismatch()
	{
		ClassWithParameterAndPropertyMismatch instance = new(42) { Comparer = 1 };
		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(instance, TestContext.Current.CancellationToken));
		NotSupportedException baseException = Assert.IsType<NotSupportedException>(ex.GetBaseException());
		this.Logger.WriteLine(baseException.Message);
	}

	[GenerateShape]
	public partial class ClassWithParameterAndPropertyMismatch
	{
		public ClassWithParameterAndPropertyMismatch(int capacity)
		{
		}

		[Key(0)]
		public int Comparer { get; init; }
	}
}
