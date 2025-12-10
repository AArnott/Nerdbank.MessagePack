// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft;
using Microsoft.FSharp.Reflection;
using PolyType.Tests;
using Xunit.Sdk;

public class SharedTestCases : MessagePackSerializerTestBase
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

		// The PolyType test cases don't consistently specify DateTimeKind.
		// Make an assumption for Kind so we can get through them all.
		this.Serializer = this.Serializer.WithAssumedDateTimeKind(DateTimeKind.Local);

		try
		{
			ITypeShape<T> shape = testCase.DefaultShape;
			byte[] msgpack;
			switch (testCase.DefaultShape)
			{
				case IEnumerableTypeShape { IsAsyncEnumerable: true }:
					// Async enumerables requires async serialization.
					Exception ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(testCase.Value, shape, TestContext.Current.CancellationToken));
					this.Logger.WriteLine(ex.GetBaseException().Message);
					Assert.IsType<NotSupportedException>(ex.GetBaseException());
					return;
				case IFunctionTypeShape:
					Assert.Skip("Delegates cannot be serialized.");
					throw Assumes.NotReachable();
				default:
					msgpack = this.Serializer.Serialize(testCase.Value, shape, TestContext.Current.CancellationToken);
					break;
			}

			this.LogMsgPack(msgpack);

			if (IsDeserializable(testCase))
			{
				T? deserializedValue = this.Serializer.Deserialize(msgpack, shape, TestContext.Current.CancellationToken);

				if (testCase.IsEquatable)
				{
					// DateTime values need special handling because deserialized DateTimes are always UTC,
					// but the original value might have been Unspecified (treated as Local during serialization).
					// We normalize both values to UTC for comparison to avoid timezone-dependent test failures.
					object? expectedValue = testCase.Value;
					object? actualValue = deserializedValue;
					if (typeof(T) == typeof(DateTime) && testCase.Value is DateTime dt)
					{
						DateTime expectedDateTime = dt.Kind == DateTimeKind.Unspecified
							? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
							: dt.ToUniversalTime();
						expectedValue = expectedDateTime;
					}

					Assert.Equal(expectedValue, actualValue);
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
			Exception baseException = ex.GetBaseException();
			if (baseException.Message == "Delegate types cannot be serialized.")
			{
				Assert.IsType<NotSupportedException>(baseException);
			}
			else
			{
				Assert.IsType<PlatformNotSupportedException>(baseException);
			}

			throw SkipException.ForSkip(baseException.Message);
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
