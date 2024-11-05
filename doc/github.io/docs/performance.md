# Performance

## Synchronous

The synchronous (de)serialization APIs are the fastest.

Memory allocations are minimal during serialization and deserialization.
We strive for serialization to be allocation free.
Obviously the @"Nerdbank.MessagePack.MessagePackSerializer.Serialize``1(``0)" method must allocate the `byte[]` that is returned to the caller, but such allocations can be avoided by using any of the other @Nerdbank.MessagePack.MessagePackSerializer.Serialize* overloads which allows serializing to pooled buffers.

### Asynchronous

The asynchronous APIs are slower (ranging from slightly to dramatically slower) but reduce total memory pressure because the entire serialized representation does not tend to need to be in memory at once.

Memory pressure improvements are likely, but not guaranteed, because there are certain atomic values that must be in memory to be deserialized.
For example a very long string or `byte[]` buffer will have to be fully in memory in its msgpack form at the same time as the deserialized or original value itself.

Async (de)serialization tends to have a few object allocations during the operation.

## Comparison to MessagePack-CSharp

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.
We may close this gap in the future by adding AOT source generation to *this* library as well.
