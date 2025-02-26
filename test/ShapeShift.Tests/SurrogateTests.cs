﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Trait("Surrogates", "true")]
public partial class SurrogateTests : MessagePackSerializerTestBase
{
	[Fact]
	public void NonNullReference()
	{
		OriginalType? deserialized = this.Roundtrip(new OriginalType(1, 2));
		Assert.NotNull(deserialized);
		Assert.Equal(3, deserialized.Sum);
	}

	[Fact]
	public void NullReference()
	{
		OriginalType? deserialized = this.Roundtrip<OriginalType>(null);
		Assert.Null(deserialized);
	}

	[Fact]
	public async Task NonNullReference_Async()
	{
		OriginalType? deserialized = await this.RoundtripAsync(new OriginalType(1, 2));
		Assert.NotNull(deserialized);
		Assert.Equal(3, deserialized.Sum);
	}

	[Fact]
	public async Task NullReference_Async()
	{
		OriginalType? deserialized = await this.RoundtripAsync<OriginalType>(null);
		Assert.Null(deserialized);
	}

	[Fact]
	[Trait("ReferencePreservation", "true")]
	public void ReferencePreservation()
	{
		this.Serializer = this.Serializer with { PreserveReferences = true };
		OriginalType original = new(1, 2);
		OriginalType[]? deserializedArray = this.Roundtrip<OriginalType[], Witness>([original, original]);
		Assert.NotNull(deserializedArray);
		Assert.Same(deserializedArray[0], deserializedArray[1]);
	}

	[GenerateShape]
	[TypeShape(Marshaller = typeof(Marshaller))]
	internal partial class OriginalType
	{
		private int a;
		private int b;

		internal OriginalType(int a, int b)
		{
			this.a = a;
			this.b = b;
		}

		public int Sum => this.a + this.b;

		internal record struct MarshaledType(int A, int B);

		internal class Marshaller : IMarshaller<OriginalType, MarshaledType?>
		{
			public OriginalType? FromSurrogate(MarshaledType? surrogate)
				=> surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;

			public MarshaledType? ToSurrogate(OriginalType? value)
				=> value is null ? null : new(value.a, value.b);
		}
	}

	[GenerateShape<OriginalType[]>]
	private partial class Witness;
}
