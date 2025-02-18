# NBMsgPack036: Async converters should not reuse readers after returning them

Custom converters (classes that derive from @"ShapeShift.MessagePackConverter`1") that override the @ShapeShift.MessagePackConverter`1.ReadAsync* method should not reuse a @ShapeShift.MessagePackReader or @ShapeShift.MessagePackStreamingReader after returning it via @ShapeShift.MessagePackAsyncReader.ReturnReader*.

## Example violation

In the following example, the @ShapeShift.MessagePackStreamingReader is returned before the method is done using it.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack036.cs#Defective)]

## Resolution

The fix is simply to move the @ShapeShift.MessagePackAsyncReader.ReturnReader\* call to a point after the method is done using the reader.

[!code-csharp[](../../samples/AnalyzerDocs/NBMsgPack036.cs#Fix)]
