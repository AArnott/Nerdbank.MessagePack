// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.FSharp.Reflection;
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
		// Avoid using our secure hash algorithm because that messes with the order of elements
		// in unordered collections, causing our equality testing to fail.
		this.Serializer = this.Serializer with { ComparerProvider = null };

		try
		{
			ITypeShape<T> shape = testCase.DefaultShape;
			byte[] msgpack;
			if (testCase.DefaultShape is IEnumerableTypeShape { IsAsyncEnumerable: true })
			{
				// Async enumerables requires async serialization.
				Exception ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(testCase.Value, shape, TestContext.Current.CancellationToken));
				this.Logger.WriteLine(ex.GetBaseException().Message);
				Assert.IsType<NotSupportedException>(ex.GetBaseException());
				return;
			}
			else
			{
				msgpack = this.Serializer.Serialize(testCase.Value, shape, TestContext.Current.CancellationToken);
			}

			this.LogMsgPack(msgpack);

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
				MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize(msgpack, shape, TestContext.Current.CancellationToken));
				Assert.IsType<NotSupportedException>(ex.InnerException);
			}
		}
		catch (MessagePackSerializationException ex)
		{
			Assert.IsType<PlatformNotSupportedException>(ex.InnerException);
			throw SkipException.ForSkip(ex.Message);
		}
	}

	private static bool IsDeserializable(PolyType.Tests.ITestCase testCase)
	{
		if (testCase.Value is null)
		{
			return true;
		}

		if (testCase.HasOutConstructorParameters)
		{
			return false;
		}

		if (testCase.IsAbstract && !testCase.IsUnion && !typeof(System.Collections.IEnumerable).IsAssignableFrom(testCase.Type))
		{
			return false;
		}

		if (testCase.IsUnion)
		{
			return !testCase.IsAbstract || FSharpType.IsUnion(testCase.Type, null);
		}

		if (testCase.CustomKind == TypeShapeKind.None)
		{
			return false;
		}

		return true;
	}
}
