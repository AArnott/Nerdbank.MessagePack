# Performance

## Best practices

Create, configure and reuse an instance of <xref:Nerdbank.MessagePack.MessagePackSerializer> rather than recreating it for each use.
These objects have a startup cost as they build runtime models for converters that needn't be paid multiple times if you reuse the object.
The object is thread-safe.
The object is entirely publicly immutable.

## Synchronous

The synchronous (de)serialization APIs are the fastest.

Memory allocations are minimal during serialization and deserialization.
We strive for serialization to be allocation free.
Obviously the <xref:Nerdbank.MessagePack.MessagePackSerializer.Serialize``1(``0@,System.Threading.CancellationToken)> method must allocate the `byte[]` that is returned to the caller, but such allocations can be avoided by using any of the other <xref:Nerdbank.MessagePack.MessagePackSerializer.Serialize*> overloads which allows serializing to pooled buffers.

## Asynchronous

The asynchronous APIs are slower (ranging from slightly to dramatically slower) but reduce total memory pressure because the entire serialized representation does not tend to need to be in memory at once.

Memory pressure improvements are likely, but not guaranteed, because there are certain atomic values that must be in memory to be deserialized.
For example a very long string or `byte[]` buffer will have to be fully in memory in its msgpack form at the same time as the deserialized or original value itself.

Async (de)serialization tends to have a few object allocations during the operation.

## Custom converters

The built-in converters in this library go to great lengths to optimize performance, including avoiding encoding/decoding strings for property names repeatedly.
These optimizations lead to less readable and maintainable converters, which is fine for this library where perf should be great by default.
Custom converters however are less likely to be highly tuned for performance.
For this reason, it can be a good idea to leverage the automatic converters for your data types wherever possible.

## Comparison to MessagePack-CSharp

Perf isn't everything, but it can be important in some scenarios.
Nerdbank.MessagePack is very fast, but not quite as fast as MessagePack-CSharp v3 with source generation turned on.

Features and ease of use are also important.
Nerdbank.MessagePack is much simpler to use, and comes [loaded with features](features.md#feature-comparison) that MessagePack-CSharp does not have.
Nerdbank.MessagePack also reliably works in AOT environments, while MessagePack-CSharp does not.

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.

[!include[](../includes/perf.md)]
