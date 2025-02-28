// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

internal static partial class MessagePackSerializerPolyfill
{
	// emulates what MessagePackSerializer can do with returning byte[], for convenience in testing.
	internal static byte[] Serialize<T>(this SerializerBase serializer, in T? value, ITypeShape<T> shape, CancellationToken cancellationToken = default)
	{
		Sequence<byte> seq = new();
		serializer.Serialize(seq, value, shape, cancellationToken);
		return seq.AsReadOnlySequence.ToArray();
	}

	// emulates what MessagePackSerializer can do with returning byte[], for convenience in testing.
	internal static byte[] Serialize<T, TProvider>(this SerializerBase serializer, in T? value, CancellationToken cancellationToken = default)
#if NET
		where TProvider : IShapeable<T>
#endif
	{
		Sequence<byte> seq = new();
		serializer.Serialize<T, TProvider>(seq, value, cancellationToken);
		return seq.AsReadOnlySequence.ToArray();
	}

	// emulates what MessagePackSerializer can do with returning byte[], for convenience in testing.
	internal static byte[] Serialize<T>(this SerializerBase serializer, in T? value, CancellationToken cancellationToken = default)
#if NET
		where T : IShapeable<T>
#endif
	{
		Sequence<byte> seq = new();
		serializer.Serialize<T>(seq, value, cancellationToken);
		return seq.AsReadOnlySequence.ToArray();
	}

#if !NET
	internal static byte[] Serialize<T>(this MessagePackSerializer serializer, in T? value, CancellationToken cancellationToken = default)
		=> serializer.Serialize(value, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static byte[] Serialize<T, TProvider>(this MessagePackSerializer serializer, in T? value, CancellationToken cancellationToken = default)
		=> serializer.Serialize(value, MessagePackSerializerTestBase.GetShapeProvider<TProvider>(), cancellationToken);

	internal static void Serialize<T, TProvider>(this SerializerBase serializer, IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		=> serializer.Serialize(writer, value, MessagePackSerializerTestBase.GetShapeProvider<TProvider>(), cancellationToken);

	internal static void Serialize<T>(this SerializerBase serializer, IBufferWriter<byte> writer, in T? value, CancellationToken cancellationToken = default)
		=> serializer.Serialize(writer, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static void Serialize<T>(this SerializerBase serializer, Stream stream, in T? value, CancellationToken cancellationToken = default)
		=> serializer.Serialize(stream, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static ValueTask SerializeAsync<T>(this SerializerBase serializer, Stream writer, in T? value, CancellationToken cancellationToken = default)
		=> serializer.SerializeAsync(writer, value, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static T? Deserialize<T, TProvider>(this SerializerBase serializer, byte[] sequence, CancellationToken cancellationToken = default)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<TProvider>(), cancellationToken);

	internal static T? Deserialize<T, TProvider>(this SerializerBase serializer, ReadOnlySequence<byte> sequence, CancellationToken cancellationToken = default)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<TProvider>(), cancellationToken);

	internal static T? Deserialize<T>(this SerializerBase serializer, ReadOnlySequence<byte> sequence, CancellationToken cancellationToken = default)
		=> serializer.Deserialize<T>(sequence, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static T? Deserialize<T>(this SerializerBase serializer, byte[] buffer, CancellationToken cancellationToken = default)
		=> serializer.Deserialize<T>(buffer, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static T? Deserialize<T>(this SerializerBase serializer, Stream stream, CancellationToken cancellationToken = default)
		=> serializer.Deserialize<T>(stream, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static ValueTask<T?> DeserializeAsync<T>(this SerializerBase serializer, PipeReader pipeReader, CancellationToken cancellationToken = default)
		=> serializer.DeserializeAsync<T>(pipeReader, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static ValueTask<T?> DeserializeAsync<T>(this SerializerBase serializer, Stream pipeReader, CancellationToken cancellationToken = default)
		=> serializer.DeserializeAsync<T>(pipeReader, MessagePackSerializerTestBase.GetShapeProvider<Witness>(), cancellationToken);

	internal static Converter<T> GetConverter<T>(this SerializationContext context)
		=> context.GetConverter<T>(MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	internal static Converter<T> GetConverter<T, TProvider>(this SerializationContext context)
		=> context.GetConverter<T>(MessagePackSerializerTestBase.GetShapeProvider<TProvider>());

	internal static JsonObject GetJsonSchema<T>(this SerializerBase serializer)
		=> serializer.GetJsonSchema<T>(MessagePackSerializerTestBase.GetShapeProvider<Witness>());

	[GenerateShape<int>]
	internal partial class Witness;
#endif
}
