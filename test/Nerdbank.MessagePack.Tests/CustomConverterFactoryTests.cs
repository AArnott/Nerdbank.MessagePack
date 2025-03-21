// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public class CustomConverterFactoryTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[Fact]
	public void CustomUnionSerializer()
	{
		this.Serializer.RegisterConverterFactory(new CustomUnionConverterFactory());
	}

	[Fact]
	public void MarshaledInterfaceSerializer()
	{
		this.Serializer.RegisterConverterFactory(new MarshaledObjectConverterFactory());
	}

	internal class A;

	internal class B : A;

	internal class C : A;

	[RpcMarshaled]
	internal interface IMarshaledInterface
	{
		Task DoSomethingAsync();
	}

	[RpcMarshaled]
	internal interface IMarshaledInterface2
	{
		Task DoSomethingAsync();
	}

	[AttributeUsage(AttributeTargets.Interface)]
	internal class RpcMarshaledAttribute : Attribute;

	internal class MarshaledObjectConverterFactory : IConverterFactory
	{
		public MessagePackConverter<T>? CreateConverter<T>()
		{
			if (typeof(T).GetCustomAttribute<RpcMarshaledAttribute>() is null)
			{
				return null;
			}

			return new MarshaledObjectConverter<T>();
		}
	}

	internal class MarshaledObjectConverter<T> : MessagePackConverter<T>
	{
		public override T? Read(ref MessagePackReader reader, SerializationContext context)
		{
			throw new NotImplementedException();
		}

		public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
		{
			throw new NotImplementedException();
		}
	}

	internal class CustomUnionConverterFactory : IConverterFactory
	{
		public MessagePackConverter<T>? CreateConverter<T>()
		{
			if (typeof(A).IsAssignableFrom(typeof(T)))
			{
				return new CustomUnionConverter<T>();
			}

			return null;
		}
	}

	class CustomUnionConverter<T> : MessagePackConverter<T>
	{
		public override T? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return default;
			}

			return reader.ReadString() switch
			{
				"A" => (T)(object)new A(),
				"B" => (T)(object)new B(),
				"C" => (T)(object)new C(),
				_ => throw new InvalidOperationException("Unknown type"),
			};
		}

		public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write(value.GetType().Name);
		}
	}
}
