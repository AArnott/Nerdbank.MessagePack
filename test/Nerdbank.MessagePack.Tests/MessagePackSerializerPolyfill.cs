// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET

using System.Text.Json.Nodes;

internal static partial class MessagePackSerializerPolyfill
{
	internal static byte[] Serialize<T>(this MessagePackSerializer serializer, in T? value)
		=> serializer.Serialize(value, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static byte[] Serialize<T, TProvider>(this MessagePackSerializer serializer, in T? value)
		=> serializer.Serialize(value, MessagePackSerializerTestBase.GetShapeProvider<TProvider>());

	internal static void Serialize<T>(this MessagePackSerializer serializer, IBufferWriter<byte> writer, in T? value)
		=> serializer.Serialize(writer, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static void Serialize<T>(this MessagePackSerializer serializer, Stream stream, in T? value)
		=> serializer.Serialize(stream, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static ValueTask SerializeAsync<T>(this MessagePackSerializer serializer, Stream writer, in T? value)
		=> serializer.SerializeAsync(writer, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static T? Deserialize<T, TProvider>(this MessagePackSerializer serializer, byte[] sequence)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<TProvider>());

	internal static T? Deserialize<T, TProvider>(this MessagePackSerializer serializer, ReadOnlySequence<byte> sequence)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<TProvider>());

	internal static T? Deserialize<T>(this MessagePackSerializer serializer, ReadOnlySequence<byte> sequence)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static T? Deserialize<T>(this MessagePackSerializer serializer, byte[] buffer)
		=> serializer.Deserialize<T>(buffer, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static T? Deserialize<T>(this MessagePackSerializer serializer, Stream stream)
		=> serializer.Deserialize<T>(stream, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static ValueTask<T?> DeserializeAsync<T>(this MessagePackSerializer serializer, PipeReader pipeReader)
		=> serializer.DeserializeAsync<T>(pipeReader, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static ValueTask<T?> DeserializeAsync<T>(this MessagePackSerializer serializer, Stream pipeReader)
		=> serializer.DeserializeAsync<T>(pipeReader, MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static MessagePackConverter<T> GetConverter<T>(this SerializationContext context)
		=> context.GetConverter<T>(MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static MessagePackConverter<T> GetConverter<T, TProvider>(this SerializationContext context)
		=> context.GetConverter<T>(MessagePackSerializerTestBase.GetShapeProvider<TProvider>());

	internal static JsonObject GetJsonSchema<T>(this MessagePackSerializer serializer)
		=> serializer.GetJsonSchema<T>(MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	[GenerateShape<int>]
	internal partial class Witness;
}

#endif
