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

	[Fact]
	public void InitialCapacityMatchesSize()
	{
		ListOfInt list = new() { 1, 2, 3 };
		ListOfInt? deserialized = this.Roundtrip(list);
		Assert.Equal(list.Count, deserialized!.InitialCapacity);
	}

	[Fact]
	public void InitialCapacityHonorsCap()
	{
		this.Serializer = this.Serializer with
		{
			StartingContext = this.Serializer.StartingContext with
			{
				Security = this.Serializer.StartingContext.Security with
				{
					MaxCollectionPreallocation = 2,
				},
			},
		};

		ListOfInt list = new() { 1, 2, 3 };
		ListOfInt? deserialized = this.Roundtrip(list);
		Assert.Equal(2, deserialized!.InitialCapacity);
		Assert.Equal([1, 2, 3], list);
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

		public ListOfInt(int capacity)
			: base(capacity)
		{
			this.InitialCapacity = capacity;
		}

		internal int? InitialCapacity { get; }
	}
}
