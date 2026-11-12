# Streaming sequences

While typical <xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeAsync*> and <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeAsync*> methods exist, these are methods that complete only when the entire job is done.
In particular for <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeAsync*>, this means that no subset of the deserialized data is available until it is fully deserialized.

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

## Serializing a sequence

<xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeEnumerableAsync*> writes an <xref:System.Collections.Generic.IAsyncEnumerable`1> as concatenated top-level MessagePack values, with no array around them.
That is the same framing <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeEnumerableAsync*> reads.
An empty sequence writes no bytes.
Each value is written as it is produced, and the destination is flushed when the unflushed buffer exceeds <xref:Nerdbank.MessagePack.SerializationContext.UnflushedBytesThreshold>, so a long or unbounded source stays bounded and applies backpressure.
The caller keeps ownership of the pipe or stream: pipes are not completed, and streams are not closed.

[!code-csharp[](../../samples/cs/StreamingSerialization.cs#TopLevelStreamingSerialization)]

Reference preservation is not supported for this framing.
A reference id would refer to another top-level value, and <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeEnumerableAsync*> rejects reference preservation for the same reason.

### Writing one array

A MessagePack array stores its length before any element, so an asynchronous sequence cannot be written as one array unless the length is already known.
<xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeArrayAsync*> takes that length, writes the array header, and then writes each element as the sequence produces it.
The result is an ordinary MessagePack array.
Read it with <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializeAsync*>, or stream its elements with <xref:Nerdbank.MessagePack.MessagePackSerializer.DeserializePathEnumerableAsync*>.

[!code-csharp[](../../samples/cs/StreamingSerialization.cs#ArrayStreamingSerialization)]

The sequence must produce exactly the declared number of elements.
A mismatch fails with <xref:Nerdbank.MessagePack.MessagePackSerializationException>, and any bytes already flushed stay written.
Reference preservation applies inside this array, because the array is one object graph.

When the length is not known ahead of time, use <xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeEnumerableAsync*>.
<xref:Nerdbank.MessagePack.MessagePackSerializer.SerializeAsync*> of an <xref:System.Collections.Generic.IEnumerable`1> or <xref:System.Collections.Generic.IAsyncEnumerable`1> is unchanged: it still buffers the elements and then writes one array.
