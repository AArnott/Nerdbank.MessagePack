// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder;

// These tests exercise the `dynamic` keyword, which relies on the DLR and runtime code generation.
// That is not supported under NativeAOT. The DeserializePrimitives() override below (which is called
// by every test in this class, including those inherited from the base class) detects this at runtime
// via RuntimeFeature.IsDynamicCodeCompiled and skips gracefully when dynamic code generation is unavailable,
// so it is safe to suppress the trim/AOT analysis warnings on the affected methods.
[InheritsTests]
public partial class PrimitivesDynamicDeserializationTests : PrimitivesDerializationTests
{
	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void ReachIntoArray()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Equal(5, deserialized.nestedArray.Length);
		Assert.Equal(true, deserialized.nestedArray[0]);
		Assert.Equal(3.5, deserialized.nestedArray[1]);
		Assert.Equal(new Extension(15, new byte[] { 1, 2, 3 }), deserialized.nestedArray[2]);
		Assert.Equal<DateTime>(ExpectedDateTime, deserialized.nestedArray[3]);
		Assert.Equal<DateTime>(ExpectedDateTime, deserialized.nestedArray[4]);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void ReachIntoMap_Property()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Equal("nestedValue", deserialized.nestedObject.nestedProp);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void SimpleValuesByMember()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.NotNull(deserialized);
		Assert.Equal("Value1", deserialized.Prop1);
		Assert.Equal(42, (int)deserialized.Prop2);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void SimpleValuesByIndex_MultipleIndexes()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Assert.Throws<RuntimeBinderException>(() => deserialized[45, 1]);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void MissingMembers_Property()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Console.WriteLine(Assert.Throws<KeyNotFoundException>(() => deserialized.nonexistent).Message);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void WritingNotAllowed_Dynamic()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Console.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized["doesNotExist"] = "hi").Message);
		Console.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized["nestedObject"] = "hi").Message);
		Console.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.doesNotExist = 3).Message);
		Console.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.nestedObject = "hi").Message);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void InvokingMethodsNotAllowed()
	{
		dynamic deserialized = this.DeserializePrimitives();
		Console.WriteLine(Assert.Throws<RuntimeBinderException>(() => deserialized.GetEnumerator()).Message);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void Enumerate_Dynamic()
	{
		dynamic deserialized = this.DeserializePrimitives();
		foreach (object key in deserialized)
		{
			Console.WriteLine($"{key} ({key.GetType().Name}) = {deserialized[key]}");
		}

		IEnumerator enumerator = ((IEnumerable)deserialized).GetEnumerator();
		for (int i = 0; i < ExpectedKeys.Length; i++)
		{
			Assert.True(enumerator.MoveNext());
			Assert.Equal(ExpectedKeys.Span[i], enumerator.Current);
		}

		Assert.False(enumerator.MoveNext());
	}

#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	protected override IDictionary<object, object?> DeserializePrimitives()
	{
#if NET
		if (!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("dynamic requires runtime code generation, which is unavailable under NativeAOT.");
		}
#endif

		MessagePackReader reader = this.ConstructReader();
		dynamic? deserialized = this.Serializer.DeserializeDynamicPrimitives(ref reader, this.TimeoutToken);
		Assert.NotNull(deserialized);
		return deserialized!;
	}
}
