// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Tests;

public class SharedTestCases(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Theory]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
	public void Roundtrip_Value<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
	{
		ITypeShape<T> shape = TProvider.GetShape();
		byte[] msgpack = this.Serializer.Serialize(testCase.Value, shape);

		if (IsDeserializable(testCase))
		{
			T? deserializedValue = this.Serializer.Deserialize(msgpack, shape);

			if (testCase.IsEquatable)
			{
				Assert.Equal(testCase.Value, deserializedValue);
			}
			else
			{
				if (testCase.IsStack)
				{
					deserializedValue = this.Roundtrip(deserializedValue, shape);
				}

				byte[] msgpack2 = this.Serializer.Serialize(deserializedValue, shape);

				Assert.Equal(msgpack, msgpack2);
			}
		}
		else
		{
			Assert.Throws<NotSupportedException>(() => this.Serializer.Deserialize(msgpack, shape));
		}
	}

	private static bool IsDeserializable<T>(TestCase<T> testCase)
	{
		if (testCase.Value is null)
		{
			return true;
		}

		if (testCase.HasOutConstructorParameters)
		{
			return false;
		}

		if (testCase.IsAbstract && !typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)))
		{
			return false;
		}

		return true;
	}
}
