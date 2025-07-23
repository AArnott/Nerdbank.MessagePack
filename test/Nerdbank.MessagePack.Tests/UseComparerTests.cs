// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0008 // Use explicit type

public partial class UseComparerTests : MessagePackSerializerTestBase
{
	[Fact]
	public void EqualityComparerByStaticMember()
	{
		var deserialized = this.Roundtrip(new UsesStaticEqualityComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void ComparerByStaticMember()
	{
		var deserialized = this.Roundtrip(new UsesStaticComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void EqualityComparerByInstanceMember()
	{
		var deserialized = this.Roundtrip(new UsesInstanceEqualityComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void ComparerByInstanceMember()
	{
		var deserialized = this.Roundtrip(new UsesInstanceComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void EqualityComparerByType()
	{
		var deserialized = this.Roundtrip(new UsesInstanceEqualityComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void ComparerByType()
	{
		var deserialized = this.Roundtrip(new UsesInstanceComparer());
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[Fact]
	public void ComparerOnParameter()
	{
		var deserialized = this.Roundtrip(new ComparerOnConstructorParameter(new()));
		Assert.Same(StringComparer.OrdinalIgnoreCase, deserialized?.StringInt.Comparer);
	}

	[GenerateShape]
	public partial class UsesStaticEqualityComparer
	{
		[UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
		public Dictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial class UsesStaticComparer
	{
		[UseComparer(typeof(StringComparer), nameof(StringComparer.OrdinalIgnoreCase))]
		public SortedDictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial class UsesInstanceEqualityComparer
	{
		[UseComparer(typeof(MyComparers), nameof(MyComparers.EqualityComparer))]
		public Dictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial class UsesInstanceComparer
	{
		[UseComparer(typeof(MyComparers), nameof(MyComparers.Comparer))]
		public SortedDictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial class UsesTypeEqualityComparer
	{
		[UseComparer(typeof(MyComparer))]
		public Dictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial class UsesTypeComparer
	{
		[UseComparer(typeof(MyComparer))]
		public SortedDictionary<string, int> StringInt { get; set; } = new();
	}

	[GenerateShape]
	public partial record ComparerOnConstructorParameter(
		[UseComparer(typeof(MyComparers), nameof(MyComparers.EqualityComparer))] Dictionary<string, int> StringInt);

	private class MyComparers
	{
		public IEqualityComparer<string> EqualityComparer => StringComparer.OrdinalIgnoreCase;

		public IComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;
	}

	private class MyComparer : IEqualityComparer<string>, IComparer<string>
	{
		public int Compare(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Compare(x, y);

		public bool Equals(string? x, string? y) => StringComparer.OrdinalIgnoreCase.Equals(x, y);

		public int GetHashCode(string obj) => StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
	}
}
