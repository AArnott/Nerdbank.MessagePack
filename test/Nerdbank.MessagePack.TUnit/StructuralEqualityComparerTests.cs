// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8767 // null ref annotations
#endif

// These exhaustive tests use reflection (MakeGenericMethod/MakeGenericType) to dispatch across every type known to
// PolyType's test case catalog, since TUnit does not support open generic test methods whose type arguments are
// inferred from a dynamically-produced data source. This is not compatible with NativeAOT, but the tests detect
// that failure at runtime and skip gracefully (see the try/catch blocks below), so the trim/AOT analysis warnings
// are suppressed via [UnconditionalSuppressMessage] on the two affected test methods below.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using PolyType.Tests;

public abstract partial class StructuralEqualityComparerTests
{
	private static readonly MethodInfo GetEqualityComparerOpenMethod = typeof(StructuralEqualityComparerTests)
		.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
		.Single(m => m.Name == nameof(GetEqualityComparer) && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 1);

	internal enum FruitKind
	{
		Apple,
		Banana,
	}

	/// <summary>
	/// Exhaustively verifies <see cref="IEqualityComparer{T}.Equals(T, T)"/> for every test case type known to PolyType.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This test takes the non-generic <see cref="ITestCase"/> and dispatches to the generic
	/// <see cref="GetEqualityComparer{T}(ITypeShape{T})"/> method via reflection, since TUnit does not support
	/// open generic test methods whose type arguments are inferred from a dynamically-produced data source
	/// (unlike xunit's <c>Theory</c>/<c>MemberData</c> combination, which this test used prior to migration).
	/// </para>
	/// <para>
	/// Because this relies on <see cref="MethodInfo.MakeGenericMethod(Type[])"/> at runtime, it is not compatible
	/// with NativeAOT and is skipped automatically when dynamic code generation is unavailable.
	/// </para>
	/// </remarks>
	[Test]
	[MethodDataSource(typeof(TestTypes), nameof(TestTypes.GetTestCases))]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void Equals_Exhaustive(ITestCase testCase)
	{
		// We do not expect these cases to work.
		Skip.When(testCase.Type == typeof(object), "T = object");

		object equalityComparer;
		try
		{
			equalityComparer = GetEqualityComparerOpenMethod.MakeGenericMethod(testCase.Type).Invoke(this, [testCase.DefaultShape])!;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException nse)
		{
			// We don't expect all types to be supported.
			Skip.Test($"Unsupported: {nse.Message}");
			return;
		}
		catch (Exception ex) when (ex is NotSupportedException or PlatformNotSupportedException)
		{
			// MakeGenericMethod requires runtime code generation, which is unavailable when published for NativeAOT.
			Skip.Test($"This test requires runtime code generation, which is unavailable in this environment: {ex.Message}");
			return;
		}

		MethodInfo equalsMethod = typeof(IEqualityComparer<>).MakeGenericType(testCase.Type).GetMethod(nameof(IEqualityComparer<object>.Equals))!;
		Assert.True((bool)equalsMethod.Invoke(equalityComparer, [testCase.Value, testCase.Value])!);
	}

	/// <summary>
	/// Exhaustively verifies <see cref="IEqualityComparer{T}.GetHashCode(T)"/> for every test case type known to PolyType.
	/// </summary>
	/// <remarks>
	/// See remarks on <see cref="Equals_Exhaustive(ITestCase)"/> for why this uses reflection instead of an open generic test method.
	/// </remarks>
	[Test]
	[MethodDataSource(typeof(TestTypes), nameof(TestTypes.GetTestCases))]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void GetHashCode_Exhaustive(ITestCase testCase)
	{
		// We do not expect these cases to work.
		Skip.When(testCase.Type == typeof(object), "T = object");

		object equalityComparer;
		try
		{
			equalityComparer = GetEqualityComparerOpenMethod.MakeGenericMethod(testCase.Type).Invoke(this, [testCase.DefaultShape])!;
		}
		catch (TargetInvocationException ex) when (ex.InnerException is NotSupportedException nse)
		{
			// We don't expect all types to be supported.
			Skip.Test($"Unsupported: {nse.Message}");
			return;
		}
		catch (Exception ex) when (ex is NotSupportedException or PlatformNotSupportedException)
		{
			// MakeGenericMethod requires runtime code generation, which is unavailable when published for NativeAOT.
			Skip.Test($"This test requires runtime code generation, which is unavailable in this environment: {ex.Message}");
			return;
		}

		// We don't really have anything useful to check the return value against, but
		// at least verify it doesn't throw.
		if (testCase.Value is not null)
		{
			MethodInfo getHashCodeMethod = typeof(IEqualityComparer<>).MakeGenericType(testCase.Type).GetMethod(nameof(IEqualityComparer<object>.GetHashCode))!;
			getHashCodeMethod.Invoke(equalityComparer, [testCase.Value]);
		}
	}

