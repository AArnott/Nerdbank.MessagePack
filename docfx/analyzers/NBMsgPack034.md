# NBMsgPack034: Async converters should not reuse MessagePackWriter after returning it

Custom converters (classes that derive from @"ShapeShift.MessagePackConverter`1") that override the @ShapeShift.MessagePackConverter`1.WriteAsync* method should not reuse a @ShapeShift.MessagePackWriter after returning it via @ShapeShift.MessagePackAsyncWriter.ReturnWriter*.

## Example violation

In the example below, notice how @ShapeShift.MessagePackAsyncWriter.ReturnWriter\* is called and then the returned writer is used again on the next statement.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack034.cs#Defective)]

## Resolution

Rearrange the code so that all statements that use the writer occur before returning it.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack034.cs#Fix)]

Note that the return must occur before and awaited expression.
If the sync writer must be used _after_ the await expression as well, you may create a new sync writer after the await statement.
