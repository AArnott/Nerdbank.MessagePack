// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public partial class CustomEnumerableTypes : MessagePackSerializerTestBase
{
	[Fact]
	public void CustomListOfIntDerivedCollection()
	{
		ListOfInt list = new() { 1, 2, 3 };
		ListOfInt? deserialized = this.Roundtrip(list);
		Assert.Equal<int>(list, deserialized);

		MessagePackReader reader = new(this.lastRoundtrippedMsgpack);
		Assert.Equal(3, reader.ReadArrayHeader());
	}

	[GenerateShape, TypeShape(Kind = TypeShapeKind.Enumerable)]
	public partial class ListOfInt : List<int>
	{
		public ListOfInt()
			: base()
		{
		}

		public ListOfInt(IEnumerable<int> values)
			: base(values)
		{
		}

		public ListOfInt(int value)
			: base(value)
		{
		}
	}
}
