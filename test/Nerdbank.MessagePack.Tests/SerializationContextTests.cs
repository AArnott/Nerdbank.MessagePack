// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET
#pragma warning disable CS1574 // unresolvable cref
#endif

public partial class SerializationContextTests
{
	/// <summary>
	/// Verifies that the <see cref="SerializationContext.GetConverter{T}()"/> method throws when not within a serialization operation.
	/// </summary>
	[Fact]
	public void GetConverterThrows()
	{
		SerializationContext context = new();
		Assert.Throws<InvalidOperationException>(() => context.GetConverter<MyType>());
	}

	[Fact]
	public void DepthStep_ThrowsOnCancellation()
	{
		CancellationTokenSource cts = new();
		SerializationContext context = new() { CancellationToken = cts.Token };
		context.DepthStep();
		cts.Cancel();
		Assert.Throws<OperationCanceledException>(context.DepthStep);
	}

	[Fact]
	public void DepthStep_ThrowsOnStackDepth()
	{
		SerializationContext context = new() { MaxDepth = 2 };
		context.DepthStep();
		context.DepthStep();
		Assert.Throws<SerializationException>(context.DepthStep);
	}

	[Fact]
	public void StateDictionary_Add_Remove()
	{
		SerializationContext context = new()
		{
			["first"] = "FIRST",
		};
		Assert.Equal("FIRST", context["first"]);

		// Test key removal.
		context["first"] = null;
		Assert.Null(context["first"]);
	}

	[Fact]
	public void StateDictionary_NonExistent()
	{
		SerializationContext context = new();
		Assert.Null(context["DOESnotEXIST"]);
	}

	[Fact]
	public void StateDictionary_PersistentCollection()
	{
		SerializationContext original = new()
		{
			["first"] = "FIRST",
		};

		SerializationContext derived = original;
		derived["second"] = "SECOND";

		// Both contexts have the original key.
		Assert.Equal("FIRST", original["first"]);
		Assert.Equal("FIRST", derived["first"]);

		// Only the derived context has the second.
		Assert.Null(original["second"]);
		Assert.Equal("SECOND", derived["second"]);
	}

	[GenerateShape]
	public partial class MyType;
}
