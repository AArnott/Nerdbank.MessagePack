// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Numerics;
using PolyType.Tests;

public abstract partial class ByValueEqualityComparerTests(ITestOutputHelper logger)
{
	internal enum FruitKind
	{
		Apple,
		Banana,
	}

	[Fact]
	public void Boolean() => this.AssertEqualityComparerBehavior<bool, Witness>([true], [false]);

	[Fact]
	public void BigInteger() => this.AssertEqualityComparerBehavior<BigInteger, Witness>([new BigInteger(5), new BigInteger(5)], [new BigInteger(10)]);

	[Fact]
	public void CustomType_Tree() => this.AssertEqualityComparerBehavior(
		[new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Apple), new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Apple)],
		[
			new Tree([new Fruit(3, "Red"), new Fruit(4, "Yellow")], 4, FruitKind.Apple),
			new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Banana),
			new Tree([new Fruit(4, "Red")], 4, FruitKind.Banana),
			new Tree([new Fruit(3, "Yellow")], 4, FruitKind.Apple)]);

	[Fact]
	public void CustomType_Fruit() => this.AssertEqualityComparerBehavior(
		[new Fruit(3, "Red"), new Fruit(3, "Red")],
		[new Fruit(4, "Red"), new Fruit(3, "Yellow")]);

	[Fact]
	public abstract void CustomHash();

	[Fact]
	public void CustomHashingAndEquality() => this.AssertEqualityComparerBehavior(
		[new CustomHasher(), new CustomHasher()],
		[new CustomHasher() { SpecialCode = 33 }]);

	[Fact]
	public void ReadOnlyMemoryOfByte() => this.AssertEqualityComparerBehavior(
		[new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2 }), new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2 })],
		[new HaveReadOnlyMemoryOfByte(new byte[] { 1, 3 }), new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2, 3 })]);

	[SkippableTheory(typeof(NotSupportedException))]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
#if NET
	public void Equals_Exhaustive<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
#else
	public void Equals_Exhaustive<T, TProvider>(TestCase<T> testCase)
#endif
	{
		// We do not expect these cases to work.
		Skip.If(typeof(T) == typeof(object));

		IEqualityComparer<T> equalityComparer = this.GetEqualityComparer(testCase.DefaultShape);
		Assert.True(equalityComparer.Equals(testCase.Value!, testCase.Value!));
	}

	[SkippableTheory(typeof(NotSupportedException))]
	[MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
#if NET
	public void GetHashCode_Exhaustive<T, TProvider>(TestCase<T, TProvider> testCase)
		where TProvider : IShapeable<T>
#else
	public void GetHashCode_Exhaustive<T, TProvider>(TestCase<T> testCase)
#endif
	{
		// We do not expect these cases to work.
		Skip.If(typeof(T) == typeof(object));

		IEqualityComparer<T> equalityComparer = this.GetEqualityComparer(testCase.DefaultShape);

		// We don't really have anything useful to check the return value against, but
		// at least verify it doesn't throw.
		if (testCase.Value is not null)
		{
			equalityComparer.GetHashCode(testCase.Value);
		}
	}

	protected IEqualityComparer<T> GetEqualityComparer<T, TProvider>()
#if NET
		where TProvider : IShapeable<T> => this.GetEqualityComparer(TProvider.GetShape());
#else
		=> this.GetEqualityComparer(MessagePackSerializerTestBase.GetShape<T, TProvider>());
#endif

	protected IEqualityComparer<T> GetEqualityComparer<T>()
#if NET
		where T : IShapeable<T> => this.GetEqualityComparer<T>(T.GetShape());
#else
		=> this.GetEqualityComparer(MessagePackSerializerPolyfill.Witness.ShapeProvider.GetShape<T>());
#endif

	protected abstract IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape);

	/// <inheritdoc cref="AssertEqualityComparerBehavior{T, TProvider}(T[], T[])"/>
	private void AssertEqualityComparerBehavior<T>(T[] equivalent, T[] different)
#if NET
		where T : notnull, IShapeable<T> => this.AssertEqualityComparerBehavior<T, T>(equivalent, different);
