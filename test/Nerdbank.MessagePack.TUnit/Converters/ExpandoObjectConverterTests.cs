// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This file exercises System.Dynamic/ExpandoObject's dynamic (DLR) dispatch, which requires runtime code
// generation and is therefore incompatible with NativeAOT. Each affected test detects this at runtime via
// RuntimeFeature.IsDynamicCodeCompiled and skips gracefully (see Skip.Test calls below), so it is safe
// to suppress the trim/AOT analysis warnings on these methods.
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.CompilerServices;

namespace Converters;

public partial class ExpandoObjectConverterTests : MessagePackSerializerTestBase
{
	public ExpandoObjectConverterTests()
	{
		this.Serializer = this.Serializer.WithExpandoObjectConverter();
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void SimplePropertyValues()
	{
#if NET
		if (!RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("ExpandoObject's dynamic dispatch requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		dynamic e = new ExpandoObject();
		e.a = 5;
		e.b = "hi";
		e.Nothing = null;

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal(5, (int)e2!.a);
		Assert.Equal("hi", (string?)e2.b);
		Assert.Null(e2!.Nothing);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void Depth()
	{
#if NET
		if (!RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("ExpandoObject's dynamic dispatch requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		dynamic e = new ExpandoObject();
		e.a = new byte[] { 1, 2 };
		e.b = new ExpandoObject();
		e.b.c = "bah";

		dynamic? e2 = this.Roundtrip<ExpandoObject, Witness>(e);
		Assert.Equal<byte>([1, 2], (byte[])e2.a);
		Assert.Equal("bah", e2.b.c);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void MaxPropertyCountHonoredOnSerialization()
	{
#if NET
		if (!RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("ExpandoObject's dynamic dispatch requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with
			{
				Security = this.Serializer.StartingContext.Security with
				{
					ExpandoObjectMaxPropertyCount = 2,
				},
			},
		};

		dynamic e = new ExpandoObject();
		e.a = 1;
		e.b = 2;
		e.c = 3;

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Serialize<ExpandoObject, Witness>((ExpandoObject)e, this.TimeoutToken));
		Console.WriteLine(ex.GetBaseException().Message);
		Assert.Contains($"3 properties", ex.GetBaseException().Message);
	}

	[Test]
#if NET
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Test detects and skips when dynamic code isn't supported.")]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Test detects and skips when dynamic code isn't supported.")]
#endif
	public void MaxPropertyCountHonoredOnDeserialization()
	{
#if NET
		if (!RuntimeFeature.IsDynamicCodeCompiled)
		{
			Skip.Test("ExpandoObject's dynamic dispatch requires runtime code generation, which is not available under NativeAOT.");
		}
#endif

		const int MaxPropertyCount = 2;
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with
			{
				Security = this.Serializer.StartingContext.Security with
				{
					ExpandoObjectMaxPropertyCount = MaxPropertyCount,
				},
			},
		};

		dynamic? e = this.Serializer.Deserialize<ExpandoObject, Witness>(ConstructMap(MaxPropertyCount), this.TimeoutToken);
		Assert.Equal(1, (int)e?.A);

		MessagePackSerializationException ex = Assert.Throws<MessagePackSerializationException>(
			() => this.Serializer.Deserialize<ExpandoObject, Witness>(ConstructMap(MaxPropertyCount + 1), this.TimeoutToken));
		Console.WriteLine(ex.GetBaseException().Message);
		Assert.Contains($"{MaxPropertyCount + 1} properties", ex.GetBaseException().Message);

		ReadOnlySequence<byte> ConstructMap(int count)
		{
			Sequence<byte> msgpack = new();
			MessagePackWriter writer = new(msgpack);
			writer.WriteMapHeader(count);
			for (int i = 0; i < count; i++)
			{
				writer.Write($"{(char)('A' + i)}");
				writer.Write(i + 1);
			}

			writer.Flush();
			this.LogMsgPack(msgpack);
			return msgpack;
		}
	}

	[Test]
	public void Null()
	{
		Assert.Null(this.Roundtrip<ExpandoObject, Witness>(null));
	}

	[GenerateShapeFor<int>]
	[GenerateShapeFor<ExpandoObject>]
	private partial class Witness;
}
