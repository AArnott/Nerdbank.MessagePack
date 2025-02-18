# NBMsgPack050: Use ref parameters for ref structs

It is critical for certain `ref struct` types defined in ShapeShift to be passed by `ref` when used as a parameter.
Without this, a copy of the struct is made, and the buffers or position tracking fields that are modified by the callee will not apply back to the caller.

These types fall into this category:

- @ShapeShift.MessagePackReader
- @ShapeShift.MessagePackStreamingReader
- @ShapeShift.MessagePackWriter

## Example violation

The following method is declared with one of the above types as a parameter type, but without using the `ref` modifier:

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack050.cs#Defective)]

## Resolution

Add the `ref` modifier to the parameter (and all callers of the method):

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack050.cs#Fix)]
