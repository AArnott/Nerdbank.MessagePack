// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SurrogateTests
{
	[GenerateShape(Marshaler = typeof(Marshaler))]
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

		internal int GetA() => this.a;

		internal int GetB() => this.b;

		internal record struct MarshaledType(int A, int B);

		internal class Marshaler : IMarshaler<OriginalType, MarshaledType?>
		{
			public OriginalType? Unmarshal(MarshaledType? surrogate)
				=> surrogate.HasValue ? new(surrogate.Value.A, surrogate.Value.B) : null;

			public MarshaledType? Marshal(OriginalType? value)
				=> value is null ? null : new(value.a, value.b);
		}
	}
}