	[Test]
	public void Boolean() => this.AssertEqualityComparerBehavior<bool, Witness>([true], [false]);

	[Test]
	public void BigInteger() => this.AssertEqualityComparerBehavior<BigInteger, Witness>([new BigInteger(5), new BigInteger(5)], [new BigInteger(10)]);

	[Test]
	public void ByteArray()
	{
		byte[] shared = [1, 2];
		this.AssertEqualityComparerBehavior<byte[], Witness>([shared, shared, [1, 2]], [[1, 3], [1, 2, 3]]);
	}

	[Test]
	public void CustomType_Tree() => this.AssertEqualityComparerBehavior(
		[new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Apple), new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Apple)],
		[
			new Tree([new Fruit(3, "Red"), new Fruit(4, "Yellow")], 4, FruitKind.Apple),
			new Tree([new Fruit(3, "Red"), new Fruit(4, "Green")], 4, FruitKind.Banana),
			new Tree([new Fruit(4, "Red")], 4, FruitKind.Banana),
			new Tree([new Fruit(3, "Yellow")], 4, FruitKind.Apple)]);

	[Test]
	public void CustomType_Fruit() => this.AssertEqualityComparerBehavior(
		[new Fruit(3, "Red"), new Fruit(3, "Red")],
		[new Fruit(4, "Red"), new Fruit(3, "Yellow")]);

	[Test]
	public abstract void CustomHash();

	[Test]
	public void CustomHashingAndEquality() => this.AssertEqualityComparerBehavior(
		[new CustomHasher(), new CustomHasher()],
		[new CustomHasher() { SpecialCode = 33 }]);

	[Test]
	public void ReadOnlyMemoryOfByte() => this.AssertEqualityComparerBehavior(
		[new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2 }), new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2 })],
		[new HaveReadOnlyMemoryOfByte(new byte[] { 1, 3 }), new HaveReadOnlyMemoryOfByte(new byte[] { 1, 2, 3 })]);

	[Test]
	public void ReadOnlySequenceOfByte() => this.AssertEqualityComparerBehavior(
		[
			new HaveReadOnlySequenceOfByte(new([1, 2])),
			new HaveReadOnlySequenceOfByte(new([1, 2])),
			new HaveReadOnlySequenceOfByte(SequenceBuilder.Create(new byte[] { 1 }, new byte[] { 2 })),
			new HaveReadOnlySequenceOfByte(SequenceBuilder.Create(new byte[] { 1 }, ReadOnlyMemory<byte>.Empty, new byte[] { 2 })),
		],
		[new HaveReadOnlySequenceOfByte(new([1, 3])), new HaveReadOnlySequenceOfByte(new([1, 2, 3]))]);

	[Test]
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

		Console.WriteLine("Testing values that are expected to be equal:");
		foreach (T valueA in equivalent)
		{
			int valueAHashCode = eq.GetHashCode(valueA);
			Console.WriteLine($"{valueA} hash code: {valueAHashCode}");

			foreach (T valueB in equivalent)
			{
				int valueBHashCode = eq.GetHashCode(valueB);
				Console.WriteLine($"{valueB} hash code: {valueBHashCode}");
				Assert.True(eq.Equals(valueA, valueB));
				Assert.Equal(valueAHashCode, valueBHashCode);
			}
		}

		Console.WriteLine("Testing values that are expected to be different:");
		T baseline = equivalent[0];
		int baselineHashCode = eq.GetHashCode(baseline);
		foreach (T differentValue in different)
		{
			int differentValueHashCode = eq.GetHashCode(differentValue);
			Console.WriteLine($"{differentValue} hash code: {differentValueHashCode}");
			Assert.False(eq.Equals(equivalent[0], differentValue));
			Assert.NotEqual(baselineHashCode, differentValueHashCode);
		}
	}

	[InheritsTests]
	public class DefaultStructural : StructuralEqualityComparerTests
	{
		[Test]
		public override void CustomHash()
		{
			CustomHasher obj = new();
			Assert.Equal(obj.SpecialCode, this.GetEqualityComparer<CustomHasher>().GetHashCode(obj));
		}

		protected override IEqualityComparer<T> GetEqualityComparer<T>(ITypeShape<T> shape)
			=> StructuralEqualityComparer.GetDefault(shape);
	}

	[InheritsTests]
	public class HashCollisionResistant : StructuralEqualityComparerTests
	{
		[Test]
		public void Dictionary()
		{
			Dictionary<string, int> forward = new()
			{
				["a"] = 1,
				["b"] = 2,
			};
			Dictionary<string, int> reverse = new()
			{
				["b"] = 2,
				["a"] = 1,
			};

			IEqualityComparer<Dictionary<string, int>> comparer = this.GetEqualityComparer<Dictionary<string, int>, Witness>();
			Assert.True(comparer.Equals(forward, reverse));
			Assert.Equal(comparer.GetHashCode(forward), comparer.GetHashCode(reverse));
		}

		[Test]
		public void Decimal()
		{
			IEqualityComparer<decimal> comparer = this.GetEqualityComparer<decimal, Witness>();
			Assert.True(comparer.Equals(1.0m, 1.00m));
			Assert.Equal(comparer.GetHashCode(1.0m), comparer.GetHashCode(1.00m));
			Assert.True(comparer.Equals(123.4500m, 123.45m));
			Assert.Equal(comparer.GetHashCode(123.4500m), comparer.GetHashCode(123.45m));
			Assert.True(comparer.Equals(0m, new decimal(0, 0, 0, isNegative: true, scale: 1)));
			Assert.Equal(comparer.GetHashCode(0m), comparer.GetHashCode(new decimal(0, 0, 0, isNegative: true, scale: 1)));
		}

		[Test]
		public void Uri()
		{
			IEqualityComparer<Uri> comparer = this.GetEqualityComparer<Uri, Witness>();
			Uri first = new("https://example.com/path?query=value");
			Uri second = new("https://example.com/path?query=value");
			Uri relativeFirst = new("relative/path?query=value", UriKind.Relative);
			Uri relativeSecond = new("relative/path?query=value", UriKind.Relative);

			Assert.True(comparer.Equals(first, second));
			Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
			Assert.True(comparer.Equals(relativeFirst, relativeSecond));
			Assert.Equal(comparer.GetHashCode(relativeFirst), comparer.GetHashCode(relativeSecond));
		}

		[Test]
		public void Extension_IgnoresSegmentBoundaries()
		{
			IEqualityComparer<Extension> comparer = this.GetEqualityComparer<Extension>();
			Extension contiguous = new(5, new byte[] { 1, 2 });
			Extension segmented = new(5, SequenceBuilder.Create(new byte[] { 1 }, new byte[] { 2 }));
			Extension segmentedWithEmpty = new(5, SequenceBuilder.Create(new byte[] { 1 }, ReadOnlyMemory<byte>.Empty, new byte[] { 2 }));

			Assert.True(comparer.Equals(contiguous, segmented));
			Assert.Equal(comparer.GetHashCode(contiguous), comparer.GetHashCode(segmented));
			Assert.True(comparer.Equals(contiguous, segmentedWithEmpty));
			Assert.Equal(comparer.GetHashCode(contiguous), comparer.GetHashCode(segmentedWithEmpty));
		}

		[Test]
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
	[GenerateShapeFor<byte[]>]
	[GenerateShapeFor<decimal>]
	[GenerateShapeFor<Dictionary<string, int>>]
	[GenerateShapeFor<Uri>]
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
