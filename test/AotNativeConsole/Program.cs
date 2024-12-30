// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Nerdbank.MessagePack;
using PolyType;
using PolyType.Abstractions;

GenericData<int> data = new() { Value = 42 };

MessagePackSerializer serializer = new()
{
	// Until the ordinary ITypeShapeProvider interface allows acquisition of shapes for generic types
	// given the unbound type and type arguments, we have to provide an implementation of our own.
	GenericShapeProvider = new TypeShapeProvider2(Witness.ShapeProvider),
};

GenericData<int> deserializedTree = serializer.Deserialize<GenericData<int>, Witness>(serializer.Serialize<GenericData<int>, Witness>(data))!;
Console.WriteLine($"Value: {deserializedTree.Value}");

#pragma warning disable NBMsgPack020
[MessagePackConverter(typeof(GenericDataConverter<>))]
public partial record GenericData<T>
{
	internal T? Value { get; set; }
}

partial class GenericDataConverter<T> : MessagePackConverter<GenericData<T>>
{
	public GenericDataConverter()
	{
	}

	public override GenericData<T>? Read(ref MessagePackReader reader, SerializationContext context)
	{
		if (reader.TryReadNil())
		{
			return null;
		}

		int count = reader.ReadArrayHeader();
		if (count != 1)
		{
			throw new MessagePackSerializationException("Expected array of length 1.");
		}

		return new GenericData<T>
		{
			Value = GetTConverter(ref context).Read(ref reader, context),
		};
	}

	public override void Write(ref MessagePackWriter writer, in GenericData<T>? value, SerializationContext context)
	{
		if (value is null)
		{
			writer.WriteNil();
			return;
		}

		writer.WriteArrayHeader(1);
		GetTConverter(ref context).Write(ref writer, value.Value, context);
	}

	private static MessagePackConverter<T> GetTConverter(ref SerializationContext context)
		=> (MessagePackConverter<T>)context.GetConverter(typeof(T), null);
}

[GenerateShape<GenericData<int>>]
[GenerateShape<GenericDataConverter<int>>]
[GenerateShape<int>] // PolyType: why is this necessary? Shouldn't it be inferred?
partial class Witness;

/// <summary>
/// This implementation is hand-written but entirely generatable by PolyType's source generator, in theory.
/// </summary>
class TypeShapeProvider2(ITypeShapeProvider inner) : ITypeShapeProvider2
{
	private List<(Type Unbound, ReadOnlyMemory<Type> TypeArgs, Type BoundType)> sourceGenerated =
	[
		(typeof(GenericDataConverter<>), new Type[] { typeof(int) }, typeof(GenericDataConverter<int>)!),
	];

	public ITypeShape? GetShape(Type unboundGenericType, ReadOnlySpan<Type> genericTypeArguments)
	{
		foreach (var item in sourceGenerated)
		{
			if (item.Unbound.IsEquivalentTo(unboundGenericType) && item.TypeArgs.Span.SequenceEqual(genericTypeArguments))
			{
				return inner.GetShape(item.BoundType);
			}
		}

		return null;
	}
}
