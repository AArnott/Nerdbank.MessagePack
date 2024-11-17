// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sample1
{
	#region Farm
	public class Farm
	{
		public List<Animal>? Animals { get; set; }
	}
	#endregion

	#region FarmAnimals
	[KnownSubType<Cow>(1)]
	[KnownSubType<Horse>(2)]
	[KnownSubType<Dog>(3)]
	public class Animal
	{
		public string? Name { get; set; }
	}

	[GenerateShape]
	public partial class Cow : Animal { }
	[GenerateShape]
	public partial class Horse : Animal { }
	[GenerateShape]
	public partial class Dog : Animal { }
	#endregion

	#region HorsePen
	public class HorsePen
	{
		public List<Horse>? Horses { get; set; }
	}
	#endregion

	#region HorseBreeds
	[KnownSubType<QuarterHorse>(1)]
	[KnownSubType<Thoroughbred>(2)]
	public partial class Horse : Animal { }

	[GenerateShape]
	public partial class QuarterHorse : Horse { }
	[GenerateShape]
	public partial class Thoroughbred : Horse { }
	#endregion
}

namespace GenericSubTypes
{
	#region ClosedGenericSubTypes
	[KnownSubType<Horse>(1)]
	[KnownSubType<Cow<SolidHoof>, Witness>(2)]
	[KnownSubType<Cow<ClovenHoof>, Witness>(3)]
	class Animal
	{
		public string? Name { get; set; }
	}

	[GenerateShape]
	partial class Horse : Animal { }

	partial class Cow<THoof> : Animal { }

	[GenerateShape<Cow<SolidHoof>>]
	[GenerateShape<Cow<ClovenHoof>>]
	partial class Witness;

	class SolidHoof { }

	class ClovenHoof { }
	#endregion
}
