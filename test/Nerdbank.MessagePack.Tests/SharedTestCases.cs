// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using TypeShape.Tests;

public class SharedTestCases(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	private static readonly IProviderUnderTest Provider = SourceGenProviderUnderTest.Default; // or RefectionProviderUnderTest.Default;

	[Theory]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
	public void Roundtrip_Value<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
	{
		ITypeShape<T> shape = TProvider.GetShape();
		byte[] msgpack = this.Serializer.Serialize(testCase.Value, shape);

		if (!HasConstructors(Provider, testCase) && testCase.Value is not null)
		{
			Assert.Throws<NotSupportedException>(() =>
			{
				this.Serializer.Deserialize(msgpack, shape);
			});
		}
		else
		{
			T? deserializedValue = this.Serializer.Deserialize(msgpack, shape);

			if (testCase.IsLossyRoundtrip)
			{
				return;
			}

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
	}

	// https://github.com/eiriktsarpalis/typeshape-csharp/issues/42
	private static bool HasConstructors<T>(IProviderUnderTest provider, TestCase<T> testCase)
	{
		return (!testCase.IsAbstract || typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T))) && !testCase.HasOutConstructorParameters && (!testCase.UsesSpanConstructor || provider.Kind != ProviderKind.Reflection);
	}
}
