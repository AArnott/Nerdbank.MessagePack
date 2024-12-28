// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Nerdbank.MessagePack;
using PolyType;

GenericData<int> data = new() { Value = 42 };

MessagePackSerializer serializer = new();
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
