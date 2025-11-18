// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

using System.Numerics;
using PolyType.Tests;
using Xunit.Sdk;

public abstract partial class StructuralEqualityComparerTests(ITestOutputHelper logger)
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

	[Fact]
	public void ReadOnlySequenceOfByte() => this.AssertEqualityComparerBehavior(
		[new HaveReadOnlySequenceOfByte(new([1, 2])), new HaveReadOnlySequenceOfByte(new([1, 2]))],
		[new HaveReadOnlySequenceOfByte(new([1, 3])), new HaveReadOnlySequenceOfByte(new([1, 2, 3]))]);

	[Fact]
	public void DerivedTypeEquality()
	{
		SomeBaseType.Derived1 derived1A = new(42);
		SomeBaseType.Derived1 derived1B = new(42);
		SomeBaseType.Derived1 derived1C = new(43);
		SomeBaseType.Derived2 derived2A = new(42);
		SomeBaseType.Derived3 derived3 = new();
		SomeBaseType.Derived4 derived4 = new();

		this.AssertEqualityComparerBehavior<SomeBaseType>(
			[derived1A, derived1B],
			[derived1C, derived2A, derived3, derived4]);
	}

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

	protected IEqualityComparer<T> GetEqualityComparer<T, TProvider>()
#if NET
		where TProvider : IShapeable<T> => this.GetEqualityComparer(TProvider.GetTypeShape());
#else
		=> this.GetEqualityComparer(TypeShapeResolver.ResolveDynamicOrThrow<T, TProvider>());
#endif

	protected IEqualityComparer<T> GetEqualityComparer<T>()
#if NET
		where T : IShapeable<T> => this.GetEqualityComparer<T>(T.GetTypeShape());
#else
		=> this.GetEqualityComparer(Witness.GeneratedTypeShapeProvider.GetTypeShape<T>());
#endif

	protected abstract IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape);

	/// <inheritdoc cref="AssertEqualityComparerBehavior{T, TProvider}(T[], T[])"/>
	private void AssertEqualityComparerBehavior<T>(T[] equivalent, T[] different)
#if NET
		where T : notnull, IShapeable<T> => this.AssertEqualityComparerBehavior<T, T>(equivalent, different);
#else
		where T : notnull => this.AssertEqualityComparerBehavior<T, T>(equivalent, different);
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

		logger.WriteLine("Testing values that are expected to be equal:");
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

		logger.WriteLine("Testing values that are expected to be different:");
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

	public class DefaultStructural(ITestOutputHelper logger) : StructuralEqualityComparerTests(logger)
	{
		[Fact]
		public override void CustomHash()
		{
			CustomHasher obj = new();
			Assert.Equal(obj.SpecialCode, this.GetEqualityComparer<CustomHasher>().GetHashCode(obj));
		}

		protected override IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
			=> StructuralEqualityComparer.GetDefault(shape);
	}

	public class HashCollisionResistant(ITestOutputHelper logger) : StructuralEqualityComparerTests(logger)
	{
		[Fact]
		public override void CustomHash()
		{
			CustomHasher obj = new();
			Assert.Equal(obj.SpecialCode * 2, this.GetEqualityComparer<CustomHasher, Witness>().GetHashCode(obj));
		}

		protected override IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
			=> StructuralEqualityComparer.GetHashCollisionResistant(shape);
	}

	[GenerateShapeFor<bool>]
	[GenerateShapeFor<BigInteger>]
	[GenerateShapeFor<CustomHasher>]
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
	internal partial class HaveReadOnlySequenceOfByte(ReadOnlySequence<byte> buffer)
	{
		public ReadOnlySequence<byte> Buffer => buffer;
	}

	[GenerateShape]
	internal partial class CustomHasher : IStructuralSecureEqualityComparer<CustomHasher>
	{
		// This is internal on purpose, so that PolyType will ignore the property for purposes of equality
		// and hashing, and tests will only pass if the custom hash and equality methods on this class are used.
		internal int SpecialCode { get; set; } = 42;

		public bool StructuralEquals(CustomHasher? other) => other is not null && this.SpecialCode == other.SpecialCode;

		public long GetSecureHashCode() => this.SpecialCode * 2;

		public override int GetHashCode() => this.SpecialCode;
	}

	[GenerateShape]
	[DerivedTypeShape(typeof(Derived1))]
	[DerivedTypeShape(typeof(Derived2))]
	[DerivedTypeShape(typeof(Derived3))]
	[DerivedTypeShape(typeof(Derived4))]
	internal abstract partial record SomeBaseType
	{
		internal record Derived1(int Value) : SomeBaseType;

		internal record Derived2(int Value) : SomeBaseType;

		internal record Derived3 : SomeBaseType;

		[TypeShape(Marshaler = typeof(Marshaler))]
		internal record Derived4 : SomeBaseType
		{
			internal class Marshaler : IMarshaler<Derived4, Marshaler.Surrogate?>
			{
				public Surrogate? Marshal(Derived4? value) => value is null ? null : default(Surrogate);

				public Derived4? Unmarshal(Surrogate? value) => value is null ? null : new Derived4();

				internal struct Surrogate;
			}
		}
	}
}
