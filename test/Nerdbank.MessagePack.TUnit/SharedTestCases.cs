// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This test uses reflection (MakeGenericMethod) to dispatch across every type known to
// PolyType's test case catalog, since TUnit does not support open generic test methods whose type
// arguments are inferred from a dynamically-produced data source (unlike xunit's
// Theory/MemberData combination, which this test used prior to migration).
// This is not compatible with NativeAOT, but the test detects that failure at runtime and skips
// gracefully, so the trim/AOT analysis warnings are suppressed via [UnconditionalSuppressMessage].
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft;
using PolyType.Tests;

public partial class SharedTestCases : MessagePackSerializerTestBase
{
	private static readonly MethodInfo RoundtripValueOpenMethod = typeof(SharedTestCases)
		.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
		.Single(m => m.Name == nameof(RoundtripValueCore) && m.IsGenericMethodDefinition);

	[Test]
	[MethodDataSource(typeof(TestTypes), nameof(TestTypes.GetTestCases))]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void Roundtrip_Value(ITestCase testCase)
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			// Unlike Equals_Exhaustive/GetHashCode_Exhaustive (which detect and skip individual unsupported
			// cases via caught exceptions), MakeGenericMethod here can succeed under NativeAOT's shared/canonical
			// generic instantiations for some union/abstract type hierarchies while producing silently incorrect
			// results (rather than throwing), so we cannot rely on exception-based detection alone. We skip the
			// entire test under NativeAOT rather than risk a false pass or a spurious failure.
			Skip.Test("This test requires runtime code generation, which is unavailable in this environment.");
			return;
		}
#endif

		try
		{
			RoundtripValueOpenMethod.MakeGenericMethod(testCase.Type).Invoke(this, [testCase]);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is SkipRequestedException skip)
		{
			Skip.Test(skip.Message);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException or PlatformNotSupportedException)
		{
			// MakeGenericMethod requires runtime code generation, which is unavailable when published for NativeAOT.
			Skip.Test($"This test requires runtime code generation, which is unavailable in this environment: {ex.InnerException.Message}");
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
		}
		catch (Exception ex) when (ex is NotSupportedException or PlatformNotSupportedException)
		{
			// MakeGenericMethod requires runtime code generation, which is unavailable when published for NativeAOT.
			Skip.Test($"This test requires runtime code generation, which is unavailable in this environment: {ex.Message}");
		}
	}

	private static bool IsDeserializable(ITestCase testCase)
	{
		if (testCase.Value is null)
		{
			return true;
		}

		if (testCase.IsAbstract && !testCase.IsUnion && !typeof(System.Collections.IEnumerable).IsAssignableFrom(testCase.Type))
		{
			return false;
		}

		if (testCase.IsUnion)
		{
			return true;
		}

		if (testCase.CustomKind == TypeShapeKind.None)
		{
			return false;
		}

		return true;
	}

	private void RoundtripValueCore<T>(ITestCase testCase)
	{
		// Avoid using our secure hash algorithm because that messes with the order of elements
		// in unordered collections, causing our equality testing to fail.
		this.Serializer = this.Serializer with { ComparerProvider = null };

		// The PolyType test cases don't consistently specify DateTimeKind.
		// Make an assumption for Kind so we can get through them all.
		this.Serializer = this.Serializer.WithAssumedDateTimeKind(DateTimeKind.Local);

		try
		{
			ITypeShape<T> shape = (ITypeShape<T>)testCase.DefaultShape;
			T value = (T)testCase.Value!;
			byte[] msgpack;
			switch (testCase.DefaultShape)
			{
				case IEnumerableTypeShape { IsAsyncEnumerable: true }:
					// Async enumerables requires async serialization.
					Exception ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Serialize(value, shape, this.TimeoutToken));
					Console.WriteLine(ex.GetBaseException().Message);
					Assert.IsType<NotSupportedException>(ex.GetBaseException());
					return;
				case IFunctionTypeShape:
					throw new SkipRequestedException("Delegates cannot be serialized.");
				default:
					msgpack = this.Serializer.Serialize(value, shape, this.TimeoutToken);
					break;
			}

			this.LogMsgPack(msgpack);

			if (IsDeserializable(testCase))
			{
				T? deserializedValue = this.Serializer.Deserialize(msgpack, shape, this.TimeoutToken);

				if (testCase.IsEquatable)
				{
					// DateTime values need special handling because deserialized DateTimes are always UTC,
					// but the original value might have been Unspecified (treated as Local during serialization).
					// We normalize both values to UTC for comparison to avoid timezone-dependent test failures.
					Type type = typeof(T);
					if (type == typeof(DateTime) || type == typeof(DateTime?))
					{
						DateTime? expectedDateTime = value is DateTime dt
							? (dt.Kind == DateTimeKind.Unspecified
								? DateTime.SpecifyKind(dt, DateTimeKind.Local).ToUniversalTime()
								: dt.ToUniversalTime())
							: null;

						DateTime? actualDateTime = deserializedValue is DateTime dtActual
							? dtActual.ToUniversalTime() // Deserialized DateTimes should already be UTC, but normalize just to be safe.
							: null;

						Assert.Equal(expectedDateTime, actualDateTime);
					}
					else
					{
						Assert.Equal(value, deserializedValue);
					}
				}
				else
				{
					if (testCase.IsStack)
					{
						deserializedValue = this.Roundtrip(deserializedValue, shape);
					}

					byte[] msgpack2 = this.Serializer.Serialize(deserializedValue, shape, this.TimeoutToken);

					Assert.Equal(msgpack, msgpack2);
				}
			}
			else
			{
				MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(() => this.Serializer.Deserialize(msgpack, shape, this.TimeoutToken));
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
				Assert.IsAssignableFrom<NotSupportedException>(baseException);
			}

			throw new SkipRequestedException(baseException.Message);
		}
	}

	/// <summary>
	/// A marker exception used to signal from <see cref="RoundtripValueCore{T}(ITestCase)"/> (invoked via reflection)
	/// that the calling <see cref="Roundtrip_Value(ITestCase)"/> test should be reported as skipped rather than failed or passed.
	/// </summary>
	private sealed class SkipRequestedException(string message) : Exception(message);
}
