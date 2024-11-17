// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

partial class IncludingExcludingMembers
{
	#region IncludingExcludingMembers
	class MyType
	{
		[PropertyShape(Ignore = true)] // exclude this property from serialization
		public string? MyName { get; set; }

		[PropertyShape] // include this non-public property in serialization
		internal string? InternalMember { get; set; }
	}
	#endregion
}

partial class ChangingPropertyNames
{
	#region ChangingPropertyNames
	class MyType
	{
		[PropertyShape(Name = "name")] // serialize this property as "name"
		public string? MyName { get; set; }
	}
	#endregion
}

partial class ApplyNamePolicy
{
	class MyType
	{
		void Example()
		{
			#region ApplyNamePolicy
			var serializer = new MessagePackSerializer
			{
				PropertyNamingPolicy = MessagePackNamingPolicy.CamelCase,
			};
			#endregion
		}
	}
}

namespace DeserializingConstructors
{
	#region DeserializingConstructors
	[GenerateShape]
	partial class ImmutablePerson
	{
		public ImmutablePerson(string? name)
		{
			this.Name = name;
		}

		public string? Name { get; }
	}
	#endregion
}

namespace DeserializingConstructorsPropertyRenamed
{
	#region DeserializingConstructorsPropertyRenamed
	[GenerateShape]
	partial class ImmutablePerson
	{
		public ImmutablePerson(string? name)
		{
			this.Name = name;
		}

		[PropertyShape(Name = "person_name")]
		public string? Name { get; }
	}
	#endregion
}

namespace SerializeWithKey
{
	#region SerializeWithKey
	[GenerateShape]
	partial class MyType
	{
		[Key(0)]
		public string? OneProperty { get; set; }

		[Key(1)]
		public string? AnotherProperty { get; set; }
	}
	#endregion
}
