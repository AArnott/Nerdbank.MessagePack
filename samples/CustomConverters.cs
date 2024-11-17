// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CustomConverter;
using Nerdbank.MessagePack;

namespace CustomConverter
{
	#region YourOwnConverter
	using Nerdbank.MessagePack;

	public record Foo(int MyProperty1, string? MyProperty2);

	class FooConverter : MessagePackConverter<Foo?>
	{
		public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			int property1 = 0;
			string? property2 = null;

			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				string? key = reader.ReadString();
				switch (key)
				{
					case "MyProperty":
						property1 = reader.ReadInt32();
						break;
					case "MyProperty2":
						property2 = reader.ReadString();
						break;
					default:
						// Skip the value, as we don't know where to put it.
						reader.Skip(context);
						break;
				}
			}

			return new Foo(property1, property2);
		}

		public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteMapHeader(2);

			writer.Write("MyProperty");
			writer.Write(value.MyProperty1);

			writer.Write("MyProperty2");
			writer.Write(value.MyProperty2);
		}
	}
	#endregion

	class VersionSafeConverter : MessagePackConverter<Foo>
	{
		public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			#region ReadWholeArray
			int property1 = 0;
			string? property2 = null;
			int count = reader.ReadArrayHeader();
			for (int i = 0; i < count; i++)
			{
				switch (i)
				{
					case 0:
						property1 = reader.ReadInt32();
						break;
					case 1:
						property2 = reader.ReadString();
						break;
					default:
						// Skip the value, as we don't know where to put it.
						reader.Skip(context);
						break;
				}
			}

			return new Foo(property1, property2);
			#endregion
		}

		public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
		{
			throw new NotImplementedException();
		}
	}
}

namespace SubValues
{
	using Nerdbank.MessagePack;

	public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

	[GenerateShape]
	public partial record SomeOtherType;

	class FooConverter : MessagePackConverter<Foo?>
	{
		public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			SomeOtherType? property1 = null;
			string? property2 = null;

			int count = reader.ReadMapHeader();
			for (int i = 0; i < count; i++)
			{
				string? key = reader.ReadString();
				switch (key)
				{
					case "MyProperty":
						property1 = context.GetConverter<SomeOtherType>().Read(ref reader, context);
						break;
					case "MyProperty2":
						property2 = reader.ReadString();
						break;
					default:
						// Skip the value, as we don't know where to put it.
						reader.Skip(context);
						break;
				}
			}

			return new Foo(property1, property2);
		}

		#region DelegateSubValues
		public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.WriteMapHeader(2);

			writer.Write("MyProperty");
			SomeOtherType? propertyValue = value.MyProperty1;
			context.GetConverter<SomeOtherType>().Write(ref writer, propertyValue, context);

			writer.Write("MyProperty2");
			writer.Write(value.MyProperty2);
		}
		#endregion
	}
}

namespace SubValuesWithWitness
{
	using Nerdbank.MessagePack;

	public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

	#region WitnessOnFormatter
	// SomeOtherType is outside your assembly and not attributed.
	public partial record SomeOtherType;

	[GenerateShape<SomeOtherType>] // allow FooConverter to provide the shape for SomeOtherType
	partial class FooConverter : MessagePackConverter<Foo?>
	{
		public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
		{
			// ...
			context.GetConverter<SomeOtherType, FooConverter>().Read(ref reader, context);
			// ...
			#endregion

			throw new NotImplementedException();
		}

		public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
		{
			throw new NotImplementedException();
		}
	}
}

namespace WitnessForArray
{
	using Nerdbank.MessagePack;

	public record Foo(SomeOtherType? MyProperty1, string? MyProperty2);

	#region ArrayWitnessOnFormatter
	// SomeOtherType is outside your assembly and not attributed.
	public partial record SomeOtherType;

	[GenerateShape<SomeOtherType[]>]
	partial class FooConverter : MessagePackConverter<Foo?>
	{
		public override Foo? Read(ref MessagePackReader reader, SerializationContext context)
		{
			// ...
			context.GetConverter<SomeOtherType[], FooConverter>().Read(ref reader, context);
			// ...
			#endregion

			throw new NotImplementedException();
		}

		public override void Write(ref MessagePackWriter writer, in Foo? value, SerializationContext context)
		{
			throw new NotImplementedException();
		}
	}
}

namespace CustomConverterRegistration
{
	#region CustomConverterByAttribute
	[MessagePackConverter(typeof(MyCustomTypeConverter))]
	public class MyCustomType { }
	#endregion

	public class MyCustomTypeConverter : MessagePackConverter<MyCustomType>
	{
		public override MyCustomType? Read(ref MessagePackReader reader, SerializationContext context)
		{
			throw new NotImplementedException();
		}

		public override void Write(ref MessagePackWriter writer, in MyCustomType? value, SerializationContext context)
		{
			throw new NotImplementedException();
		}
	}

	class CustomConverterByRegister
	{
		void Main()
		{
			#region CustomConverterByRegister
			MessagePackSerializer serializer = new();
			serializer.RegisterConverter(new MyCustomTypeConverter());
			#endregion
		}
	}
}
