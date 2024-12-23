// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using PolyType.Tests;
using Xunit.Sdk;

public class SharedTestCases(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Theory]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
#if NET
	public void Roundtrip_Value<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
#else
	public void Roundtrip_Value<T, TProvider>(TestCase<T> testCase)
#endif
	{
		try
		{
			ITypeShape<T> shape = testCase.DefaultShape;
			byte[] msgpack = this.Serializer.Serialize(testCase.Value, shape, TestContext.Current.CancellationToken);

			if (IsDeserializable(testCase))
			{
				T? deserializedValue = this.Serializer.Deserialize(msgpack, shape, TestContext.Current.CancellationToken);

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

					byte[] msgpack2 = this.Serializer.Serialize(deserializedValue, shape, TestContext.Current.CancellationToken);

					Assert.Equal(msgpack, msgpack2);
				}
			}
			else
			{
				Assert.Throws<NotSupportedException>(() => this.Serializer.Deserialize(msgpack, shape, TestContext.Current.CancellationToken));
			}
		}
		catch (PlatformNotSupportedException ex)
		{
			throw SkipException.ForSkip(ex.Message);
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

		if (testCase.CustomKind == TypeShapeKind.None)
		{
			return false;
		}

		return true;
	}
}
