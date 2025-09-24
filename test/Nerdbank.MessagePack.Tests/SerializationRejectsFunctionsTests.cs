// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class SerializationRejectsFunctionsTests : MessagePackSerializerTestBase
{
	[Fact]
	public void UnserializedDelegateTypesDoNotBreakSerialization()
	{
		this.AssertRoundtrip(new TypeWithUnserializedPropertyWithFunctionProperty { SimpleProperty = 42 });
	}

	[GenerateShape]
	public partial record TypeWithUnserializedPropertyWithFunctionProperty
	{
		public int SimpleProperty { get; set; }

		public TypeWithFunctionProperty Unserialized => throw new NotImplementedException();
	}

	public class TypeWithFunctionProperty
	{
		public Func<int>? FunctionProperty { get; set; }
	}
}
