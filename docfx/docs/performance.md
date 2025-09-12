# Performance

## Best practices

Create, configure and reuse an instance of <xref:Nerdbank.MessagePack.MessagePackSerializer> rather than recreating it for each use.
These objects have a startup cost as they build runtime models for converters that needn't be paid multiple times if you reuse the object.
The object is thread-safe.
The object is entirely publicly immutable.

> [!TIP]
> You can get a quick performance boost by setting <xref:Nerdbank.MessagePack.MessagePackSerializer.PerfOverSchemaStability> to `true`, if you do not need to be able to deserialize data written with a previous version of your application.

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

## Assembly loads

If your application watches its startup perf to the extent of monitoring assembly loads, you can help keep your unrelated assembly loads down for serialization by using the serialization method overloads that take <xref:PolyType.ITypeShape`1> parameters instead of those that take <xref:PolyType.ITypeShapeProvider> parameters.
Note this is primarily relevant to older runtime targets like .NET Framework and .NET Standard, but can be relevant even for .NET when the top-level type to be serialized is an externally-defined type thus requiring a type shape or type shape provider to be explicitly specified.

Using methods that take <xref:PolyType.ITypeShapeProvider> [ends up loading every assembly that declares any type for which a type shape is generated](https://github.com/eiriktsarpalis/PolyType/issues/252) due to the way PolyType's source generated type shape provider works internally.

Take this example, which passes in a Witness type:

[!code-csharp[](../../samples/cs/Performance.cs#LoadsTooMany)]

Because the union of all witness types in an assembly contribute to a single source generated type shape provider, the mere presence of `BigInteger` (which is declared in `System.Numerics`) on any witness type will cause its assembly to be loaded when the witness type is used during serialization.

While declaring the witness type is required, you can avoid referencing it in your call into the serializer like this:

[!code-csharp[](../../samples/cs/Performance.cs#LoadsJustRight)]

This avoids the unwanted `System.Numerics` assembly load when serializing other types.

The pattern for the name to use to directly access the type shape is: `PolyType.SourceGenerator.TypeShapeProvider_<ASSEMBLY_NAME>.Default.<TYPE_NAME>`, where some character substitutions may exist for the substituted names.

Note when targeting .NET, using serialization overloads that neither <xref:PolyType.ITypeShape`1> nor <xref:PolyType.ITypeShapeProvider> (by virtue of a generic type parameter that is constrained to be <xref:PolyType.IShapeable`1>) avoids unwanted assembly loads as well.

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
