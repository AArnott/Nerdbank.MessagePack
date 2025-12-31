// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using PolyType.Tests;
using Xunit.Sdk;

public abstract partial class StructuralEqualityComparerTests
{
	[Theory]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
#if NET
	public void Equals_Exhaustive<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
#else
	public void Equals_Exhaustive<T, TProvider>(TestCase<T> testCase)
#endif
	{
		// We do not expect these cases to work.
		Assert.SkipWhen(typeof(T) == typeof(object), "T = object");

		IEqualityComparer<T> equalityComparer;
		try
		{
			equalityComparer = this.GetEqualityComparer(testCase.DefaultShape);
		}
		catch (NotSupportedException ex)
		{
			// We don't expect all types to be supported.
			throw SkipException.ForSkip($"Unsupported: {ex.Message}");
		}

		Assert.True(equalityComparer.Equals(testCase.Value!, testCase.Value!));
	}

	[Theory]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
#if NET
	public void GetHashCode_Exhaustive<T, TProvider>(TestCase<T, TProvider> testCase)
	where TProvider : IShapeable<T>
#else
	public void GetHashCode_Exhaustive<T, TProvider>(TestCase<T> testCase)
#endif
	{
		// We do not expect these cases to work.
		Assert.SkipWhen(typeof(T) == typeof(object), "T = object");

		IEqualityComparer<T> equalityComparer;
		try
		{
			equalityComparer = this.GetEqualityComparer(testCase.DefaultShape);
		}
		catch (NotSupportedException ex)
		{
			// We don't expect all types to be supported.
			throw SkipException.ForSkip($"Unsupported: {ex.Message}");
		}

		// We don't really have anything useful to check the return value against, but
		// at least verify it doesn't throw.
		if (testCase.Value is not null)
		{
			equalityComparer.GetHashCode(testCase.Value);
		}
	}

	protected abstract IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape);
}