#else
		where T : notnull => this.AssertEqualityComparerBehavior<T, Witness>(equivalent, different);
#endif

	/// <summary>
	/// Asserts that hash codes and equality checks match or mismatch for various values.
	/// </summary>
	/// <typeparam name="T">The type of the values to be tested.</typeparam>
	/// <typeparam name="TProvider">The witness type for the data type.</typeparam>
	/// <param name="equivalent">An array of values that should all be considered equivalent.</param>
	/// <param name="different">An array of values that are each distinct from any of the values in the <paramref name="equivalent"/> array.</param>
	private void AssertEqualityComparerBehavior<T, TProvider>(T[] equivalent, T[] different)
#if NET
		where TProvider : IShapeable<T>
#endif
		where T : notnull
	{
		IEqualityComparer<T> eq = this.GetEqualityComparer<T, TProvider>();
		foreach (T valueA in equivalent)
		{
			int valueAHashCode = eq.GetHashCode(valueA);
			logger.WriteLine($"{valueA} hash code: {valueAHashCode}");

			foreach (T valueB in equivalent)
			{
				int valueBHashCode = eq.GetHashCode(valueB);
				logger.WriteLine($"{valueB} hash code: {valueBHashCode}");
				Assert.True(eq.Equals(valueA, valueB));
				Assert.Equal(valueAHashCode, valueBHashCode);
			}
		}

		T baseline = equivalent[0];
		int baselineHashCode = eq.GetHashCode(baseline);
		foreach (T differentValue in different)
		{
			int differentValueHashCode = eq.GetHashCode(differentValue);
			logger.WriteLine($"{differentValue} hash code: {differentValueHashCode}");
			Assert.False(eq.Equals(equivalent[0], differentValue));
			Assert.NotEqual(baselineHashCode, differentValueHashCode);
		}
	}

	public class DefaultByValue(ITestOutputHelper logger) : ByValueEqualityComparerTests(logger)
	{
		[Fact]
		public override void CustomHash()
		{
			CustomHasher obj = new();
			Assert.Equal(obj.SpecialCode, this.GetEqualityComparer<CustomHasher>().GetHashCode(obj));
		}

		protected override IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
			=> ByValueEqualityComparer.GetDefault(shape);
	}

	public class HashCollisionResistant(ITestOutputHelper logger) : ByValueEqualityComparerTests(logger)
	{
		[Fact]
		public override void CustomHash()
		{
			CustomHasher obj = new();
			Assert.Equal(obj.SpecialCode * 2, this.GetEqualityComparer<CustomHasher, Witness>().GetHashCode(obj));
		}

		protected override IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
			=> ByValueEqualityComparer.GetHashResistant(shape);
	}

	[GenerateShape<bool>]
	[GenerateShape<BigInteger>]
	[GenerateShape<CustomHasher>]
	internal partial class Witness;

	[GenerateShape]
	internal partial class Tree(Fruit[] fruits, int height, FruitKind kind)
	{
		public Fruit[] Fruits => fruits;

		public int Height => height;

		public FruitKind Kind => kind;

		public override string ToString() => $"{kind} tree with {fruits.Length} fruits and height {height}";
	}

	[GenerateShape]
	internal partial class Fruit(int weight, string color)
	{
		public int Weight => weight;

		public string Color => color;

		public override string ToString() => $"Fruit weighing {weight} and color {color}";
	}

	[GenerateShape]
	internal partial class HaveReadOnlyMemoryOfByte(ReadOnlyMemory<byte> buffer)
	{
		public ReadOnlyMemory<byte> Buffer => buffer;
	}

	[GenerateShape]
	internal partial class CustomHasher : IDeepSecureEqualityComparer<CustomHasher>
	{
		// This is internal on purpose, so that PolyType will ignore the property for purposes of equality
		// and hashing, and tests will only pass if the custom hash and equality methods on this class are used.
		internal int SpecialCode { get; set; } = 42;

		public bool DeepEquals(CustomHasher? other) => other is not null && this.SpecialCode == other.SpecialCode;

		public long GetSecureHashCode() => this.SpecialCode * 2;

		public override int GetHashCode() => this.SpecialCode;
	}
}
