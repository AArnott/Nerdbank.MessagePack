// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public partial class CustomConverterFactoryTests(ITestOutputHelper logger) : MessagePackSerializerTestBase(logger)
{
	[RpcMarshaled, GenerateShape]
	internal partial interface IMarshaledInterface
	{
		Task DoSomethingAsync();
	}

	[Fact]
	public void CustomUnionSerializer()
	{
		this.Serializer.RegisterConverterFactory(new CustomUnionConverterFactory());

		A? a = this.Roundtrip(new A());
		Assert.NotNull(a);
		Assert.True(a.CustomSerialized);

		A? bAsA = this.Roundtrip<A>(new B());
		Assert.IsType<B>(bAsA);
		Assert.True(bAsA.CustomSerialized);

		B? bAsB = this.Roundtrip(new B());
		Assert.IsType<B>(bAsB);
		Assert.True(bAsB.CustomSerialized);
	}

	[Fact]
	public void MarshaledInterfaceSerializer()
	{
		this.Serializer.RegisterConverterFactory(new MarshaledObjectConverterFactory());

		MarshaledObject obj = new();
		IMarshaledInterface? proxy = this.Roundtrip<IMarshaledInterface>(obj);
		Assert.IsType<MarshaledInterfaceProxy>(proxy);
	}

	[Fact]
	[Trait("ReferencePreservation", "true")]
	public void FactoryWithReferencePreservation()
	{
		this.Serializer = this.Serializer with { PreserveReferences = ReferencePreservationMode.RejectCycles };
		this.Serializer.RegisterConverterFactory(new CustomUnionConverterFactory());

		A a = new A();
		A[] array = [a, a];

		A[]? deserialized = this.Roundtrip<A[], Witness>(array);

		Assert.NotNull(deserialized);
		Assert.Same(deserialized[0], deserialized[1]);
		Assert.True(deserialized[0].CustomSerialized);
	}

	[GenerateShape, TypeShape(Kind = TypeShapeKind.None)]
	internal partial class A
	{
		public bool CustomSerialized { get; set; }
	}

	[GenerateShape, TypeShape(Kind = TypeShapeKind.None)]
	internal partial class B : A;

	internal class MarshaledObject : IMarshaledInterface
	{
		public Task DoSomethingAsync() => throw new NotImplementedException();
	}

	internal class MarshaledInterfaceProxy : IMarshaledInterface
	{
		public Task DoSomethingAsync() => throw new NotImplementedException();
	}

	[AttributeUsage(AttributeTargets.Interface)]
	internal class RpcMarshaledAttribute : Attribute;

	internal class MarshaledObjectConverterFactory : IMessagePackConverterFactory
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
			if (reader.TryReadNil())
			{
				return default;
			}

			return reader.ReadString() switch
			{
				nameof(IMarshaledInterface) => (T)(object)new MarshaledInterfaceProxy(),
				_ => throw new NotImplementedException(),
			};
		}

		public override void Write(ref MessagePackWriter writer, in T? value, SerializationContext context)
		{
			if (value is null)
			{
				writer.WriteNil();
				return;
			}

			writer.Write(typeof(T).Name);
		}
	}

	internal class CustomUnionConverterFactory : IMessagePackConverterFactory
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

	private class CustomUnionConverter<T> : MessagePackConverter<T>
	{
		public override T? Read(ref MessagePackReader reader, SerializationContext context)
		{
			if (reader.TryReadNil())
			{
				return default;
			}

			A result = reader.ReadString() switch
			{
				"A" => new A(),
				"B" => new B(),
				_ => throw new InvalidOperationException("Unknown type"),
			};
			result.CustomSerialized = true;
			return (T)(object)result;
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

	[GenerateShape<A[]>]
	private partial class Witness;
}
