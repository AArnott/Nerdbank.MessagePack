// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

public partial class CustomConverterFactoryTests : MessagePackSerializerTestBase
{
	[RpcMarshaled, GenerateShape]
	internal partial interface IMarshaledInterface
	{
		Task DoSomethingAsync();
	}

	[Test]
	public void CustomUnionSerializer()
	{
		this.Serializer = this.Serializer with { ConverterFactories = [new CustomUnionConverterFactory()] };

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

	[Test]
	public void MarshaledInterfaceSerializer()
	{
		this.Serializer = this.Serializer with { ConverterFactories = [new MarshaledObjectConverterFactory()] };

		MarshaledObject obj = new();
		IMarshaledInterface? proxy = this.Roundtrip<IMarshaledInterface>(obj);
		Assert.IsType<MarshaledInterfaceProxy>(proxy);
	}

	[Test]
	[Property("ReferencePreservation", "true")]
	public void FactoryWithReferencePreservation()
	{
		this.Serializer = this.Serializer with
		{
			PreserveReferences = ReferencePreservationMode.RejectCycles,
			ConverterFactories = [new CustomUnionConverterFactory()],
		};

		A a = new A();
		A[] array = [a, a];

		A[]? deserialized = this.Roundtrip<A[], Witness>(array);

		Assert.NotNull(deserialized);
		Assert.Same(deserialized[0], deserialized[1]);
		Assert.True(deserialized[0].CustomSerialized);
	}

	[Test]
	public void GetSubConverterFromContext()
	{
		this.Serializer = this.Serializer with { ConverterFactories = [new DoubleArrayWrapperFactory()] };

		List<char> list = ['a', 'b', 'c'];
		ReadOnlySequence<byte> msgpack = this.AssertRoundtrip<List<char>, Witness>(list);

		// Verify that the list was in fact double-wrapped, indicating the custom factory was used.
		MessagePackReader reader = new(msgpack);
		Assert.Equal(1, reader.ReadArrayHeader());
		Assert.Equal(3, reader.ReadArrayHeader());
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

	internal class DoubleArrayWrapperFactory : IMessagePackConverterFactory
	{
		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		{
			if (shape is IEnumerableTypeShape { Type.IsGenericType: true, ElementType: ITypeShape elementShape } enumShape && shape.Type.GetGenericTypeDefinition() == typeof(List<>))
			{
				return (MessagePackConverter?)shape.Accept(new Visitor(), context);
			}

			return null;
		}

		private class Visitor : TypeShapeVisitor
		{
			public override object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
			{
				ConverterContext context = (ConverterContext)state!;
				MessagePackConverter<TElement> subconverter = context.GetConverter(enumerableShape.ElementType);
				Assert.Same(subconverter, context.GetConverter((ITypeShape)enumerableShape.ElementType));
				Assert.Same(subconverter, context.GetConverter(enumerableShape.ElementType.Type));
				Assert.Same(subconverter, context.GetConverter(enumerableShape.ElementType.Type, enumerableShape.ElementType.Provider));
				Assert.Same(subconverter, context.GetConverter<TElement>(context.TypeShapeProvider));

				return new Converter<TElement>(subconverter);
			}
		}

		private class Converter<T>(MessagePackConverter<T> elementConverter) : MessagePackConverter<List<T>>
		{
			public override List<T>? Read(ref MessagePackReader reader, SerializationContext context)
			{
				if (reader.TryReadNil())
				{
					return null;
				}

				Assert.Equal(1, reader.ReadArrayHeader());
				int len = reader.ReadArrayHeader();
				List<T> result = new(len);
				for (int i = 0; i < len; i++)
				{
					result.Add(elementConverter.Read(ref reader, context)!);
				}

				return result;
			}

			public override void Write(ref MessagePackWriter writer, in List<T>? value, SerializationContext context)
			{
				if (value is null)
				{
					writer.WriteNil();
					return;
				}

				writer.WriteArrayHeader(1);
				writer.WriteArrayHeader(value.Count);
				for (int i = 0; i < value.Count; i++)
				{
					elementConverter.Write(ref writer, value[i], context);
				}
			}
		}
	}

	internal class MarshaledObjectConverterFactory : IMessagePackConverterFactory, ITypeShapeFunc
	{
		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		{
			return shape?.Type.GetCustomAttribute<RpcMarshaledAttribute>() is null ? null : this.Invoke(shape);
		}

		object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => new MarshaledObjectConverter<T>();
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

	internal class CustomUnionConverterFactory : IMessagePackConverterFactory, ITypeShapeFunc
	{
		public MessagePackConverter? CreateConverter(Type type, ITypeShape? shape, in ConverterContext context)
		{
			return typeof(A).IsAssignableFrom(type) && shape is not null ? this.Invoke(shape) : null;
		}

		object? ITypeShapeFunc.Invoke<T>(ITypeShape<T> typeShape, object? state) => new CustomUnionConverter<T>();
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
				_ => throw new InvalidOperationException("Unspecified type"),
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

	[GenerateShapeFor<A[]>]
	[GenerateShapeFor<List<char>>]
	private partial class Witness;
}
