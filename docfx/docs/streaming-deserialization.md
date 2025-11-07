# Streaming deserialization

While typical @Nerdbank.MessagePack.MessagePackSerializer.SerializeAsync* and @Nerdbank.MessagePack.MessagePackSerializer.DeserializeAsync* methods exist, these are methods that complete only when the entire job is done.
In particular for @Nerdbank.MessagePack.MessagePackSerializer.DeserializeAsync*, this means that no subset of the deserialized data is available until it is fully deserialized.

There are times however where progressively obtaining the deserialized elements can be useful.
For example, perhaps the stream contains a very long sequence of elements, and processing them incrementally instead of all at once can save memory or improve performance.
Or perhaps the stream is intentionally a long-lived generator stream that emits values over long periods of time, and it is important to the receiver that values are produced and available right away, before the stream ends.

The <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeEnumerableAsync*> and <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePathEnumerableAsync*> methods address such use cases.

One must first classify the presentation of msgpack streaming values to be deserialized.
Two forms are supported:

1. A stream that contains multiple msgpack structures without any envelope (e.g. a msgpack array).
1. A stream that contains a msgpack structure, within which is a sequence to be streamed (e.g. a msgpack array of elements).

## Sequence with no envelope

A sequence of msgpack structures without an array or any other data is said to have no envelope.
To asynchronously enumerate each of these structures, we use the <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeEnumerableAsync*> methods.

[!code-csharp[](../../samples/cs/StreamingDeserialization.cs#TopLevelStreamingEnumeration)]

## Sequence within an envelope

A sequence of msgpack structures that are found within a larger structure (e.g. a msgpack array) is said to have an envelope.
To asynchronously enumerate each of these structures requires first parsing through the envelope preamble to navigate to the sequence.
After enumerating the sequence, the remainder of the envelope is parsed in order to leave the reader positioned at valid position, at the end of the overall msgpack structure.

Navigating through the envelope is done by an expression provided to the <xref:Nerdbank.MessagePack.MessagePackSerializer.StreamingEnumerationOptions`2> argument passed to any of the <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePathEnumerableAsync*> methods that accept that as a parameter.

[!code-csharp[](../../samples/cs/StreamingDeserialization.cs#StreamingEnumerationWithEnvelope)]

The paths from envelope to sequence may include stepping through properties, indexing into arrays or even dictionaries.
However, not every valid C# expression will be accepted as a path.
