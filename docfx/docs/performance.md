# Performance

## Best practices

Create, configure and reuse an instance of @ShapeShift.MessagePackSerializer rather than recreating it for each use.
These objects have a startup cost as they build runtime models for converters that needn't be paid multiple times if you reuse the object.
The object is thread-safe.
The object is not entirely publicly immutable, since its @ShapeShift.SerializerBase.RegisterConverter* method is allowed until it is used to serialize for the first time, so consider that when exposing the object to other code.

## Synchronous

The synchronous (de)serialization APIs are the fastest.

Memory allocations are minimal during serialization and deserialization.
We strive for serialization to be allocation free.
Obviously the @"ShapeShift.SerializerBase.Serialize``1(``0@,System.Threading.CancellationToken)" method must allocate the `byte[]` that is returned to the caller, but such allocations can be avoided by using any of the other @ShapeShift.SerializerBase.Serialize* overloads which allows serializing to pooled buffers.

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

This library has superior startup performance compared to MessagePack-CSharp due to not relying on reflection and Ref.Emit.
Throughput performance is on par with MessagePack-CSharp.

When using AOT source generation from MessagePack-CSharp and objects serialized with maps (as opposed to arrays), MessagePack-CSharp is slightly faster at *de*serialization.
We may close this gap in the future by adding AOT source generation to _this_ library as well.
